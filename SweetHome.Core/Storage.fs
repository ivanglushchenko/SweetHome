module Storage

open FSharp.Data
open System
open System.IO
open System.Net
open System.Linq
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Runtime.Serialization.Formatters.Binary
open Model

module private State =
    let rootDirectory = @"s:\Sources\_Data\SweetHome\"

    let binSerialize where what =
        BinaryFormatter().Serialize(where, what)

    let binDeserialize from =
        BinaryFormatter().Deserialize(from)

    let storeDirectory =
        let path = Path.Combine(rootDirectory, "store")
        if Directory.Exists path = false 
        then Directory.CreateDirectory path
        else DirectoryInfo(path)

    let getOrCreateSubDir (dir: DirectoryInfo) n =
        let d = storeDirectory.GetDirectories n
        if d.Length = 0
        then storeDirectory.CreateSubdirectory n
        else d.[0]

    let subscriptionsDirectory = 
        getOrCreateSubDir storeDirectory "subscriptions"

    let advertismentsDirectory = 
        getOrCreateSubDir storeDirectory "advertisments"

    let subscriptions =
        let tasks =
            subscriptionsDirectory.GetFiles() 
            |> Seq.map (fun file -> use fs = file.OpenRead() in binDeserialize fs :?> Subscription)
        let d = Dictionary<string, Subscription>()
        for s in tasks do
            d.Add(s.Name, s)
        d
        
    let advertisments =
        let load (file: FileInfo) =
            async { use s = file.OpenRead()
                    let list = binDeserialize s :?> Advertisment[]
                    return list |> Seq.map (fun t -> { t with IsNew = false }) }
        let ads =
            advertismentsDirectory.GetFiles()
            |> Seq.map load
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Seq.concat
        let dic = Dictionary<Advertisment, Advertisment>()
        for ad in ads do
            dic.Add(reduce ad, ad)
        dic

    let existingUrls =
        let hs = HashSet<string>()
        for p in advertisments do
            hs.UnionWith p.Value.Urls
        hs

    let containsAdvertisement ad =
        existingUrls.Contains ad.Url

    let addAdvertisement ad =
        let rad = reduce ad
        if advertisments.ContainsKey rad
        then
            let existingAd = advertisments.[rad]
            let mergedAd = 
                { existingAd with 
                    LastAppearedAt = max existingAd.LastAppearedAt ad.LastAppearedAt
                    FirstAppearedAt = min existingAd.FirstAppearedAt ad.FirstAppearedAt }
            mergedAd.Origins.UnionWith ad.Origins
            mergedAd.Urls.UnionWith ad.Urls
            existingUrls.UnionWith ad.Urls
            if Option.isNone mergedAd.Bedrooms 
            then printfn "dddd"
            advertisments.[rad] <- mergedAd
        else
            if Option.isNone ad.Bedrooms 
            then printfn "dddd"
            advertisments.Add(rad, ad)

    let saveSubscriptions() =
        for subscription in subscriptions do
            let fileName = Path.Combine(subscriptionsDirectory.FullName, subscription.Value.Name)
            use fs = new FileStream(fileName, FileMode.Create)
            binSerialize fs subscription.Value
             
    let saveAdvertisments() =
        let fileName = Path.Combine(advertismentsDirectory.FullName, "all")
        use fs = new FileStream(fileName, FileMode.Create)
        advertisments |> Seq.map (fun t -> t.Value) |> Array.ofSeq |> binSerialize fs
        fs.Dispose()

let refreshSubscriptions() =
    let latestAdvertisments = 
        State.subscriptions
        |> Seq.map (fun t -> t.Value)
        |> Getter.getAllSubscriptions
        |> Array.map Parsing.parsePage
        |> List.concat
    let newAdvertisments = latestAdvertisments |> List.filter State.containsAdvertisement
    if newAdvertisments.Length > 0 then
        let enrichedAdvertisments = 
            newAdvertisments 
            |> Seq.distinctBy (fun t -> t.Url)
            |> Getter.getAllAdvertisments
            |> Array.map Parsing.enrichAdvertisment
        enrichedAdvertisments |> Seq.iter State.addAdvertisement
        State.saveAdvertisments()

let addSubscription s =
    State.subscriptions.[s.Name] <- s
    State.saveSubscriptions()

let getLatest n =
    let m = min n State.advertisments.Count
    State.advertisments
    |> Seq.map (fun t -> t.Value)
    |> Seq.sortBy (fun t -> DateTime.MaxValue.Subtract t.LastAppearedAt)
    |> Seq.take m

let markAsRead (ad: Advertisment) =
    let rad = reduce ad
    State.advertisments.[rad] <- { State.advertisments.[rad] with IsNew = false }