using PiSlicedDayPhotos.Utility;

namespace PiSlicedDayPhotos.Test;

public class ScheduleTests
{
    public string SunriseSunsetLines =>
        """
        DAY,SUNRISE,SUNSET
        1/15/2023,08:14:00-0700,17:34:00-0700
        1/16/2023,08:14:00-0700,17:35:00-0700
        1/17/2023,08:14:00-0700,17:35:00-0700
        1/18/2023,08:13:00-0700,17:36:00-0700
        1/19/2023,08:13:00-0700,17:37:00-0700
        1/20/2023,08:14:00-0700,17:38:00-0700
        1/21/2023,08:13:00-0700,17:39:00-0700
        1/22/2023,08:13:00-0700,17:40:00-0700
        1/23/2023,08:12:00-0700,17:41:00-0700
        1/24/2023,08:12:00-0700,17:42:00-0700
        1/25/2023,08:12:00-0700,17:43:00-0700
        1/26/2023,08:11:00-0700,17:44:00-0700
        1/27/2023,08:11:00-0700,17:45:00-0700
        1/28/2023,08:11:00-0700,17:46:00-0700
        1/29/2023,08:10:00-0700,17:46:00-0700
        1/30/2023,08:09:00-0700,17:47:00-0700
        1/31/2023,08:10:00-0700,17:48:00-0700
        2/1/2023,08:09:00-0700,17:50:00-0700
        2/2/2023,08:08:00-0700,17:51:00-0700
        2/3/2023,08:09:00-0700,17:51:00-0700
        2/4/2023,08:08:00-0700,17:53:00-0700
        2/5/2023,08:07:00-0700,17:54:00-0700
        2/6/2023,08:07:00-0700,17:55:00-0700
        2/7/2023,08:07:00-0700,17:56:00-0700
        2/8/2023,08:06:00-0700,17:57:00-0700
        2/9/2023,08:05:00-0700,17:58:00-0700
        2/10/2023,08:04:00-0700,17:59:00-0700
        2/11/2023,08:03:00-0700,18:01:00-0700
        2/12/2023,08:02:00-0700,18:01:00-0700
        2/13/2023,08:01:00-0700,18:02:00-0700
        2/14/2023,07:59:00-0700,18:03:00-0700
        2/15/2023,07:58:00-0700,18:04:00-0700
        """;

    [Test]
    public void NextPhotoTimesWithOneDivisions()
    {
        var entries = PhotographTimeTools
            .SunriseSunsetsFromString(SunriseSunsetLines, DateOnly.MinValue, DateOnly.MaxValue).ToList();

        var schedule = PhotographTimeTools.PhotographTimeSchedule(2, new DateTime(2023, 1, 22), entries, 1, 1);

        var expectedStringResults = """
                                    1/22/2023 12:56:00 AM
                                    1/22/2023 8:13:00 AM
                                    1/22/2023 12:56:30 PM
                                    1/22/2023 5:40:00 PM
                                    1/23/2023 12:56:00 AM
                                    1/23/2023 8:12:00 AM
                                    1/23/2023 12:56:30 PM
                                    1/23/2023 5:41:00 PM
                                    """;

        var expectedDateTimeResults = expectedStringResults
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(DateTime.Parse).ToList();

        Assert.That(schedule.Count, Is.EqualTo(8));
        Assert.That(schedule.Select(x => x.ScheduledTime).ToList(), Is.EquivalentTo(expectedDateTimeResults));
        Assert.That(schedule[0].Kind, Is.EqualTo(PhotoKind.Night));
        Assert.That(schedule[1].Kind, Is.EqualTo(PhotoKind.Sunrise));
        Assert.That(schedule[2].Kind, Is.EqualTo(PhotoKind.Day));
        Assert.That(schedule[3].Kind, Is.EqualTo(PhotoKind.Sunset));
        Assert.That(schedule[4].Kind, Is.EqualTo(PhotoKind.Night));
        Assert.That(schedule[5].Kind, Is.EqualTo(PhotoKind.Sunrise));
        Assert.That(schedule[6].Kind, Is.EqualTo(PhotoKind.Day));
        Assert.That(schedule[7].Kind, Is.EqualTo(PhotoKind.Sunset));
    }

    [Test]
    public void NextPhotoTimesWithZeroDivisions()
    {
        var entries = PhotographTimeTools
            .SunriseSunsetsFromString(SunriseSunsetLines, DateOnly.MinValue, DateOnly.MaxValue).ToList();

        var schedule = PhotographTimeTools.PhotographTimeSchedule(6, new DateTime(2023, 1, 22), entries, 0, 0);

        Assert.That(schedule.Count, Is.EqualTo(12));
        Assert.That(schedule.First().ScheduledTime, Is.EqualTo(DateTime.Parse("1/22/2023 08:13:00-0700")));
        Assert.That(schedule.Last().ScheduledTime, Is.EqualTo(DateTime.Parse("1/27/2023 17:45:00-0700")));
    }

    [Test]
    public void TotalNumberOfEntries()
    {
        var entries = PhotographTimeTools
            .SunriseSunsetsFromString(SunriseSunsetLines, DateOnly.MinValue, DateOnly.MaxValue).ToList();
        Assert.That(entries.Count, Is.EqualTo(32));
    }
}