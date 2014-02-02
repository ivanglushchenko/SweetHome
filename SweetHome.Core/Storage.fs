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
        
    let markSimiliarAds ads =
        let dic = Dictionary<string, List<Advertisment>>()
        for ad in ads do
            let key = sprintf "%O:%O:%O:%O:%O:%O:%O" ad.Bedrooms ad.Price ad.Caption ad.Place ad.Address ad.AddressUrl ad.CLTags
            if dic.ContainsKey key = false then
                dic.Add(key, List())
            dic.[key].Add ad
        let mark g =
            let s = g |> Seq.sortBy (fun t -> -t.LastAppearedAt.Ticks) |> Seq.toList
            let dateMin = s |> Seq.map (fun t -> t.FirstAppearedAt) |> Seq.min
            s |> Seq.map (fun t -> { t with FirstAppearedAt = dateMin; IsDuplicated = t <> s.Head })
        dic |> Seq.collect (fun t -> mark t.Value)

    let advertisments =
        let load (file: FileInfo) =
            async { use s = file.OpenRead()
                    let list = binDeserialize s :?> Advertisment[]
                    for a in list do
                        a.IsNew <- false
                    return list }
        let ads =
            advertismentsDirectory.GetFiles()
            |> Seq.map load
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Seq.concat
        List(markSimiliarAds ads)

    let advertismentsByUrl =
        let hs = Dictionary<string, Advertisment>()
        for p in advertisments do
            hs.Add(p.Url, p)
        hs

    let containsAdvertisement ad =
        advertismentsByUrl.ContainsKey ad.Url

    let addAdvertisement ad =
        advertisments.Add ad
        advertismentsByUrl.Add(ad.Url, ad)
        ad.IsNew <- true

    let addOrigin ad =
        advertismentsByUrl.[ad.Url].Origins.UnionWith ad.Origins

    let saveSubscriptions() =
        for subscription in subscriptions do
            let fileName = Path.Combine(subscriptionsDirectory.FullName, subscription.Value.Name)
            use fs = new FileStream(fileName, FileMode.Create)
            binSerialize fs subscription.Value
             
    let saveAdvertisments() =
        let fileName = Path.Combine(advertismentsDirectory.FullName, "all")
        use fs = new FileStream(fileName, FileMode.Create)
        advertisments |> Array.ofSeq |> binSerialize fs
        fs.Dispose()

let refreshSubscriptions() =
    let latestAdvertisments = 
        State.subscriptions
        |> Seq.map (fun t -> t.Value)
        |> Getter.getAllSubscriptions
        |> Array.map Parsing.parsePage
        |> List.concat
    let (existingAdvertisments, newAdvertisments) = latestAdvertisments |> List.partition State.containsAdvertisement
    for ad in existingAdvertisments do
        State.addOrigin ad
    if newAdvertisments.Length > 0 then
        let enrichedAdvertisments = 
            newAdvertisments 
            |> Seq.distinctBy (fun t -> t.Url)
            |> Getter.getAllAdvertisments
            |> Array.map Parsing.enrichAdvertisment
        enrichedAdvertisments |> Seq.iter State.addAdvertisement
    if existingAdvertisments.Length > 0 || newAdvertisments.Length > 0 then
        State.saveAdvertisments()

let addSubscription s =
    State.subscriptions.[s.Name] <- s
    State.saveSubscriptions()

let getLatest n =
    let m = min n State.advertisments.Count
    State.advertisments
    |> Seq.sortBy (fun t -> DateTime.MaxValue.Subtract t.LastAppearedAt)
    |> Seq.take m