module MainApp

open System
open System.Windows
open System.Windows.Controls
open System.Linq
open System.Collections.ObjectModel
open System.Collections.Generic
open System.Windows
open System.Windows.Data
open System.Windows.Input
open System.Windows.Media
open System.Windows.Documents
open System.Diagnostics
open System.Windows
open FSharpx

type MainWindow = XAML<"MainWindow.xaml">

let getUrl (source: DependencyObject) =
    let rec loop (s: DependencyObject) =
        match s with
        | :? Hyperlink as hp -> if hp.Tag <> null then Some (hp.Tag.ToString()) else hp.Parent |> loop
        | :? FrameworkContentElement as r -> r.Parent |> loop
        | :? FrameworkElement as fe ->
            match fe.DataContext with
            | :? Model.Advertisment as ad -> ad.Url |> Some
            | _ -> None
        | _ -> None
    match source with
    | :? FrameworkContentElement as r -> loop r
    | _ -> None

let loadWindow() =
    let window = MainWindow()
    let items = ObservableCollection<Model.Advertisment>(Storage.getLatest 1000)
    let itemsView = ListCollectionView(items)

    let includedOrigins = HashSet<string>()
    let includedBedrooms = HashSet<int option>()

    Storage.addSubscription { Model.EmptySubscription with Name = "midwood"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=midwood&minAsk=1400&maxAsk=2100&bedrooms=1&housing_type=&hasPic=1&excats="; BaseAddress = "http://newyork.craigslist.org" }
    Storage.addSubscription { Model.EmptySubscription with Name = "kings hwy"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=kings+highway&minAsk=1400&maxAsk=2100&bedrooms=1&housing_type=&hasPic=1&excats="; BaseAddress = "http://newyork.craigslist.org" }
    Storage.addSubscription { Model.EmptySubscription with Name = "brighton"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=brighton+beach&minAsk=1400&maxAsk=2100&bedrooms=1&housing_type=&excats="; BaseAddress = "http://newyork.craigslist.org" }
    Storage.addSubscription { Model.EmptySubscription with Name = "astoria"; Url = "http://newyork.craigslist.org/search/aap/que?zoomToPosting=&catAbb=aap&query=astoria&minAsk=1400&maxAsk=2100&bedrooms=1&housing_type=&excats="; BaseAddress = "http://newyork.craigslist.org" }
    Storage.addSubscription { Model.EmptySubscription with Name = "lic"; Url = "http://newyork.craigslist.org/search/aap/que?zoomToPosting=&catAbb=aap&query=long+island+city&minAsk=1400&maxAsk=2100&bedrooms=1&housing_type=&hasPic=1&excats="; BaseAddress = "http://newyork.craigslist.org" }
    
    let invoke f =
        window.Root.Dispatcher.BeginInvoke(new Action(f)) |> ignore

    let refreshFilters() =
        window.lbFilterByOrigin.ItemsSource <- (items |> Seq.collect (fun t -> t.Origins) |> Set.ofSeq).AsEnumerable()
        window.lbFilterByBedrooms.ItemsSource <- (items |> Seq.map (fun t -> t.Bedrooms) |> Set.ofSeq).AsEnumerable()

    let refreshItems() =
        window.bdWait.Visibility <- Visibility.Visible
        window.lblMessage.Text <- "Downloading subscriptions..."
        Getter.progressCallback <- Some (fun i j k -> invoke (fun () -> window.lblProgress.Text <- sprintf "%i in progress, %i in queue, %i completed" j i k))
        async { Storage.refreshSubscriptions()
                invoke (fun () ->
                            items.Clear()
                            for ad in Storage.getLatest 1000 do
                                items.Add ad
                            refreshFilters()
                            window.bdWait.Visibility <- Visibility.Collapsed)
                return () } |> Async.Start

    let resetFilter() =
        let check (ad: Model.Advertisment) =
            if includedOrigins.Count > 0 && (includedOrigins.Intersect ad.Origins).Count() = 0
            then false
            else if includedBedrooms.Count > 0 && includedBedrooms.Contains ad.Bedrooms = false
            then false
            else true
        itemsView.Filter <- new Predicate<obj>(fun o -> o :?> Model.Advertisment |> check)

    refreshFilters()

    let asHandler f = new RoutedEventHandler(fun s e -> (e.OriginalSource :?> CheckBox).DataContext |> (fun t -> ignore(f t); resetFilter()))
        
    window.lbItems.ItemsSource <- itemsView
    window.lbItems.PreviewMouseUp.Add(fun e -> getUrl (e.OriginalSource :?> DependencyObject) |> Option.bind (fun url -> Process.Start(url.ToString()) |> Some) |> ignore)
    window.btnRefresh.Click.Add(fun e -> refreshItems())
    
    window.lbFilterByOrigin.AddHandler(CheckBox.CheckedEvent, asHandler (fun o -> o :?> string |> includedOrigins.Add))
    window.lbFilterByOrigin.AddHandler(CheckBox.UncheckedEvent, asHandler (fun o -> o :?> string |> includedOrigins.Remove))
    window.lbFilterByBedrooms.AddHandler(CheckBox.CheckedEvent, asHandler (fun o -> o :?> int option |> includedBedrooms.Add))
    window.lbFilterByBedrooms.AddHandler(CheckBox.UncheckedEvent, asHandler (fun o -> o :?> int option |> includedBedrooms.Remove))
    window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore