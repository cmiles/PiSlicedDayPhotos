using System.Diagnostics;
using System.Text.Json;
using PiSlicedDayPhotos.Utility;
using Serilog;

namespace PiSlicedDayPhotos;

public class PhotoWorker : BackgroundService
{
    private DateTime _nextTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settingsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "PiSlicedDaySettings.json"));

        Log.Information("Setup - Settings File {settingsFile}", settingsFile.FullName);

        if (!settingsFile.Exists)
        {
            Log.Error(
                "Please include a settings file named 'PiSlicedDaySettings.json' in the program directory. Did not Find {settingsFile}",
                settingsFile.FullName);
            return;
        }

        PiSlicedDaySettings? settings;

        Log.Information("Reading Setting File {settingsFile}", settingsFile.FullName);

        try
        {
            settings = JsonSerializer.Deserialize<PiSlicedDaySettings>(
                await File.ReadAllTextAsync(settingsFile.FullName, stoppingToken));
        }
        catch (Exception e)
        {
            Log.Error(e, "Problem Reading Settings File {settingsFile}", settingsFile.FullName);
            return;
        }

        if (settings == null)
        {
            Log.Error(
                "Reading Settings File {settingsFile} returned null - empty or invalid settings or file format?",
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
            await process.WaitForExitAsync(stoppingToken);

            _nextTime = SunriseSunsetTools.PhotographTime(DateTime.Now, settings.SunriseSunsetCsvFile,
                settings.DaySlices,
                settings.NightSlices);

            mainLoop?.Change(_nextTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);

            Console.WriteLine();
            Log.Information($"Next Scheduled Photo: {DateTime.Now:O}");

            Console.WriteLine();
            Console.Write("Heartbeat: ");

            heartBeatTimer?.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        _nextTime = SunriseSunsetTools.PhotographTime(DateTime.Now, settings.SunriseSunsetCsvFile, settings.DaySlices,
            settings.NightSlices);

        mainLoop = new Timer(HandleTimerCallback, null, _nextTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);

        Log.Information($"Next Scheduled Photo: {_nextTime:O} - {_nextTime.Subtract(DateTime.Now):g}");

        heartBeatTimer = new Timer((_) => { Console.WriteLine($"Photo in {_nextTime.Subtract(DateTime.Now):c}"); },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

        await stoppingToken.WhenCancelled();

        await Log.CloseAndFlushAsync();
    }
}