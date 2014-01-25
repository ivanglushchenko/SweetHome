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

    let adGroups =
        let load (file: FileInfo) =
            async { use s = file.OpenRead()
                    let publishedAt = DateTime.Parse file.Name
                    let dic = binDeserialize s :?> Dictionary<string, Advertisment>
                    for pair in dic.ToList() do
                        dic.[pair.Key] <- { pair.Value with IsNew = false }
                    return publishedAt, dic }
        let groups =
            advertismentsDirectory.GetFiles()
            |> Seq.map load
            |> Async.Parallel
            |> Async.RunSynchronously
        let dic = Dictionary<DateTime, Dictionary<string, Advertisment>>()
        for dt, ads in groups do
            dic.Add(dt, ads)
        dic

    let getGroup ad =
        if adGroups.ContainsKey ad.LastAppearedAt.Date
        then adGroups.[ad.LastAppearedAt.Date]
        else
            let d = Dictionary<string, Advertisment>()
            adGroups.Add(ad.LastAppearedAt.Date, d)
            d

    let containsAdvertisement ad =
        let group = getGroup ad
        group.ContainsKey ad.Url

    let addAdvertisement ad =
        let group = getGroup ad
        group.[ad.Url] <- ad

    let addOrigin ad =
        let group = getGroup ad
        group.[ad.Url].Origins.UnionWith ad.Origins

    let addAdvertisments ads =
        ads |> Seq.iter (addAdvertisement >> ignore)

    let saveSubscriptions() =
        for subscription in subscriptions do
            let fileName = Path.Combine(subscriptionsDirectory.FullName, subscription.Value.Name)
            use fs = new FileStream(fileName, FileMode.Create)
            binSerialize fs subscription.Value
             
    let saveAdvertisments() =
        for group in adGroups do
            let fileName = Path.Combine(advertismentsDirectory.FullName, group.Key.ToString("yyyy-MM-dd"))
            use fs = new FileStream(fileName, FileMode.Create)
            group.Value |> binSerialize fs
            fs.Dispose()

let refreshSubscriptions() =
    let getContent (context, url) =
        async { let client = new WebClient()
                let! content = client.AsyncDownloadString(new Uri(url))
                return context, content }
    let latestAdvertisments = 
        State.subscriptions
        |> Seq.map (fun t -> t.Value)
        |> Getter.getAllSubscriptions
        |> Array.map Parsing.parsePage
        |> List.concat
    let (existingAdvertisments, newAdvertisments) = latestAdvertisments |> List.partition State.containsAdvertisement
    if newAdvertisments.Length > 0 then
        let enrichedAdvertisments = 
            newAdvertisments 
            |> Seq.distinctBy (fun t -> t.Url)
            |> Getter.getAllAdvertisments
            |> Array.map Parsing.enrichAdvertisment
        State.addAdvertisments enrichedAdvertisments
        State.saveAdvertisments()
    existingAdvertisments |> List.iter State.addOrigin

let addSubscription s =
    State.subscriptions.[s.Name] <- s
    State.saveSubscriptions()

let getLatest n =
    let rec loadMore n (groups: Dictionary<string, Advertisment> list) = 
        seq { if n > 0 && groups.IsEmpty = false then
                let m = min n groups.Head.Count
                yield! groups.Head |> Seq.sortBy (fun t -> (DateTime.MaxValue.Subtract t.Value.LastAppearedAt).TotalDays) |> Seq.map (fun t -> t.Value)
                yield! loadMore (n - m) groups.Tail }
    loadMore n (State.adGroups |> Seq.sortBy (fun t -> (DateTime.MaxValue.Subtract t.Key).TotalDays) |> Seq.map (fun t -> t.Value) |> Seq.toList) |> Seq.sortBy (fun t -> DateTime.MaxValue.Subtract t.LastAppearedAt)