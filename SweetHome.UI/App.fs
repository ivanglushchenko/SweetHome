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
open System.Diagnostics
open System.Windows
open FSharpx

type MainWindow = XAML<"MainWindow.xaml">

let rec getUrl (source: DependencyObject) =
    match source with
    | null -> None
    | :? ListViewItem as li -> (li.DataContext :?> Model.Advertisment).Url |> Some
    | _ -> VisualTreeHelper.GetParent source |> getUrl

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
    
    let refreshFilters() =
        window.lbFilterByOrigin.ItemsSource <- (items |> Seq.collect (fun t -> t.Origins) |> Set.ofSeq).AsEnumerable()
        window.lbFilterByBedrooms.ItemsSource <- (items |> Seq.map (fun t -> t.Bedrooms) |> Set.ofSeq).AsEnumerable()

    let refreshItems() =
        Storage.refreshSubscriptions()
        items.Clear()
        for ad in Storage.getLatest 1000 do
            items.Add ad
        refreshFilters()

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
    window.lbItems.MouseDoubleClick.Add(fun e -> getUrl (e.OriginalSource :?> DependencyObject) |> Option.bind (fun url -> Process.Start(url.ToString()) |> Some) |> ignore)
    window.btnRefresh.Click.Add(fun e -> refreshItems())
    
    window.lbFilterByOrigin.AddHandler(CheckBox.CheckedEvent, asHandler (fun o -> o :?> string |> includedOrigins.Add))
    window.lbFilterByOrigin.AddHandler(CheckBox.UncheckedEvent, asHandler (fun o -> o :?> string |> includedOrigins.Remove))
    window.lbFilterByBedrooms.AddHandler(CheckBox.CheckedEvent, asHandler (fun o -> o :?> int option |> includedBedrooms.Add))
    window.lbFilterByBedrooms.AddHandler(CheckBox.UncheckedEvent, asHandler (fun o -> o :?> int option |> includedBedrooms.Remove))
    window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore