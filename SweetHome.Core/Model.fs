module Model

open System
open System.Collections.Generic

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

        // UI-only properties. These properties should be in a viewmodel, but i am way too lazy for that
        IsNew: bool }
    override x.ToString() = sprintf "[%O] %s (%O/%O) - %O" x.LastAppearedAt x.Caption x.Place x.Bedrooms x.Price

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
        IsNew = false }

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