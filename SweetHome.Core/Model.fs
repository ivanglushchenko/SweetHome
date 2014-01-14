namespace SweetHome.Core

open System

module Model =
    
    type FeedItem =
        {   Id: string
            FeedId: string
            Url: string
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
            SimiliarItems: FeedItem list
            SimiliarItemsTotal: int
            SimiliarItemsToday: int
            FirstAppearanceOffset: int
            Phones: string list
            Year: int }

    type Feed =
        {   Id: string
            Name: string
            Url: string
            Keywords: string
            Ignores: string
            MinPrice: int
            MaxPrice: int
            Items: FeedItem list
            IsEnabled: bool }