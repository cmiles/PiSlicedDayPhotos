using System.Diagnostics;
using System.Text.Json;
using CrypticWizard.RandomWordGenerator;
using PiSlicedDayPhotos.Utility;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("PiSlicedDayPhotos");


var settingsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "PiSlicedDaySettings.json"));

Log.Information("Setup - Settings File {settingsFile}", settingsFile.FullName);

if (!settingsFile.Exists)
{
    Log.Error(
        "Please include a settings file named 'PiSlicedDaySettings.json' in the program directory. Did not Find {settingsFile}",
        settingsFile.FullName);
    return;
}

PiSlicedDaySettings? settings = null;

Log.Information("Reading Setting File {settingsFile}", settingsFile.FullName);

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

Log.Information("Checking for Sunrise/Sunset File {sunriseSunsetFile}", sunriseSunsetCsv.FullName);

if (!sunriseSunsetCsv.Exists)
{
    Log.Error(
        "Please include a CSV file named 'SunriseAndSunset.csv' in the program directory. Did not Find {sunriseSunsetFile}",
        sunriseSunsetCsv.FullName);
    return;
}

Log.Information("Initializing Camera");

WordGenerator heartBeatWordGenerator = new();

Timer? heartBeatTimer = null;

Timer? mainLoop = null;

async void HandleTimerCallback(object? state)
{
    heartBeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);

    Console.WriteLine();

    var fileName = Path.Combine(settings.PhotoStorageDirectory,
        $"{settings.PhotoNamePrefix}{(string.IsNullOrWhiteSpace(settings.PhotoNamePrefix) ? "" : "-")}{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.jpg");

    Log.Information($"Taking Photo at {DateTime.Now:O} - {fileName}");

    var process = new Process();
    process.StartInfo.FileName = "libcamera-still";
    process.StartInfo.Arguments = $"-o {fileName}";
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    process.OutputDataReceived += (_, eventArgs) => Console.WriteLine(eventArgs.Data);
    process.ErrorDataReceived += (_, eventArgs) => Console.WriteLine(eventArgs.Data);
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    var newNextTime = SunriseSunsetTools.PhotographTime(DateTime.Now, settings.SunriseSunsetCsvFile, settings.DaySlices,
        settings.NightSlices);

    mainLoop?.Change(newNextTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);

    Console.WriteLine();
    Log.Information($"Next Scheduled Photo: {DateTime.Now:O}");

    Console.WriteLine();
    Console.Write("Heartbeat: ");

    heartBeatTimer?.Change(TimeSpan.Zero, TimeSpan.FromMinutes(15));
}

var nextTime =
    SunriseSunsetTools.PhotographTime(DateTime.Now, settings.SunriseSunsetCsvFile, settings.DaySlices,
        settings.NightSlices);

mainLoop = new Timer(HandleTimerCallback, null, nextTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);

Log.Information($"Next Scheduled Photo: {nextTime:O} - {nextTime.Subtract(DateTime.Now):g}");

Console.WriteLine();
Console.Write("Heartbeat words: ");

heartBeatTimer = new Timer((e) => { Console.Write($"{heartBeatWordGenerator.GetWord()} "); }, null, TimeSpan.Zero,
    TimeSpan.FromMinutes(15));

Console.Read();

await Log.CloseAndFlushAsync();