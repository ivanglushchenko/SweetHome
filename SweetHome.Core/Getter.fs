module Getter

open System
open System.Net
open System.Linq
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic
open Model

let maxConnections = 20

let mutable progressCallback: (int -> int -> int -> unit) option = None

let getAll (tasks: (unit -> 'v) seq) =
    let queuedItems = Queue<unit -> 'v>()
    let results = List<'v>()
    tasks |> Seq.iter (fun t -> queuedItems.Enqueue t)

    let getNextTast() =
        if queuedItems.Count > 0 
        then queuedItems.Dequeue() |> Some
        else None

    let initialArray = 
        Array.init maxConnections (fun i -> match getNextTast() with | Some f -> (Task.Run f) :> Task | _ -> null)
        |> Array.filter (fun t -> t <> null)

    let reportProgress i j k =
        match progressCallback with
        | Some f -> f i j k
        | None -> ()

    let rec loop tasks =
        reportProgress queuedItems.Count (Array.length tasks) results.Count
        let i = Task.WaitAny tasks
        results.Add((tasks.[i] :?> Task<'v>).Result)
        tasks.[i] <- null
        match getNextTast() with
        | Some f -> 
            tasks.[i] <- (Task.Run f) :> Task
            loop tasks
        | None ->
            if tasks.Length > 1
            then tasks |> Array.filter (fun t -> t <> null) |> loop

    loop initialArray
    
    results |> Seq.toArray

let getContent (context, url) =
    let client = new WebClient()
    let content = client.DownloadString(new Uri(url))
    context, content

let getAllSubscriptions subscriptions =
    subscriptions
    |> Seq.map (fun t -> fun () -> getContent (t, t.QueryUrl))
    |> getAll

let getAllAdvertisments advertisments =
    advertisments
    |> Seq.map (fun t -> fun () -> getContent (t, t.Url))
    |> getAll