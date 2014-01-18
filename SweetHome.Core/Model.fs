module Model

open System
open System.Collections.Generic

type Advertisment =
    {   Url: string
        Place: string
        Caption: string
        Specials: string
        Price: float option
        PublishedAt: DateTime
        ReceivedAt: DateTime
        Map: string
        Address: string
        Bedrooms: int option
        Content: string
        Images: string list
        SimiliarItems: string list
        SimiliarItemsTotal: int
        SimiliarItemsToday: int
        FirstAppearanceOffset: int
        Phones: string list
        Year: int }
    override x.ToString() = sprintf "[%O] %s (%O/%O) - %O" x.PublishedAt x.Caption x.Place x.Bedrooms x.Price

let EmptyAdvertisment =
    {   Url = "";
        Place = "";
        Caption = "";
        Specials = "";
        Price = None;
        PublishedAt = DateTime.MinValue;
        ReceivedAt = DateTime.MinValue;
        Map = "";
        Address = "";
        Bedrooms = None;
        Content = "";
        Images = [];
        SimiliarItems = [];
        SimiliarItemsTotal = 0;
        SimiliarItemsToday = 0;
        FirstAppearanceOffset = 0;
        Phones = [];
        Year = 0 }

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

