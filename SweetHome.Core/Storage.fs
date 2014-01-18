module Storage

open FSharp.Data
open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Runtime.Serialization.Formatters.Binary
open System.Runtime.Serialization.Json
open System.Runtime.Serialization
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

    let subscriptionsSerializer = new DataContractSerializer(typeof<Subscription>)

    let subscriptions =
        let tasks =
            subscriptionsDirectory.GetFiles() 
            |> Seq.map (fun file -> use fs = file.OpenRead() in binDeserialize fs :?> Subscription)
        List(tasks)

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

    let getGroup dt =
        if adGroups.ContainsKey dt
        then adGroups.[dt]
        else
            let d = Dictionary<string, Advertisment>()
            adGroups.Add(dt, d)
            d

    let addAdvertisement ad =
        let group = getGroup ad.PublishedAt
        group.[ad.Url] <- ad

    let addAdvertisments ads =
        ads |> Seq.iter addAdvertisement

    let saveSubscriptions() =
        for subscription in subscriptions do
            let fileName = Path.Combine(subscriptionsDirectory.FullName, subscription.Name)
            use fs = new FileStream(fileName, FileMode.Create)
            binSerialize fs subscription 
             
    let saveAdvertisments() =
        for group in adGroups do
            let fileName = Path.Combine(advertismentsDirectory.FullName, group.Key.ToString("yyyy-MM-dd"))
            use fs = new FileStream(fileName, FileMode.Create)
            group.Value |> binSerialize fs
            fs.Dispose()

let getContent url =
    async { let client = new WebClient()
            let! content = client.AsyncDownloadString(new Uri(url))
            return content  }

let downloadSubscriptions() =
    let advertisments = 
        State.subscriptions
        |> Seq.map (fun t -> t.Url)
        |> Seq.map getContent
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.map Parsing.parsePage
        |> List.concat
    State.addAdvertisments advertisments
    State.saveAdvertisments()

let load() =
    downloadSubscriptions()
    ()

let addSubscription s =
    State.subscriptions.Add s
    State.saveSubscriptions()