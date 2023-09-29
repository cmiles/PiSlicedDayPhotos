using PiSlicedDayPhotos.Utility;

namespace PiSlicedDayPhotos.Test;

internal class CustomTimeTests
{
    [Test]
    public void CustomAmbiguousTimeTest()
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "3:45" } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 3, 45, 0)));
    }

    [Test]
    public void CustomNaturalLanguageNoonTimeTest()
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "noon" } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 12, 0, 0)));
    }

    [TestCase("Sunrise-124")]
    [TestCase("Sunrise-124m")]
    [TestCase("Sunrise - 124minutes")]
    [TestCase("Sunrise- 124min")]
    [TestCase("Sunrise -124")]
    [TestCase("-124Sunrise")]
    [TestCase("-124mSunrise")]
    [TestCase("-124min Sunrise")]
    [TestCase("-124 minutes Sunrise")]
    public void CustomSunriseMinusTimeTest(string sunriseMinus)
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = sunriseMinus } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 6, 10, 0)));
    }

    [TestCase("Sunrise+33")]
    [TestCase("Sunrise+33 m")]
    [TestCase("Sunrise + 33 minutes")]
    [TestCase("Sunrise+ 33min")]
    [TestCase("Sunrise +33")]
    [TestCase("+33Sunrise")]
    [TestCase("+33mSunrise")]
    [TestCase("+33min Sunrise")]
    [TestCase("+33 minutes Sunrise")]
    public void CustomSunrisePlusTimeTest(string sunriseMinus)
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = sunriseMinus } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 8, 47, 0)));
    }

    [TestCase("Sunset-24")]
    [TestCase("Sunset-24m")]
    [TestCase("Sunset - 24minutes")]
    [TestCase("Sunset- 24min")]
    [TestCase("Sunset -24")]
    [TestCase("-24Sunset")]
    [TestCase("-24mSunset")]
    [TestCase("-24min Sunset")]
    [TestCase("-24 minutes Sunset")]
    public void CustomSunsetMinusTimeTest(string sunsetMinus)
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = sunsetMinus } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 17, 10, 0)));
    }

    [TestCase("Sunset+1")]
    [TestCase("Sunset+1m")]
    [TestCase("Sunset + 1minutes")]
    [TestCase("Sunset+ 1min")]
    [TestCase("Sunset +1")]
    [TestCase("+1Sunset")]
    [TestCase("+1mSunset")]
    [TestCase("+1min Sunset")]
    [TestCase("+1 minutes Sunset")]
    public void CustomSunsetPlusTimeTest(string sunsetMinus)
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = sunsetMinus } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 17, 35, 0)));
    }

    [Test]
    public void CustomTimeTest()
    {
        var result = PhotographTimeTools.GetCustomTimes(
            new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "3:45 pm" } },
            new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0));

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Settings, Is.EqualTo("test return"));
        Assert.That(result.First().Time, Is.EqualTo(new DateTime(2023, 1, 15, 15, 45, 0)));
    }

    [Test]
    public void DecimalMinutesNotAllowedForSunrise()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "Sunrise +0.1" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void DecimalMinutesNotAllowedForSunset()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "-.99 Sunset" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void NoMinutesNotAllowedForSunrise()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "Sunrise" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void NoMinutesNotAllowedForSunset()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "Sunset" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void OneDayMinutesNotAllowedForSunrise()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "Sunrise1440" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void OneDayMinutesNotAllowedForSunset()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "-25999877 Sunset" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void ZeroMinutesNotAllowedForSunrise()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "Sunrise +0" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }

    [Test]
    public void ZeroMinutesNotAllowedForSunset()
    {
        Assert.Throws<Exception>(() =>
            PhotographTimeTools.GetCustomTimes(
                new List<CustomTimeAndSettings>() { new() { Settings = "test return", Time = "-0Sunset" } },
                new DateTime(2023, 1, 15, 8, 14, 0), new DateTime(2023, 1, 15, 17, 34, 0)));
    }
}