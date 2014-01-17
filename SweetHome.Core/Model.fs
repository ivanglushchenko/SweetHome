module Model

open System
open System.Collections.Generic

type Advertisment =
    {   Id: string
        FeedId: string
        Url: string
        Place: string
        Caption: string
        Specials: string
        Price: float option
        PublishedAt: DateTime option
        ReceivedAt: DateTime option
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

let EmptyAdvertisment =
    {   Id = "";
        FeedId = "";
        Url = "";
        Place = "";
        Caption = "";
        Specials = "";
        Price = None;
        PublishedAt = None;
        ReceivedAt = None;
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
        Ignores: string
        Items: List<string>
        IsEnabled: bool }

let EmptySubscription =
    {   Name = "";
        Url = "";
        Ignores = "";
        Items = List();
        IsEnabled = true }

