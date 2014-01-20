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

type ViewModel() =
    let d = 0

let loadWindow() =
    let window = MainWindow()
    let items = ObservableCollection<Model.Advertisment>(Storage.getLatest 1000)
    let itemsView = ListCollectionView(items)

    let includedOrigins = HashSet<string>()
    let includedBedrooms = HashSet<int option>()

    Storage.addSubscription { Model.EmptySubscription with Name = "midwood"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=midwood&minAsk=1000&maxAsk=2100&bedrooms=1&housing_type=&hasPic=1&excats="; BaseAddress = "http://newyork.craigslist.org" }
    Storage.addSubscription { Model.EmptySubscription with Name = "brighton"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=brighton+beach&minAsk=1000&maxAsk=2100&bedrooms=1&housing_type=&excats="; BaseAddress = "http://newyork.craigslist.org" }
    Storage.addSubscription { Model.EmptySubscription with Name = "astoria"; Url = "http://newyork.craigslist.org/search/aap/que?zoomToPosting=&catAbb=aap&query=astoria&minAsk=1000&maxAsk=2100&bedrooms=1&housing_type=&excats="; BaseAddress = "http://newyork.craigslist.org" }
    
    window.lbItems.ItemsSource <- items
    window.lbItems.MouseDoubleClick.Add(fun e -> getUrl (e.OriginalSource :?> DependencyObject) |> Option.bind (fun url -> Process.Start(url.ToString()) |> Some) |> ignore)
    window.btnRefresh.Click.Add(fun e -> Storage.refreshSubscriptions())
    window.lbFilterByOrigin.ItemsSource <- (items |> Seq.collect (fun t -> t.Origins) |> Set.ofSeq).AsEnumerable()
    window.lbFilterByBedrooms.ItemsSource <- (items |> Seq.map (fun t -> t.Bedrooms) |> Set.ofSeq).AsEnumerable()
    window.Root.DataContext <- ViewModel()

    let onFilterByOrigin s e = 
        ()

    let onFilterByBedrooms s e =
        ()
        0

    let asHandler f = new RoutedEventHandler(fun s e -> (e.OriginalSource :?> CheckBox).DataContext |> f |> ignore)
        
    window.lbFilterByOrigin.AddHandler(CheckBox.CheckedEvent, asHandler (fun o -> o :?> string |> includedOrigins.Add))
    window.lbFilterByOrigin.AddHandler(CheckBox.UncheckedEvent, asHandler (fun o -> o :?> string |> includedOrigins.Remove))
    window.lbFilterByBedrooms.AddHandler(CheckBox.CheckedEvent, asHandler (fun o -> o :?> int option |> includedBedrooms.Add))
    window.lbFilterByBedrooms.AddHandler(CheckBox.UncheckedEvent, asHandler (fun o -> o :?> int option |> includedBedrooms.Remove))

    //window.Root.CommandBindings.Add(new CommandBinding(ApplicationCommands.CancelPrint, new ExecutedRoutedEventHandler(onFilterByOrigin))) |> ignore
    //window.Root.CommandBindings.Add(new CommandBinding(ApplicationCommands.CorrectionList, new ExecutedRoutedEventHandler(onFilterByBedrooms))) |> ignore
    window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore