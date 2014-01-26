module Model

open System
open System.Collections.Generic

type Advertisment =
    {   Url: string
        Place: string
        Caption: string
        Specials: string
        Price: int option
        FirstAppearedAt: DateTime
        LastAppearedAt: DateTime
        Map: string
        Address: string
        AddressUrl: string
        Bedrooms: int option
        Content: string
        Images: string list
        SimiliarItems: string list
        SimiliarItemsTotal: int
        SimiliarItemsToday: int
        Phones: string list
        Origins: HashSet<string>

        // UI-only properties. These properties should be in a viewmodel, but i am way too lazy for that
        IsNew: bool }
    override x.ToString() = sprintf "[%O] %s (%O/%O) - %O" x.LastAppearedAt x.Caption x.Place x.Bedrooms x.Price

let EmptyAdvertisment =
    {   Url = ""
        Place = ""
        Caption = ""
        Specials = ""
        Price = None;
        FirstAppearedAt = DateTime.MinValue
        LastAppearedAt = DateTime.MinValue
        Map = ""
        Address = ""
        AddressUrl = ""
        Bedrooms = None
        Content = ""
        Images = []
        SimiliarItems = []
        SimiliarItemsTotal = 0
        SimiliarItemsToday = 0
        Phones = []
        Origins = HashSet<string>()
        IsNew = false }

type Subscription =
    {   Name: string
        Url: string
        BaseAddress: string
        Ignores: string
        Items: List<string>
        IsEnabled: bool }

let EmptySubscription =
    {   Name = "";
        Url = "";
        BaseAddress = "";
        Ignores = "";
        Items = List();
        IsEnabled = true }

