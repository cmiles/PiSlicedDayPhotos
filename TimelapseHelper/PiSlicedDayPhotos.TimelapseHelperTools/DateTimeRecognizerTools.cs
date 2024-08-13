using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;

namespace PiSlicedDayPhotos.TimelapseHelperTools;

public static class DateTimeRecognizerTools
{
    public static DateTime? GetDateTime(string input, bool returnFirstOfRange)
    {
        var dateTimeParse = DateTimeRecognizer.RecognizeDateTime(input, Culture.English,
            DateTimeOptions.None, DateTime.Now);

        if (dateTimeParse.Count == 0 || dateTimeParse[0].Resolution.Count == 0) return null;

        if (dateTimeParse[0].TypeName == "datetimeV2.daterange")
        {
            var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("start", out var searchStartDateTimeString) ||
                !DateTime.TryParse(searchStartDateTimeString, out var searchStartDateTime) ||
                !valuesDictionary[0].TryGetValue("end", out var searchEndDateTimeString) ||
                !DateTime.TryParse(searchEndDateTimeString, out var searchEndDateTime))
                return null;

            return returnFirstOfRange ? searchStartDateTime : searchEndDateTime;
        }

        if (dateTimeParse[0].TypeName == "datetimeV2.date")
        {
            var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                !DateTime.TryParse(searchDateTimeString, out var searchDateTime))
                return null;

            return searchDateTime;
        }

        if (dateTimeParse[0].TypeName == "datetimeV2.datetime")
        {
            var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                !DateTime.TryParse(searchDateTimeString, out var searchDateTime))
                return null;

            return searchDateTime;
        }

        if (dateTimeParse[0].TypeName == "datetimeV2.time")
        {
            var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                !DateTime.TryParse(searchDateTimeString, out var searchTime))
                return null;

            return searchTime;
        }

        return null;
    }
}