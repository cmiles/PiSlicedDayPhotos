using Serilog;

namespace PiSlicedDayPhotos.Utility;

public static class SunriseSunsetTools
{
    public static DateTime PhotographTime(DateTime referenceDateTime, string sunriseSunsetFileName, int dayDivisions,
        int nightDivisions)
    {
        var startSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(-1));
        var endSunriseSunsetSearch = DateOnly.FromDateTime(referenceDateTime.Date.AddDays(1));

        var sunriseSunsetEntries =
            SunriseSunsetsFromFile(sunriseSunsetFileName, startSunriseSunsetSearch, endSunriseSunsetSearch)
                .OrderBy(x => x.ReferenceDate).ToList();

        Console.WriteLine(
            $"Found {sunriseSunsetEntries.Count} Entries searching {sunriseSunsetFileName} from {startSunriseSunsetSearch} to {endSunriseSunsetSearch}");
        sunriseSunsetEntries.ForEach(x =>
            Console.WriteLine(
                $"   Reference Date {x.ReferenceDate}, Local Sunrise {x.LocalSunrise}, Local Sunset {x.LocalSunset}"));

        var pastSunrise = sunriseSunsetEntries.Where(x => x.LocalSunrise < referenceDateTime)
            .MinBy(x => referenceDateTime.Subtract(x.LocalSunrise))!.LocalSunrise;
        var nextSunrise = sunriseSunsetEntries.Where(x => x.LocalSunrise > referenceDateTime)
            .MinBy(x => x.LocalSunrise.Subtract(referenceDateTime))!.LocalSunrise;

        var pastSunset = sunriseSunsetEntries.Where(x => x.LocalSunset < referenceDateTime)
            .MinBy(x => referenceDateTime.Subtract(x.LocalSunset))!.LocalSunset;
        var nextSunset = sunriseSunsetEntries.Where(x => x.LocalSunset > referenceDateTime)
            .MinBy(x => x.LocalSunset.Subtract(referenceDateTime))!.LocalSunset;

        var nightTime = pastSunset > pastSunrise;

        Console.WriteLine($"Nighttime: {nightTime}");
        Console.WriteLine($"  Past Sunrise: {pastSunrise}");
        Console.WriteLine($"  Past Sunset: {pastSunset}");
        Console.WriteLine($"  Next Sunrise: {nextSunrise}");
        Console.WriteLine($"  Next Sunset: {nextSunset}");

        if (nightTime)
        {
            var nightLength = nextSunrise.Subtract(pastSunset);
            var nightInterval = TimeSpan.FromSeconds(nightLength.Seconds / nightDivisions);

            Console.WriteLine($"Night Length: {nightLength:g} - Night Interval {nightInterval:g}");

            for (var i = 0; i < nightDivisions; i++)
            {
                var nextIntervalTime = pastSunset.AddSeconds(nightInterval.Seconds * (i + 1));

                if (referenceDateTime < nextIntervalTime) return nextIntervalTime;
            }

            Console.WriteLine($"Returning {nextSunrise} as next Photograph Time");

            return nextSunrise;
        }

        var dayLength = nextSunset.Subtract(pastSunrise);
        var dayInterval = TimeSpan.FromSeconds(dayLength.Seconds / dayDivisions);

        Console.WriteLine($"Day Length: {dayLength:g} - Day Interval {dayInterval:g}");

        for (var i = 0; i < dayDivisions; i++)
        {
            var nextIntervalTime = pastSunrise.AddSeconds(dayInterval.Seconds * (i + 1));

            if (referenceDateTime < nextIntervalTime) return nextIntervalTime;
        }

        Console.WriteLine($"Returning {nextSunset} as next Photograph Time");

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
                var sunriseSunsetEntry = new SunriseSunsetEntry()
                {
                    ReferenceDate = DateOnly.FromDateTime(DateTime.Parse(lineParts[0])),
                    LocalSunrise = DateTime.Parse(lineParts[1]),
                    LocalSunset = DateTime.Parse(lineParts[2])
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