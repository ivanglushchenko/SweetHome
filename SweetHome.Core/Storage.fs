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

let rootDirectory = @"s:\Sources\_Data\SweetHome\"

let storeDirectory =
    let path = Path.Combine(rootDirectory, "store")
    if Directory.Exists path = false 
    then Directory.CreateDirectory path
    else DirectoryInfo(path)

let subscriptionsDirectory = 
    let d = storeDirectory.GetDirectories "subscriptions"
    if d.Length = 0
    then storeDirectory.CreateSubdirectory "subscriptions"
    else d.[0]

let advertismentsDirectory = 
    let d = storeDirectory.GetDirectories "advertisments"
    if d.Length = 0
    then storeDirectory.CreateSubdirectory "advertisments"
    else d.[0]

let getContent url =
    async { let client = new WebClient()
            let! content = client.AsyncDownloadString(new Uri(url))
            return content  }

module private State =
    let subscriptionsSerializer = new DataContractSerializer(typeof<Subscription>)
    let subscriptions =
        let loadSubscription (stream: Stream) =
            async { let s = subscriptionsSerializer.ReadObject(stream) :?> Subscription
                    return s } 
        let tasks =
            subscriptionsDirectory.GetFiles() 
            |> Seq.map (fun file -> file.OpenRead() |> subscriptionsSerializer.ReadObject :?> Subscription)
        List(tasks)

    let saveSubscription s =
        let sFileName = Path.Combine(subscriptionsDirectory.FullName, s.Name)
        use fs = new FileStream(sFileName, FileMode.Create)
        subscriptionsSerializer.WriteObject(fs, s)

let downloadSubscriptions() =
    let parse content =
        let r = new Regex("""<p class="row"[^>]*>(\s)*<a[^>]*>(\s)*</a>(\s)*<span class="star"></span>(\s)*<span class="pl">(\s)*<span class="date">[^<]*</span>(\s)*<a href="[^"]*">[^<]*</a>(\s)*</span>(\s)*<span[^>]*>(\s)*<span class="price">[^<]*</span>(\s)*/[^<]*<span class="pnr">((\s)*<small>[^<]*</small>(\s)*)?""")
        let s = seq { for m in r.Matches(content) -> m } |> List.ofSeq
        () 

    let contents = 
        State.subscriptions
        |> Seq.map (fun t -> t.Url)
        |> Seq.map getContent
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.map parse
    ()

let load() =
    downloadSubscriptions()
    //State.loadSubscriptions()
    let ad = { EmptyAdvertisment with Id = "sss" }
    let bf = new BinaryFormatter()
    let ms = new MemoryStream()
    bf.Serialize(ms, ad)

    ms.Seek(0L, SeekOrigin.Begin) |> ignore

    let ad2 = bf.Deserialize(ms) :?> Advertisment
    let partitions = storeDirectory.GetFiles()
//    let loadPartition (f: FileInfo) =
//        async { let s = f.OpenText()
//
//                return Array.map convert adv }

        //async { let! s = Stream. }
    ()

let addSubscription s =
    State.subscriptions.Add s
    State.saveSubscription s