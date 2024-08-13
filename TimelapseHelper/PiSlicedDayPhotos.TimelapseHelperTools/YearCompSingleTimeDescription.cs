using SkiaSharp;

namespace PiSlicedDayPhotos.TimelapseHelperTools;

public static class YearCompSingleTimeDescription
{
    public const string ConsoleSettingsIdentifier = "yearcomp";

    public static async Task<(string resultFile, bool errors, List<string> runLog)>
        YearCompSingleTimeDescriptionTimelapse(List<PiSlicedDayPhotoInformation> photos,
            DateTime mainSetStartTime, DateTime mainSetEndTime,
            Dictionary<int, string> seriesOrderLookup, int framerate, string ffmpegExe, IProgress<string> progress,
            bool writeDateTimeString, string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        var fileDirectory = YearCompSingleTimeDescriptionTimelapseFiles(photos, mainSetStartTime, mainSetEndTime,
             seriesOrderLookup, framerate,
            ffmpegExe, progress, writeDateTimeString, dateCaptionDateTimeFormat, fontSize);

        var resultFile = $"Timelapse-Created-{DateTime.Now:yyyy-MM-dd HH-mm}.mp4";

        var command =
            $"""
             cd '{fileDirectory}'
             {ffmpegExe} -framerate {framerate} -i z_timelapse--%06d.jpg -s:v 3840x2160 -c:v libx264 -crf 17 -r 30 '{resultFile}'
             """;

        var runResult = await PowerShellRun.ExecuteScript(command, progress);

        return (Path.Combine(fileDirectory, resultFile), runResult.fatalErrors, runResult.runLog);
    }

    public static string
        YearCompSingleTimeDescriptionTimelapseFiles(List<PiSlicedDayPhotoInformation> photos,
            DateTime mainSetStartTime, DateTime mainSetEndTime, 
            Dictionary<int, string> seriesOrderLookup, int framerate, string ffmpegExe, IProgress<string> progress,
            bool writeDateTimeString, string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        var selectedTimeDescriptions = photos.Select(x => x.Description).Distinct().ToList();

        if (selectedTimeDescriptions.Count > 1)
            throw new ArgumentException("Only one time description is allowed for this method",
                nameof(selectedTimeDescriptions));

        var selectedSeriesNames = photos.Select(x => x.Series).Distinct().ToList();

        progress.Report($"Time Description: {string.Join(", ", selectedTimeDescriptions)}");
        progress.Report($"Series: {string.Join(", ", selectedSeriesNames)}");

        var timeDescriptionGroups = new List<TimeDescriptionPhotoSet>();

        progress.Report("Assembling Photo Groups");

        foreach (var loopPhotos in photos)
        {
            var existingSet = timeDescriptionGroups.FirstOrDefault(x =>
                Math.Abs(x.ReferenceDateTime.Subtract(loopPhotos.TakenOn.Date).TotalMinutes) <= 2);

            if (existingSet == null)
            {
                existingSet = new TimeDescriptionPhotoSet { ReferenceDateTime = loopPhotos.TakenOn.Date };
                timeDescriptionGroups.Add(existingSet);
            }

            existingSet.Photos.Add(loopPhotos);
        }

        progress.Report($"Found {timeDescriptionGroups.Count} Groups for Images");

        var tempStorageDirectory = FileLocationTools.UniqueTimeLapseStorageDirectory();

        var commonDimension = timeDescriptionGroups.SelectMany(x => x.Photos)
            .GroupBy(x => new { x.Width, x.Height })
            .OrderByDescending(x => x.Count()).First().Key;

        var captionMaxWidth = 0;

        if (writeDateTimeString)
        {
            var strings = timeDescriptionGroups.Select(x => x.ReferenceDateTime.ToString(dateCaptionDateTimeFormat))
                .Distinct().ToList();

            captionMaxWidth = PiSlicedDayPhotoTools.CalculateMaxDimensionsForText(strings, fontSize).maxWidth;
        }

        var orderedMainGroups = timeDescriptionGroups
            .Where(x => x.ReferenceDateTime >= mainSetStartTime && x.ReferenceDateTime <= mainSetEndTime)
            .OrderBy(x => x.ReferenceDateTime).ToList();

        var counter = 1;

        foreach (var loopThisYearGroup in orderedMainGroups)
        {
            if (counter == 1 || counter % 25 == 0)
                progress.Report($"Converting Photo Group {counter} of {orderedMainGroups.Count}");
            var destinationFile =
                Path.Combine(tempStorageDirectory.FullName, $"z_timelapse--{counter++:D6}.jpg");

            var lastYearDateTime = loopThisYearGroup.ReferenceDateTime.AddDays(-364);
            var lastYearGroup = timeDescriptionGroups
                .Where(x => Math.Abs(x.ReferenceDateTime.Subtract(lastYearDateTime).TotalHours) < 12).MinBy(x =>
                    Math.Abs(x.ReferenceDateTime.Subtract(lastYearDateTime).TotalMinutes));

            var thisYearFiles = new List<string>();
            var lastYearFiles = new List<string>();

            foreach (var loopSeries in seriesOrderLookup.OrderBy(x => x.Key))
            {
                var thisYearPossibleFile = loopThisYearGroup.Photos.FirstOrDefault(x => x.Series == loopSeries.Value);
                var lastYearPossibleFile = lastYearGroup?.Photos.FirstOrDefault(x => x.Series == loopSeries.Value);

                thisYearFiles.Add(thisYearPossibleFile?.FileName ?? string.Empty);
                lastYearFiles.Add(lastYearPossibleFile?.FileName ?? string.Empty);
            }

            CombineImages(thisYearFiles, lastYearFiles, destinationFile, commonDimension.Width,
                commonDimension.Height);

            if (writeDateTimeString)
            {
                PiSlicedDayPhotoTools.AddTextToImage(destinationFile,
                    loopThisYearGroup.ReferenceDateTime.ToString(dateCaptionDateTimeFormat),
                    fontSize, commonDimension.Width * seriesOrderLookup.Count - captionMaxWidth - 10,
                    commonDimension.Height - 10);
                if (lastYearGroup is not null)
                    PiSlicedDayPhotoTools.AddTextToImage(destinationFile,
                        lastYearGroup.ReferenceDateTime.ToString(dateCaptionDateTimeFormat),
                        fontSize, commonDimension.Width * seriesOrderLookup.Count - captionMaxWidth - 10,
                        commonDimension.Height * 2 - 10);
            }
        }

        return tempStorageDirectory.FullName;
    }


    private static void CombineImages(List<string> topRowImages, List<string> bottomRowImages, string outputPath,
        int width, int height)
    {
        var rows = 2;
        var columns = Math.Max(topRowImages.Count, bottomRowImages.Count);
        var canvasWidth = columns * width;
        var canvasHeight = rows * height;

        using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        DrawImages(canvas, topRowImages, width, height, 0);
        DrawImages(canvas, bottomRowImages, width, height, height);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    private static void DrawImages(SKCanvas canvas, List<string> images, int width, int height, int yOffset)
    {
        for (var i = 0; i < images.Count; i++)
        {
            if (string.IsNullOrEmpty(images[i]))
                continue;

            using var bitmap = SKBitmap.Decode(images[i]);
            if (bitmap != null)
            {
                var resizedBitmap = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
                var x = i * width;
                var y = yOffset;

                var destRect = new SKRect(x, y, x + width, y + height);
                var srcRect = new SKRect(0, 0, resizedBitmap.Width, resizedBitmap.Height);

                canvas.DrawBitmap(resizedBitmap, srcRect, destRect);
            }
        }
    }
}