using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Serilog;

namespace PiSlicedDayPhotos.Utility;

public static class PhotographTimeTools
{
    public static List<CustomTimeAndSettingsTranslated> GetCustomTimes(List<CustomTimeAndSettings> customTimes,
        DateTime sunrise, DateTime sunset)
    {
        var returnList = new List<CustomTimeAndSettingsTranslated>();

        foreach (var loopCustomTime in customTimes)
        {
            if(string.IsNullOrWhiteSpace(loopCustomTime.Time)) continue;

            if (loopCustomTime.Time.Contains("Sunrise", StringComparison.OrdinalIgnoreCase))
            {
                var isMinus = loopCustomTime.Time.Contains("-", StringComparison.OrdinalIgnoreCase);
                var minutesString = loopCustomTime.Time
                    .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("+", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("Sunrise", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("minutes", String.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("min", String.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("m", String.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();
                
                if(string.IsNullOrWhiteSpace(minutesString)) throw new Exception($"The Custom Time {loopCustomTime} could not be parsed - no number of minutes +/- Sunrise were found.");

                if(!int.TryParse(minutesString, out var minutes)) throw new Exception($"The Custom Time {loopCustomTime} could not be parsed - the number of minutes +/- Sunrise could not be parsed as an integer.");

                if (minutes == 0)
                    throw new Exception(
                        $"The Custom Time {loopCustomTime} is not valid - the number of minutes +/- Sunrise must not be zero.");

                if (minutes >= 1440)
                    throw new Exception(
                        $"The Custom Time {loopCustomTime} is not valid - the number of minutes +/- Sunrise must be less than 1440 (less than one day).");

                returnList.Add(new CustomTimeAndSettingsTranslated { Time = isMinus ? sunrise.AddMinutes(-minutes) : sunrise.AddMinutes(minutes), Settings = loopCustomTime.Settings });

                continue;
            }

            if (loopCustomTime.Time.Contains("Sunset", StringComparison.OrdinalIgnoreCase))
            {
                var isMinus = loopCustomTime.Time.Contains("-", StringComparison.OrdinalIgnoreCase);
                var minutesString = loopCustomTime.Time
                    .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("+", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("Sunset", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("minutes", String.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("min", String.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("m", String.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (string.IsNullOrWhiteSpace(minutesString)) throw new Exception($"The Custom Time {loopCustomTime} could not be parsed - no number of minutes +/- Sunset were found.");

                if (!int.TryParse(minutesString, out var minutes)) throw new Exception($"The Custom Time {loopCustomTime} could not be parsed - the number of minutes +/- Sunset could not be parsed as an integer.");

                if (minutes == 0)
                    throw new Exception(
                        $"The Custom Time {loopCustomTime} is not valid - the number of minutes +/- Sunset must not be zero.");

                if (minutes >= 1440)
                    throw new Exception(
                        $"The Custom Time {loopCustomTime} is not valid - the number of minutes +/- Sunset must be less than 1440 (less than one day).");

                returnList.Add(new CustomTimeAndSettingsTranslated { Time = isMinus ? sunset.AddMinutes(-minutes) : sunset.AddMinutes(minutes), Settings = loopCustomTime.Settings });
                continue;
            }

            var timeParseResults = DateTimeRecognizer.RecognizeDateTime(loopCustomTime.Time, Culture.English);

            if (timeParseResults.Count == 0 || timeParseResults[0].Resolution.Count == 0 || timeParseResults.All(x => x.TypeName != "datetimeV2.time")) throw new Exception($"The Custom Time {loopCustomTime} could not be parsed.");

            var valuesFound = timeParseResults.First(x => x.TypeName == "datetimeV2.time").Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("value", out var customTimeRecognized) ||
                !TimeOnly.TryParse(customTimeRecognized, out var customTimeParsed))
            {
                throw new Exception($"The Custom Time {loopCustomTime} could not be parsed.");
                continue;
            }

            returnList.Add(new CustomTimeAndSettingsTranslated { Time = sunrise.Date.Add(customTimeParsed.ToTimeSpan()), Settings = loopCustomTime.Settings });
        }

        return returnList;
    }

    public static ScheduledPhoto PhotographTime(DateTime referenceDateTime,
        List<SunriseSunsetEntry> allSunriseSunsetEntries,
        int dayDivisions,
        int nightDivisions)
    {
        Console.WriteLine("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries = allSunriseSunsetEntries.Where(x =>
            x.ReferenceDate >= startSunriseSunsetSearch && x.ReferenceDate <= endSunriseSunsetSearch).ToList();

        var pastSunrise = sunriseSunsetEntries.Where(x => x.LocalSunrise < referenceDateTime)
            .MinBy(x => referenceDateTime.Subtract(x.LocalSunrise))!.LocalSunrise;
        var nextSunrise = sunriseSunsetEntries.Where(x => x.LocalSunrise > referenceDateTime)
            .MinBy(x => x.LocalSunrise.Subtract(referenceDateTime))!.LocalSunrise;

        var pastSunset = sunriseSunsetEntries.Where(x => x.LocalSunset < referenceDateTime)
            .MinBy(x => referenceDateTime.Subtract(x.LocalSunset))!.LocalSunset;
        var nextSunset = sunriseSunsetEntries.Where(x => x.LocalSunset > referenceDateTime)
            .MinBy(x => x.LocalSunset.Subtract(referenceDateTime))!.LocalSunset;

        var nightTime = pastSunset > pastSunrise;

        Console.WriteLine(
            $"Is Nighttime {nightTime} - Past Sunrise: {pastSunrise.ToShortOutput()} Past Sunset: {pastSunset.ToShortOutput()} -- Next Sunrise: {nextSunrise.ToShortOutput()} Next Sunset: {nextSunset.ToShortOutput()}",
            nightTime, pastSunrise, pastSunset, nextSunrise, nextSunset);

        if (nightTime)
        {
            var nightLength = nextSunrise.Subtract(pastSunset);
            var nightInterval = TimeSpan.FromSeconds(nightLength.TotalSeconds / (nightDivisions + 1));

            Console.WriteLine($"Night: Night Length: {nightLength:c} - Night Interval {nightInterval:c}");

            for (var i = 1; i <= nightDivisions; i++)
            {
                var nextIntervalTime = pastSunset.AddSeconds(nightInterval.TotalSeconds * i);

                if (referenceDateTime < nextIntervalTime)
                {
                    Console.WriteLine($" Returning {nextIntervalTime.ToShortOutput()} as next Photograph Time");
                    return new ScheduledPhoto { Kind = PhotoKind.Night, ScheduledTime = nextIntervalTime };
                }
            }

            Console.WriteLine($"Returning Next Sunrise - {nextSunrise.ToShortOutput()} - as next Photograph Time");

            return new ScheduledPhoto { Kind = PhotoKind.Sunrise, ScheduledTime = nextSunrise };
        }

        var dayLength = nextSunset.Subtract(pastSunrise);
        var dayInterval = TimeSpan.FromSeconds(dayLength.TotalSeconds / (dayDivisions + 1));

        Console.WriteLine($"Day: Day Length: {dayLength:c} - Day Interval {dayInterval:c}");

        for (var i = 1; i <= dayDivisions; i++)
        {
            var nextIntervalTime = pastSunrise.AddSeconds(dayInterval.TotalSeconds * i);

            if (referenceDateTime < nextIntervalTime)
            {
                Console.WriteLine($" Returning {nextIntervalTime.ToShortOutput()} as next Photograph Time");
                return new ScheduledPhoto { Kind = PhotoKind.Day, ScheduledTime = nextIntervalTime };
            }
        }

        Console.WriteLine($"Returning Next Sunset - {nextSunset.ToShortOutput()} - as next Photograph Time");

        return new ScheduledPhoto { Kind = PhotoKind.Sunset, ScheduledTime = nextSunset };
    }

    public static ScheduledPhoto PhotographTimeFromFile(DateTime referenceDateTime, string sunriseSunsetFileName,
        int dayDivisions,
        int nightDivisions)
    {
        Console.WriteLine("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries =
            SunriseSunsetsFromFile(sunriseSunsetFileName, startSunriseSunsetSearch, endSunriseSunsetSearch)
                .OrderBy(x => x.ReferenceDate).ToList();

        return PhotographTime(referenceDateTime, sunriseSunsetEntries, dayDivisions, nightDivisions);
    }

    public static ScheduledPhoto PhotographTimeFromString(DateTime referenceDateTime, string sunriseSunsetString,
        int dayDivisions,
        int nightDivisions)
    {
        Console.WriteLine("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries =
            SunriseSunsetsFromString(sunriseSunsetString, startSunriseSunsetSearch, endSunriseSunsetSearch)
                .OrderBy(x => x.ReferenceDate).ToList();

        return PhotographTime(referenceDateTime, sunriseSunsetEntries, dayDivisions, nightDivisions);
    }

    public static List<ScheduledPhoto> PhotographTimeSchedule(int days, DateTime startDateTime,
        List<SunriseSunsetEntry> sunriseSunsetEntries,
        int dayDivisions, int nightDivisions)
    {
        var endDateTime = startDateTime.AddDays(days);


        var returnList = new List<ScheduledPhoto>();

        var nextPhotographTime =
            PhotographTime(startDateTime, sunriseSunsetEntries, dayDivisions, nightDivisions);

        if (nextPhotographTime.ScheduledTime > endDateTime)
            return returnList;

        returnList.Add(nextPhotographTime);

        do
        {
            nextPhotographTime = PhotographTime(nextPhotographTime.ScheduledTime.AddSeconds(1), sunriseSunsetEntries,
                dayDivisions,
                nightDivisions);
            if (nextPhotographTime.ScheduledTime <= endDateTime) returnList.Add(nextPhotographTime);
        } while (nextPhotographTime.ScheduledTime <= endDateTime);

        return returnList;
    }

    public static List<ScheduledPhoto> PhotographTimeScheduleFromFile(int days, DateTime startDateTime,
        string sunriseSunsetFileName,
        int dayDivisions, int nightDivisions)
    {
        var entries = SunriseSunsetsFromFile(sunriseSunsetFileName, DateOnly.MinValue, DateOnly.MaxValue).ToList();

        return PhotographTimeSchedule(days, startDateTime, entries, dayDivisions, nightDivisions);
    }

    public static List<ScheduledPhoto> PhotographTimeScheduleFromString(int days, DateTime startDateTime,
        string sunriseSunsetString,
        int dayDivisions, int nightDivisions)
    {
        var entries = SunriseSunsetsFromString(sunriseSunsetString, DateOnly.MinValue, DateOnly.MaxValue).ToList();

        return PhotographTimeSchedule(days, startDateTime, entries, dayDivisions, nightDivisions);
    }

    public static List<SunriseSunsetEntry> SunriseSunsetsFromFile(string sunriseSunsetFileName, DateOnly startDate,
        DateOnly endDate)
    {
        var sunriseSunsetFileLines = File.ReadAllLines(sunriseSunsetFileName);

        Console.WriteLine($"Reading {sunriseSunsetFileLines.Length} Lines from {sunriseSunsetFileName}");

        return SunriseSunsetsFromLines(sunriseSunsetFileLines.ToList(), startDate, endDate);
    }

    public static List<SunriseSunsetEntry> SunriseSunsetsFromLines(List<string> sunriseSunsetLines, DateOnly startDate,
        DateOnly endDate)
    {
        var lineCounter = 0;

        Console.WriteLine($"Found {sunriseSunsetLines.Count} Lines in {sunriseSunsetLines}");

        var returnEntries = new List<SunriseSunsetEntry>();

        foreach (var loopLines in sunriseSunsetLines)
        {
            lineCounter++;

            if (string.IsNullOrWhiteSpace(loopLines)) continue;

            var lineParts = loopLines.Split(",");

            try
            {
                if (lineParts[0].Equals("Day", StringComparison.OrdinalIgnoreCase)) continue;

                var sunriseSunsetEntry = new SunriseSunsetEntry()
                {
                    ReferenceDate = DateOnly.FromDateTime(DateTime.Parse(lineParts[0])),
                    LocalSunrise = DateTime.Parse($"{lineParts[0]} {lineParts[1]}"),
                    LocalSunset = DateTime.Parse($"{lineParts[0]} {lineParts[2]}")
                };

                if (sunriseSunsetEntry.ReferenceDate >= startDate && sunriseSunsetEntry.ReferenceDate <= endDate)
                    returnEntries.Add(sunriseSunsetEntry);
            }
            catch (Exception e)
            {
                Log.ForContext("line", loopLines).Error(e,
                    "[Photograph Time Tools] Invalid Entry in Sunrise Sunset Lines - Line Number {lineNumber}",
                    lineCounter);
            }
        }

        return returnEntries;
    }

    public static List<SunriseSunsetEntry> SunriseSunsetsFromString(string sunriseSunsetString, DateOnly startDate,
        DateOnly endDate)
    {
        var sunriseSunsetStringLines =
            sunriseSunsetString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Console.WriteLine($"Reading {sunriseSunsetStringLines.Length} Lines from String");

        return SunriseSunsetsFromLines(sunriseSunsetStringLines.ToList(), startDate, endDate);
    }

    public static string ToShortOutput(this DateOnly toFormat)
    {
        return toFormat.ToString("M/d");
    }

    public static string ToShortOutput(this DateTime toFormat)
    {
        return toFormat.ToString("M/d h:mm tt");
    }
}