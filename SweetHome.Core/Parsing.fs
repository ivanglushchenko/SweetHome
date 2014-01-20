module Parsing

open System
open System.Text
open System.Collections.Generic
open System.Text.RegularExpressions
open Model

let reUnicode = new Regex("""&#x[0-9]+;""");
let rePage = new Regex("""<p class="row"[^>]*>(\s)*<a[^>]*>(\s)*</a>(\s)*<span class="star"></span>(\s)*<span class="pl">(\s)*<span class="date">(?<date>[^<]*)</span>(\s)*<a href="(?<url>[^"]*)">(?<caption>[^<]*)</a>(\s)*</span>(\s)*<span[^>]*>(\s)*<span class="price">(?<price>[^<]*)</span>(\s)*/(?<bd>[^<]*)(\s)*-(\s)*<span class="pnr">((\s)*<small>(?<place>[^<]*)</small>(\s)*)?""")
let reAdvertismentPostedDate = new Regex("""<p class="postinginfo">posted: <time datetime="(?<date>[^"]*)""")

let trim (s: string) = 
    s.Trim()

let toLower (s: string) = 
    s.ToLower()

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

let tryParseBedrooms s =
    let ts = (trim >> toLower) s
    if ts.EndsWith "br"
    then tryParseInt (ts.Substring(0, ts.Length - 2))
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
                            Price = tryParseFloat m.Groups.["price"].Value
                            Bedrooms = tryParseBedrooms m.Groups.["bd"].Value
                            Place = beautify m.Groups.["place"].Value
                            Origins = HashSet<string>([ subscribtion.Name ]) } } |> List.ofSeq
    advertisments

let enrichAdvertisment (advertisment, content) =
    let m = reAdvertismentPostedDate.Match content
    if m.Success
    then match tryParseDate m.Groups.["date"].Value with | Some d -> { advertisment with FirstAppearedAt = d } | None -> advertisment
    else advertisment