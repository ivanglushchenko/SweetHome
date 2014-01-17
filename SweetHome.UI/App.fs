module MainApp

open System
open System.Windows
open System.Windows.Controls
open FSharpx

type MainWindow = XAML<"MainWindow.xaml">

let loadWindow() =
   let window = MainWindow()
   
   //Storage.addSubscription { Model.EmptySubscription with Name = "midwood"; Url = "http://newyork.craigslist.org/search/aap/brk?zoomToPosting=&catAbb=aap&query=midwood&minAsk=1500&maxAsk=2100&bedrooms=1&housing_type=&hasPic=1&excats="; }
   // Your awesome code code here and you have strongly typed access to the XAML via "window"
   Storage.load()
   
   window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore