module Model

open System
open System.ComponentModel
open System.Collections.Generic

let AdvertismentChanged = new Event<_,_>()
let IsNewPropertyStore = Dictionary<obj, bool>()

type Advertisment =
    {   Url: string
        Place: string
        Caption: string
        Price: int option
        FirstAppearedAt: DateTime
        LastAppearedAt: DateTime
        Address: string
        AddressUrl: string
        Bedrooms: int option
        Origins: HashSet<string>
        Urls: HashSet<string>
        CLTags: string
        IsDuplicated: bool }

    override x.ToString() = sprintf "[%O] %s (%O/%O) - %O" x.LastAppearedAt x.Caption x.Place x.Bedrooms x.Price

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = AdvertismentChanged.Publish

    // UI-only properties. These properties should be in a viewmodel, but i am way too lazy for that
    member x.IsNew 
        with get() =
            if IsNewPropertyStore.ContainsKey x then IsNewPropertyStore.[x] else false
        and set(v) =
            IsNewPropertyStore.[x] <- v
            AdvertismentChanged.Trigger(x, PropertyChangedEventArgs("IsNew"))

let EmptyAdvertisment =
    {   Url = ""
        Place = ""
        Caption = ""
        Price = None;
        FirstAppearedAt = DateTime.MinValue
        LastAppearedAt = DateTime.MinValue
        Address = ""
        AddressUrl = ""
        Bedrooms = None
        Origins = HashSet<string>()
        Urls = HashSet<string>()
        CLTags = ""
        IsDuplicated = false }

type Subscription =
    {   Name: string
        QueryUrl: string
        BaseAddress: string
        Ignores: string
        IsEnabled: bool }

let EmptySubscription =
    {   Name = ""
        QueryUrl = ""
        BaseAddress = ""
        Ignores = ""
        IsEnabled = true }

let reduce ad =
    { EmptyAdvertisment with 
        Bedrooms = ad.Bedrooms
        Price = ad.Price
        Caption = ad.Caption
        CLTags = ad.CLTags; Place = ad.Place
        Address = ad.Address
        AddressUrl = ad.AddressUrl  }