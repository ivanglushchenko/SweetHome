module Parsing

open System
open System.Text
open System.Collections.Generic
open System.Text.RegularExpressions
open Model

let reUnicode = new Regex("""&#x[0-9]+;|&[a-zA-Z]+;""");
let rePage = new Regex("""<p class="row"[^>]*>(\s)*<a[^>]*>(\s)*</a>(\s)*<span class="star"></span>(\s)*<span class="pl">(\s)*<span class="date">(?<date>[^<]*)</span>(\s)*<a href="(?<url>[^"]*)">(?<caption>[^<]*)</a>(\s)*</span>(\s)*<span[^>]*>(\s)*<span class="price">(?<price>[^<]*)</span>(\s)*/(?<bd>[^<]*)(\s)*-(\s)*<span class="pnr">((\s)*<small>(?<place>[^<]*)</small>(\s)*)?""")
let reAdvertismentPostedDate = new Regex("""<p class="postinginfo">posted: <time datetime="(?<date>[^"]*)""")
let reAdvertismentUpdatedDate = new Regex("""<p class="postinginfo">updated: <time datetime="(?<date>[^"]*)""")
let reAddress = new Regex("""<p class="mapaddress">(?<address>[^<]*)<small>[^<]*<a target="_blank" href="(?<url>[^"]*)""")
let reBedroom = new Regex("""<p class="attrgroup"><span><b>(?<bd>[0-9]+)</b>BR""")
let reCLTafs = new Regex("""<!-- START CLTAGS -->(?<body>[\s\S]*)<!-- END CLTAGS -->""")

let trim (s: string) =
    if s = null
    then s
    else s.Trim()

let toLower (s: string) = 
    if s = null
    then s
    else s.ToLower()

let toAscii (s: string) = 
    let sb = StringBuilder s
    for m in reUnicode.Matches s do
        sb.Replace(m.Value, " ") |> ignore
    let bytes = UnicodeEncoding.Unicode.GetBytes (sb.ToString())
    let convertedBytes = ASCIIEncoding.Convert(UnicodeEncoding.Unicode, ASCIIEncoding.ASCII, bytes)
    ASCIIEncoding.ASCII.GetString convertedBytes

let toText (s: string) =
    let letters = s |> Seq.map (fun c -> if Char.IsLetter c || Char.IsDigit c || c = '/' || c = '-' || c = '$' || c = '%' then c else ' ') |> Seq.toArray
    new String(letters)

let trimSpaces (s: string) =
    let letters =
        seq {   let isLastWhitespace = ref false
                for c in s do
                    if c = ' '
                    then
                        if !isLastWhitespace = false
                        then 
                            isLastWhitespace := true
                            yield c
                    else
                        isLastWhitespace := false
                        yield c } |> Seq.toArray
    new String(letters)

let beautify =
    toText >> toLower >> trimSpaces >> trim

let tryParseDate s =
    let success, res = DateTime.TryParse s
    if success
    then
        if res > DateTime.Now
        then Some (res.AddYears -1)
        else Some res
    else None

let tryParseFloat s =
    let success, res = Double.TryParse s
    if success
    then Some res
    else None

let tryParseInt s =
    let success, res = Int32.TryParse s
    if success
    then Some res
    else None
    
let parsePage (subscribtion, page) =
    let cleanedPage = toAscii page
    let advertisments = 
        seq { for m in rePage.Matches cleanedPage do
                let url = m.Groups.["url"].Value
                let publishedAt = tryParseDate m.Groups.["date"].Value
                if url <> null && publishedAt.IsSome then
                    yield
                        { EmptyAdvertisment with
                            Url = subscribtion.BaseAddress + url
                            FirstAppearedAt = publishedAt.Value
                            LastAppearedAt = publishedAt.Value
                            Caption = beautify m.Groups.["caption"].Value
                            Price = tryParseInt m.Groups.["price"].Value
                            Place = beautify m.Groups.["place"].Value
                            Origins = HashSet<string>([ subscribtion.Name ])
                            Urls = HashSet<string>([ subscribtion.BaseAddress + url ])
                            IsNew = true } } |> List.ofSeq
    advertisments

let enrichAdvertisment (advertisment, content) =
    let update adv (re: Regex) (groupName: string) f =
        let m = re.Match content
        if m.Success
        then f adv m.Groups.[groupName].Value
        else adv

    let ret =
        Array.fold 
            (fun acc (re, groupName, f) -> update acc re groupName f) 
            advertisment
            [| reAdvertismentPostedDate, "date", fun t v -> let d = tryParseDate v |> Option.get in { t with FirstAppearedAt = d; LastAppearedAt = d }
               reAdvertismentUpdatedDate, "date", fun t v -> { t with LastAppearedAt = tryParseDate v |> Option.get }
               reAddress, "address", fun t v -> { t with Address = (trim >> toLower) v }
               reAddress, "url", fun t v -> { t with AddressUrl = (trim >> toLower) v }
               reBedroom, "bd", fun t v -> { t with Bedrooms = tryParseInt v }
               reCLTafs, "body", fun t v -> { t with CLTags = beautify v } |]
    ret