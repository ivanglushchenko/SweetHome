module Getter

open System.Linq
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

let maxConnections = 10

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

    let rec loop tasks =
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