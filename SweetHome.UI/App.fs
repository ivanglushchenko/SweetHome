module MainApp

open System
open System.Windows
open System.Windows.Controls
open System.Linq
open System.Collections
open FSharpx

type MainWindow = XAML<"MainWindow.xaml">

let loadWindow() =
   let window = MainWindow()
   
   Storage.addSubscription { Model.EmptySubscription with Name = "midwood"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=midwood&minAsk=1000&maxAsk=2100&bedrooms=1&housing_type=&hasPic=1&excats="; BaseAddress = "http://newyork.craigslist.org" }
   Storage.addSubscription { Model.EmptySubscription with Name = "brighton"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=brighton+beach&minAsk=1000&maxAsk=2100&bedrooms=1&housing_type=&excats="; BaseAddress = "http://newyork.craigslist.org" }
   Storage.addSubscription { Model.EmptySubscription with Name = "astoria"; Url = "http://newyork.craigslist.org/search/aap/que?zoomToPosting=&catAbb=aap&query=astoria&minAsk=1000&maxAsk=2100&bedrooms=1&housing_type=&excats="; BaseAddress = "http://newyork.craigslist.org" }
   
   Storage.load()
   window.lbItems.ItemsSource <- (Storage.getLatest 200).AsEnumerable() :> IEnumerable
   window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore