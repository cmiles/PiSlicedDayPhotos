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

        Timer? heartBeatWatchDogTimer = null;

        Timer? mainLoop = null;

        async void TakePhotographExceptionTrapWrapperCallback(object? state)
        {
            try
            {
                await TakePhotograph();
            }
            catch (Exception e)
            {
                Log.Error(e, "Main Photo Loop Error");

                var errorImageFileName = Path.Combine(settings.PhotoStorageDirectory,
                    $"Error-{settings.PhotoNamePrefix}{(string.IsNullOrWhiteSpace(settings.PhotoNamePrefix) ? "" : "-")}{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.jpg");

                await ExceptionTools.WriteExceptionToImage(e, errorImageFileName, settings.LogFullExceptionsToImages);
            }
        }

        async Task TakePhotograph()
        {
            //Give the PhotoTakerCallback a chance to run without being interrupted by the watchdog
            heartBeatWatchDogTimer?.Change(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1));

            Console.WriteLine();

            var currentPhotoDateTime = _nextTime;

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

            _nextTime = PhotographTimeTools.PhotographTimeFromFile(DateTime.Now, settings.SunriseSunsetCsvFile,
                settings.DaySlices,
                settings.NightSlices);

            if (currentPhotoDateTime.Day != _nextTime.Day)
            {
                var upcomingSchedule = PhotographTimeTools.PhotographTimeScheduleFromFile(2, currentPhotoDateTime,
                    settings.SunriseSunsetCsvFile,
                    settings.DaySlices,
                    settings.NightSlices);

                var scheduleDayGroup = upcomingSchedule.GroupBy(x => x.Date).ToList();

                scheduleDayGroup.ForEach(x =>
                    Console.WriteLine(
                        $"Schedule for {x.First():M/d}: {string.Join(", ", x.Select(y => y.ToString("h:mm tt")))}"));
            }

            mainLoop?.Change(_nextTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);

            Console.WriteLine();

            //There could be a slightly awkward interaction with the timing change at the top of this method TO
            //delay the watch dog until after the photo run - but in general the assumption is that the delay
            //should be more than enough time to execute the photo taking process.
            heartBeatWatchDogTimer?.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        _nextTime = PhotographTimeTools.PhotographTimeFromFile(DateTime.Now, settings.SunriseSunsetCsvFile,
            settings.DaySlices,
            settings.NightSlices);

        var upcomingSchedule = PhotographTimeTools.PhotographTimeScheduleFromFile(2, DateTime.Now,
            settings.SunriseSunsetCsvFile,
            settings.DaySlices,
            settings.NightSlices);

        var scheduleDayGroup = upcomingSchedule.GroupBy(x => x.Date).ToList();

        scheduleDayGroup.ForEach(x =>
            Console.WriteLine(
                $"Schedule for {x.First():M/d}: {string.Join(", ", x.Select(y => y.ToString("h:mm tt")))}"));

        mainLoop = new Timer(TakePhotographExceptionTrapWrapperCallback, null, _nextTime.Subtract(DateTime.Now),
            Timeout.InfiniteTimeSpan);

        Log.Information($"Next Scheduled Photo: {_nextTime:O} - {_nextTime.Subtract(DateTime.Now):g}");

        heartBeatWatchDogTimer = new Timer((_) =>
            {
                var timeUntilNextPhoto = _nextTime.Subtract(DateTime.Now);

                if (timeUntilNextPhoto.TotalSeconds >= 0)
                {
                    Console.WriteLine($"Photo in {_nextTime.Subtract(DateTime.Now):c}");
                    return;
                }

                if (timeUntilNextPhoto.TotalMinutes <= -5)
                {
                    var nextTime = PhotographTimeTools.PhotographTimeFromFile(DateTime.Now,
                        settings.SunriseSunsetCsvFile, settings.DaySlices,
                        settings.NightSlices);

                    Log.ForContext("hint",
                            "A past next photo time can result from errors in the main photo loop - there should be Log entries prior to this entry that help diagnose any problems.")
                        .Warning(
                            $"Photo time of {_nextTime.ToShortOutput()} is in the Past (current time {DateTime.Now.ToShortOutput()}) - resetting Next Time to {nextTime.ToShortOutput()}");

                    _nextTime = nextTime;
                }
                else
                {
                    Log.ForContext("hint",
                            "Just in case a negative Next Photo time will resolve due to delays processing the main loop (or error conditions keeping this heartbeat/watchdog loop from running as on the expected schedule negative values less than 5 minutes are logged but tolerated...")
                        .Information(
                            $"Photo time of {_nextTime.ToShortOutput()} is in the Past (current time {DateTime.Now.ToShortOutput()})...");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

        await stoppingToken.WhenCancelled();

        await Log.CloseAndFlushAsync();
    }
}