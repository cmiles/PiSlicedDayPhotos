using System.Text.Json;
using PiSlicedDayPhotos.Utility;
using Serilog;

var settingsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "PiSlicedDaySettings.json"));

if (!settingsFile.Exists)
{
    Log.Error(
        "Please include a settings file named 'PiSlicedDaySettings.json' in the program directory. Did not Find {settingsFile}",
        settingsFile.FullName);
    return;
}

PiSlicedDaySettings? settings = null;

try
{
    settings = JsonSerializer.Deserialize<PiSlicedDaySettings>(File.ReadAllText(settingsFile.FullName));
}
catch (Exception e)
{
    Log.Error(e, "Problem Reading Settings File {settingsFile}", settingsFile.FullName);
    return;
}

if (settings == null)
{
    Log.Error("Reading Settings File {settingsFile} returned null - empty or invalid settings or file format?",
        settingsFile.FullName);
    return;
}

var sunriseSunsetCsv = new FileInfo(Path.Combine(AppContext.BaseDirectory, "SunriseAndSunset.csv"));

if (!sunriseSunsetCsv.Exists)
{
    Log.Error(
        "Please include a CSV file named 'SunriseAndSunset.csv' in the program directory. Did not Find {sunriseSunsetFile}",
        sunriseSunsetCsv.FullName);
    return;
}