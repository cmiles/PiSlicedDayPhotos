using Serilog;

namespace PiSlicedDayPhotos.Utility;

public static class PhotographTimeTools
{
    public static DateTime PhotographTime(DateTime referenceDateTime, List<SunriseSunsetEntry> allSunriseSunsetEntries,
        int dayDivisions,
        int nightDivisions)
    {
        Log.Verbose("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries = allSunriseSunsetEntries.Where(x =>
                       x.ReferenceDate >= startSunriseSunsetSearch && x.ReferenceDate <= endSunriseSunsetSearch).ToList();

        var resultTextView = string.Join(Environment.NewLine, sunriseSunsetEntries.Select(x =>
            $"   Reference Date {x.ReferenceDate.ToShortOutput()}, Local Sunrise {x.LocalSunrise.ToShortOutput()}, Local Sunset {x.LocalSunset.ToShortOutput()}"));

        Log.ForContext(nameof(sunriseSunsetEntries), resultTextView).Verbose(
            $"Found {sunriseSunsetEntries.Count} Entries searching from {startSunriseSunsetSearch.ToShortOutput()} to {endSunriseSunsetSearch.ToShortOutput()}");

        var pastSunrise = sunriseSunsetEntries.Where(x => x.LocalSunrise < referenceDateTime)
            .MinBy(x => referenceDateTime.Subtract(x.LocalSunrise))!.LocalSunrise;
        var nextSunrise = sunriseSunsetEntries.Where(x => x.LocalSunrise > referenceDateTime)
            .MinBy(x => x.LocalSunrise.Subtract(referenceDateTime))!.LocalSunrise;

        var pastSunset = sunriseSunsetEntries.Where(x => x.LocalSunset < referenceDateTime)
            .MinBy(x => referenceDateTime.Subtract(x.LocalSunset))!.LocalSunset;
        var nextSunset = sunriseSunsetEntries.Where(x => x.LocalSunset > referenceDateTime)
            .MinBy(x => x.LocalSunset.Subtract(referenceDateTime))!.LocalSunset;

        var nightTime = pastSunset > pastSunrise;

        Log.Verbose(
            $"Is Nighttime {nightTime} - Past Sunrise: {pastSunrise.ToShortOutput()} Past Sunset: {pastSunset.ToShortOutput()} -- Next Sunrise: {nextSunrise.ToShortOutput()} Next Sunset: {nextSunset.ToShortOutput()}",
            nightTime, pastSunrise, pastSunset, nextSunrise, nextSunset);

        if (nightTime)
        {
            var nightLength = nextSunrise.Subtract(pastSunset);
            var nightInterval = TimeSpan.FromSeconds(nightLength.TotalSeconds / (nightDivisions + 1));

            Log.Verbose($"Night: Night Length: {nightLength:c} - Night Interval {nightInterval:c}");

            for (var i = 1; i <= nightDivisions; i++)
            {
                var nextIntervalTime = pastSunset.AddSeconds(nightInterval.TotalSeconds * i);

                if (referenceDateTime < nextIntervalTime)
                {
                    Console.WriteLine($" Returning {nextIntervalTime.ToShortOutput()} as next Photograph Time");
                    return nextIntervalTime;
                }
            }

            Log.Information($"Returning Next Sunrise - {nextSunrise.ToShortOutput()} - as next Photograph Time");

            return nextSunrise;
        }

        var dayLength = nextSunset.Subtract(pastSunrise);
        var dayInterval = TimeSpan.FromSeconds(dayLength.TotalSeconds / (dayDivisions + 1));

        Log.Verbose($"Day: Day Length: {dayLength:c} - Day Interval {dayInterval:c}");

        for (var i = 1; i <= dayDivisions; i++)
        {
            var nextIntervalTime = pastSunrise.AddSeconds(dayInterval.TotalSeconds * i);

            if (referenceDateTime < nextIntervalTime)
            {
                Console.WriteLine($" Returning {nextIntervalTime.ToShortOutput()} as next Photograph Time");
                return nextIntervalTime;
            }
        }

        Log.Information($"Returning Next Sunset - {nextSunset.ToShortOutput()} - as next Photograph Time");

        return nextSunset;
    }

    public static DateTime PhotographTimeFromFile(DateTime referenceDateTime, string sunriseSunsetFileName,
        int dayDivisions,
        int nightDivisions)
    {
        Log.Verbose("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries =
            SunriseSunsetsFromFile(sunriseSunsetFileName, startSunriseSunsetSearch, endSunriseSunsetSearch)
                .OrderBy(x => x.ReferenceDate).ToList();

        return PhotographTime(referenceDateTime, sunriseSunsetEntries, dayDivisions, nightDivisions);
    }

    public static DateTime PhotographTimeFromString(DateTime referenceDateTime, string sunriseSunsetString,
        int dayDivisions,
        int nightDivisions)
    {
        Log.Verbose("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries =
            SunriseSunsetsFromString(sunriseSunsetString, startSunriseSunsetSearch, endSunriseSunsetSearch)
                .OrderBy(x => x.ReferenceDate).ToList();

        return PhotographTime(referenceDateTime, sunriseSunsetEntries, dayDivisions, nightDivisions);
    }

    public static List<DateTime> PhotographTimeSchedule(int days, DateTime startDateTime,
        List<SunriseSunsetEntry> sunriseSunsetEntries,
        int dayDivisions, int nightDivisions)
    {
        var endDateTime = startDateTime.AddDays(days);


        var returnList = new List<DateTime>();

        var nextPhotographTime =
            PhotographTime(startDateTime, sunriseSunsetEntries, dayDivisions, nightDivisions);

        if (nextPhotographTime > endDateTime)
            return returnList;

        returnList.Add(nextPhotographTime);

        do
        {
            nextPhotographTime = PhotographTime(nextPhotographTime.AddSeconds(1), sunriseSunsetEntries,
                dayDivisions,
                nightDivisions);
            if (nextPhotographTime <= endDateTime) returnList.Add(nextPhotographTime);
        } while (nextPhotographTime <= endDateTime);

        return returnList;
    }

    public static List<DateTime> PhotographTimeScheduleFromFile(int days, DateTime startDateTime,
        string sunriseSunsetFileName,
        int dayDivisions, int nightDivisions)
    {
        var entries = SunriseSunsetsFromFile(sunriseSunsetFileName, DateOnly.MinValue, DateOnly.MaxValue).ToList();

        return PhotographTimeSchedule(days, startDateTime, entries, dayDivisions, nightDivisions);
    }

    public static List<DateTime> PhotographTimeScheduleFromString(int days, DateTime startDateTime,
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
                    "Invalid Entry in Sunrise Sunset Lines - Line Number {lineNumber}",
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