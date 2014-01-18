module Storage

open FSharp.Data
open System
open System.IO
open System.Net
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
                    return publishedAt, binDeserialize s :?> Dictionary<string, Advertisment> }
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
        if adGroups.ContainsKey ad.PublishedAt
        then adGroups.[ad.PublishedAt]
        else
            let d = Dictionary<string, Advertisment>()
            adGroups.Add(ad.PublishedAt, d)
            d

    let isNewAdvertisment ad =
        adGroups.ContainsKey ad.PublishedAt = false || adGroups.[ad.PublishedAt].ContainsKey ad.Url = false

    let addAdvertisement ad =
        let group = getGroup ad
        group.[ad.Url] <- ad

    let addAdvertisments ads =
        ads |> Seq.iter addAdvertisement

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
    let getContent url =
        async { let client = new WebClient()
                let! content = client.AsyncDownloadString(new Uri(url))
                return content  }
    let newAdvertisments = 
        State.subscriptions
        |> Seq.map (fun t -> t.Value.Url)
        |> Seq.map getContent
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.map Parsing.parsePage
        |> List.concat
        |> List.filter State.isNewAdvertisment
    if newAdvertisments.Length > 0 then
        State.addAdvertisments newAdvertisments
        State.saveAdvertisments()

let load() =
    refreshSubscriptions()
    ()

let addSubscription s =
    State.subscriptions.[s.Name] <- s
    State.saveSubscriptions()

let getLatest n =
    let rec loadMore n (groups: Dictionary<string, Advertisment> list) = 
        seq { if n > 0 && groups.IsEmpty = false then
                let m = min n groups.Head.Count
                yield! groups.Head |> Seq.sortBy (fun t -> (DateTime.MaxValue.Subtract t.Value.ReceivedAt).TotalDays) |> Seq.map (fun t -> t.Value)
                yield! loadMore (n - m) groups.Tail }
    loadMore n (State.adGroups |> Seq.sortBy (fun t -> (DateTime.MaxValue.Subtract t.Key).TotalDays) |> Seq.map (fun t -> t.Value) |> Seq.toList)