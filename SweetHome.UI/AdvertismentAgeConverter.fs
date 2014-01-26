namespace SweetHome.UI

open System.Windows
open System.Windows.Data
open System.Windows.Converters

type AdvertismentAgeConverter() =
    let asObj s = s :> obj

    interface IValueConverter with
        member x.Convert(value, targetType, parameter, culture) =
            match value with
            | :? Model.Advertisment as ad ->
                let dt = ad.LastAppearedAt.Date - ad.FirstAppearedAt.Date
                let s = 
                    if dt.TotalDays > 0.0
                    then sprintf "+%i" (int dt.TotalDays)
                    else ""
                asObj s
            | _ -> asObj ""

        member x.ConvertBack(value, targetType, parameter, culture) =
            value


