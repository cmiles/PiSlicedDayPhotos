using System.Diagnostics;
using System.Text.Json;
using PiSlicedDayPhotos.Utility;
using Serilog;

namespace PiSlicedDayPhotos;

public class PhotoWorker : BackgroundService
{
    private ScheduledPhoto _nextTime = new()
        { Kind = PhotoKind.Day, ScheduledTime = new DateTime(2012, 1, 12, 12, 0, 0), Description = "Default Scheduled Photo"};

    private string ErrorImageFileName(PiSlicedDaySettings userSettings)
    {
        return Path.Combine(userSettings.PhotoStorageDirectory,
            $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-Error{(string.IsNullOrWhiteSpace(userSettings.PhotoNamePostfix) ? "" : "-")}{userSettings.PhotoNamePostfix}.jpg");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settingsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "PiSlicedDaySettings.json"));

        Log.ForContext("settings", settingsFile.SafeObjectDump())
            .Information("[Settings] Program Started with Settings File {settingsFile}", settingsFile.FullName);

        if (!settingsFile.Exists)
        {
            Log.Error(
                "[Settings] Please include a settings file named 'PiSlicedDaySettings.json' in the program directory. Did not Find {settingsFile}",
                settingsFile.FullName);
            return;
        }

        PiSlicedDaySettings? settings;

        try
        {
            settings = JsonSerializer.Deserialize<PiSlicedDaySettings>(
                await File.ReadAllTextAsync(settingsFile.FullName, stoppingToken));
        }
        catch (Exception e)
        {
            Log.Error(e, "[Settings] Problem Reading Settings File {settingsFile}", settingsFile.FullName);
            return;
        }

        if (settings == null)
        {
            Log.Error(
                "[Settings] Reading Settings File {settingsFile} returned null - empty or invalid settings or file format?",
                settingsFile.FullName);
            return;
        }

        var sunriseSunsetCsv = new FileInfo(Path.Combine(AppContext.BaseDirectory, "SunriseAndSunset.csv"));

        Log.Information("[Settings] Program Started with Sunrise/Sunset File {sunriseSunsetFile}",
            sunriseSunsetCsv.FullName);

        if (!sunriseSunsetCsv.Exists)
        {
            Log.Error(
                "[Settings] Please include a CSV file named 'SunriseAndSunset.csv' in the program directory. Did not Find {sunriseSunsetFile}",
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
                Log.Error(e, "[General] Main Photograph Loop Error");

                await ExceptionTools.WriteExceptionToImage(string.Empty, e, ErrorImageFileName(settings),
                    settings.LogFullExceptionsToImages);
            }
        }

        async Task TakePhotograph()
        {
            //Give the PhotoTakerCallback a chance to run without being interrupted by the watchdog
            heartBeatWatchDogTimer!.Change(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1));

            Console.WriteLine();

            var currentPhotoDateTime = _nextTime;

            var fileName = Path.Combine(settings.PhotoStorageDirectory,
                $"{DateTime.Now:yyyy-MM-dd-HH-mm}-{currentPhotoDateTime.Description.SanitizeForFileName()}-{(string.IsNullOrWhiteSpace(settings.PhotoNamePostfix) ? "" : "-")}{settings.PhotoNamePostfix.SanitizeForFileName()}.jpg");

            var photoExecutable = "libcamera-still";
            var photoArguments = $"-o {fileName} {_nextTime.LibCameraParameters}".Trim();

            Log.Verbose($"Taking Photo at {DateTime.Now:O} - {photoExecutable} {photoArguments}");

            var photoDataList = new List<string>();

            try
            {
                var process = new Process();
                process.StartInfo.FileName = photoExecutable;
                process.StartInfo.Arguments = photoArguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (_, eventArgs) =>
                {
                    Console.WriteLine(eventArgs.Data);
                    photoDataList.Add(eventArgs.Data ?? string.Empty);
                };
                process.ErrorDataReceived += (_, eventArgs) =>
                {
                    Console.WriteLine(eventArgs.Data);
                    photoDataList.Add(eventArgs.Data ?? string.Empty);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(stoppingToken);

                if (process.ExitCode == 0)
                    Log.ForContext("libcamera-output", string.Join(Environment.NewLine, photoDataList))
                        .ForContext("settings", settings)
                        .Information("[Photograph] Photograph Taken - {executable} {arguments}", photoExecutable,
                            photoArguments);
                else
                    throw new Exception($"libcamera-still exited with code {process.ExitCode}");
            }
            catch (Exception e)
            {
                Log.ForContext("libcamera-output", string.Join(Environment.NewLine, photoDataList))
                    .ForContext("settings", settings).Error(e,
                        "[Photograph] Problem Running libcamera-still - {executable} {arguments}", photoExecutable,
                        photoArguments);

                await ExceptionTools.WriteExceptionToImage(
                    $"[Photograph] Problem Running libcamera-still - {photoExecutable} {photoArguments}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, photoDataList)}",
                    e, ErrorImageFileName(settings),
                    settings.LogFullExceptionsToImages);
            }

            PiSlicedDaySettings? newSettings = null;

            try
            {
                newSettings = JsonSerializer.Deserialize<PiSlicedDaySettings>(
                    await File.ReadAllTextAsync(settingsFile.FullName, stoppingToken));
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "[Settings] Problem Refreshing Settings from File {settingsFile} - Continuing with Existing Settings",
                    settingsFile.FullName);

                await ExceptionTools.WriteExceptionToImage(
                    $"Problem Refreshing Settings from File {settingsFile.FullName} - Continuing with Existing Settings.{Environment.NewLine}{Environment.NewLine}Existing Settings:{Environment.NewLine}{JsonSerializer.Serialize(settings, new JsonSerializerOptions() { WriteIndented = true })}",
                    e, ErrorImageFileName(settings),
                    settings.LogFullExceptionsToImages);
            }

            if (newSettings != null) settings = newSettings;

            _nextTime = PhotographTimeTools.PhotographTimeFromFile(DateTime.Now, settings.SunriseSunsetCsvFile,
                settings);

            Log.Information("[Timing] Next Photograph Time Set {@NextTime}", _nextTime);

            if (currentPhotoDateTime.ScheduledTime.Day != _nextTime.ScheduledTime.Day)
            {
                var upcomingSchedule = PhotographTimeTools.PhotographTimeScheduleFromFile(2,
                    currentPhotoDateTime.ScheduledTime,
                    settings.SunriseSunsetCsvFile,
                    settings);

                var scheduleDayGroup = upcomingSchedule.GroupBy(x => x.ScheduledTime.Date).ToList();

                scheduleDayGroup.ForEach(x =>
                    Console.WriteLine(
                        $"Schedule for {x.First().ScheduledTime:M/d}: {string.Join(", ", x.Select(y => y.ScheduledTime.ToString("h:mm tt")))}"));
            }

            mainLoop.Change(_nextTime.ScheduledTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);

            Console.WriteLine();

            //There could be a slightly awkward interaction with the timing change at the top of this method TO
            //delay the watch dog until after the photo run - but in general the assumption is that the delay
            //should be more than enough time to execute the photo taking process.
            heartBeatWatchDogTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        _nextTime = PhotographTimeTools.PhotographTimeFromFile(DateTime.Now, settings.SunriseSunsetCsvFile,
            settings);

        Log.Information("[Timing] Next Photograph Time Set {@NextTime}", _nextTime);

        var upcomingSchedule = PhotographTimeTools.PhotographTimeScheduleFromFile(2, DateTime.Now,
            settings.SunriseSunsetCsvFile,
            settings);

        var scheduleDayGroup = upcomingSchedule.GroupBy(x => x.ScheduledTime.Date).ToList();

        scheduleDayGroup.ForEach(x =>
            Console.WriteLine(
                $"Schedule for {x.First().ScheduledTime:M/d}: {string.Join(", ", x.Select(y => y.ScheduledTime.ToString("h:mm tt")))}"));

        mainLoop = new Timer(TakePhotographExceptionTrapWrapperCallback, null,
            _nextTime.ScheduledTime.Subtract(DateTime.Now),
            Timeout.InfiniteTimeSpan);

        Console.WriteLine(
            $"Next Scheduled Photo: {_nextTime.ScheduledTime:O} - {_nextTime.ScheduledTime.Subtract(DateTime.Now):g}");

        heartBeatWatchDogTimer = new Timer((_) =>
            {
                var timeUntilNextPhoto = _nextTime.ScheduledTime.Subtract(DateTime.Now);
                
                if (timeUntilNextPhoto.TotalSeconds >= 0)
                {
                    Console.WriteLine($"Photo in {_nextTime.ScheduledTime.Subtract(DateTime.Now):c}");
                    return;
                }

                if (timeUntilNextPhoto.TotalMinutes <= -5)
                {
                    var nextTime = PhotographTimeTools.PhotographTimeFromFile(DateTime.Now,
                        settings.SunriseSunsetCsvFile, settings);

                    Log.ForContext("hint",
                            "A past next photo time can result from errors in the main photo loop - there should be Log entries prior to this entry that help diagnose any problems.")
                        .Warning(
                            $"[Timing] Photo time of {_nextTime.ScheduledTime.ToShortOutput()} is in the Past (current time {DateTime.Now.ToShortOutput()}) - resetting Next Time to {nextTime.ScheduledTime.ToShortOutput()}");

                    _nextTime = nextTime;
                    mainLoop.Change(_nextTime.ScheduledTime.Subtract(DateTime.Now), Timeout.InfiniteTimeSpan);
                }
                else
                {
                    Log.ForContext("hint",
                            "Just in case a negative Next Photo time will resolve due to delays processing the main loop (or error conditions keeping this heartbeat/watchdog loop from running as on the expected schedule negative values less than 5 minutes are logged but tolerated...")
                        .Information(
                            $"[Timing] Logging Past Photo time of {_nextTime.ScheduledTime.ToShortOutput()} (current time {DateTime.Now.ToShortOutput()}) - Not Taking Any Action At This Time");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

        await stoppingToken.WhenCancelled();

        await Log.CloseAndFlushAsync();
    }
}