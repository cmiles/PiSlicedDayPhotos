using Serilog;

namespace PiSlicedDayPhotos.Utility;

public static class SunriseSunsetTools
{
    public static DateTime PhotographTime(DateTime referenceDateTime, string sunriseSunsetFileName, int dayDivisions,
        int nightDivisions)
    {
        Log.Verbose("Starting Next Photo Time Search");
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries =
            SunriseSunsetsFromFile(sunriseSunsetFileName, startSunriseSunsetSearch, endSunriseSunsetSearch)
                .OrderBy(x => x.ReferenceDate).ToList();

        var resultTextView = string.Join(Environment.NewLine, sunriseSunsetEntries.Select(x =>
            $"   Reference Date {x.ReferenceDate}, Local Sunrise {x.LocalSunrise}, Local Sunset {x.LocalSunset}"));

        Log.ForContext(nameof(sunriseSunsetEntries), resultTextView).Verbose(
            $"Found {sunriseSunsetEntries.Count} Entries searching {sunriseSunsetFileName} from {startSunriseSunsetSearch} to {endSunriseSunsetSearch}");

        Console.WriteLine(resultTextView);

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
            $"Is Nighttime {nightTime} - Past Sunrise: {pastSunrise} Past Sunset: {pastSunset} -- Next Sunrise: {nextSunrise} Next Sunset: {nextSunset}",
            nightTime, pastSunrise, pastSunset, nextSunrise, nextSunset);

        if (nightTime)
        {
            var nightLength = nextSunrise.Subtract(pastSunset);
            var nightInterval = TimeSpan.FromSeconds(nightLength.TotalSeconds / nightDivisions);

            Log.Verbose($"Night: Night Length: {nightLength:g} - Night Interval {nightInterval:g}");

            for (var i = 0; i < nightDivisions; i++)
            {
                var nextIntervalTime = pastSunset.AddSeconds(nightInterval.TotalSeconds * (i + 1));

                if (referenceDateTime < nextIntervalTime) return nextIntervalTime;
            }

            Log.Information($"Returning {nextSunrise} as next Photograph Time");

            return nextSunrise;
        }

        var dayLength = nextSunset.Subtract(pastSunrise);
        var dayInterval = TimeSpan.FromSeconds(dayLength.TotalSeconds / dayDivisions);

        Log.Verbose($"Day: Day Length: {dayLength:g} - Day Interval {dayInterval:g}");

        for (var i = 0; i < dayDivisions; i++)
        {
            var nextIntervalTime = pastSunrise.AddSeconds(dayInterval.TotalSeconds * (i + 1));

            Console.WriteLine($"  Checking Next Time of {nextIntervalTime} against reference of {referenceDateTime}");

            if (referenceDateTime < nextIntervalTime)
            {
                Console.WriteLine($" Returning {nextIntervalTime} as next Photograph Time");
                return nextIntervalTime;
            }
        }

        Log.Information($"Returning {nextSunset} as next Photograph Time");

        return nextSunset;
    }

    public static List<SunriseSunsetEntry> SunriseSunsetsFromFile(string sunriseSunsetFileName, DateOnly startDate,
        DateOnly endDate)
    {
        var sunriseSunsetFileLines = File.ReadAllLines(sunriseSunsetFileName);

        var lineCount = 0;

        var returnEntries = new List<SunriseSunsetEntry>();

        foreach (var loopLines in sunriseSunsetFileLines)
        {
            lineCount++;

            if (lineCount % 500 == 0)
                Console.WriteLine($"Sunrise Sunsets From File - {lineCount} of {sunriseSunsetFileLines.Length}");

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
                    "Invalid Entry in Sunrise Sunset File {sunriseSunsetFile} - Line Number {lineNumber}",
                    sunriseSunsetFileName, lineCount);
            }
        }

        return returnEntries;
    }
}