using SkiaSharp;

namespace PiSlicedDayPhotos.TimelapseHelperTools;

public static class SingleTimeDescription
{
    public const string ConsoleSettingsIdentifier = "singledescription";

    public static async Task<(string resultFile, bool errors, List<string> runLog)>
        SingleTimeDescriptionTimelapse(List<PiSlicedDayPhotoInformation> photos,
            Dictionary<int, string> seriesOrderLookup, int framerate, string ffmpegExe, IProgress<string> progress,
            bool writeDateTimeString, string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        var fileDirectory = SingleTimeDescriptionTimelapseFiles(photos, framerate, seriesOrderLookup,
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
        SingleTimeDescriptionTimelapseFiles(List<PiSlicedDayPhotoInformation> photos, int framerate,
            Dictionary<int, string> seriesOrderLookup, string ffmpegExe, IProgress<string> progress,
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

        if (selectedSeriesNames.Count == 1)
        {
            var orderedPhotos = timeDescriptionGroups.OrderBy(x => x.ReferenceDateTime)
                .SelectMany(x => x.Photos).ToList();

            var counter = 1;

            foreach (var loopTimelapsePhotos in orderedPhotos)
            {
                if (counter == 1 || counter % 50 == 0)
                    progress.Report($"Copying Photo {counter} of {orderedPhotos.Count}");

                var destinationFile =
                    Path.Combine(tempStorageDirectory.FullName, $"z_timelapse--{counter++:D6}.jpg");

                if (loopTimelapsePhotos.Width == commonDimension.Width &&
                    loopTimelapsePhotos.Height == commonDimension.Height)
                    File.Copy(loopTimelapsePhotos.FileName, destinationFile);
                else
                    ResizeAndCenterImage(loopTimelapsePhotos.FileName, destinationFile, commonDimension.Width,
                        commonDimension.Height);

                if (writeDateTimeString)
                    PiSlicedDayPhotoTools.AddTextToImage(destinationFile,
                        loopTimelapsePhotos.TakenOn.ToString(dateCaptionDateTimeFormat),
                        fontSize, commonDimension.Width - captionMaxWidth - 10, commonDimension.Height - 10);
            }
        }
        else
        {
            var orderedGroups = timeDescriptionGroups.OrderBy(x => x.ReferenceDateTime).ToList();

            var counter = 1;

            foreach (var loopGroup in orderedGroups)
            {
                if (counter == 1 || counter % 50 == 0)
                    progress.Report($"Converting Photo Group {counter} of {orderedGroups.Count}");
                var destinationFile =
                    Path.Combine(tempStorageDirectory.FullName, $"z_timelapse--{counter++:D6}.jpg");

                var files = new List<string>();

                foreach (var loopSeries in seriesOrderLookup.OrderBy(x => x.Key))
                {
                    var possibleFile = loopGroup.Photos.FirstOrDefault(x => x.Series == loopSeries.Value);

                    files.Add(possibleFile?.FileName ?? string.Empty);
                }

                CombineImages(files, destinationFile, commonDimension.Width, commonDimension.Height);

                if (writeDateTimeString)
                    PiSlicedDayPhotoTools.AddTextToImage(destinationFile,
                        loopGroup.ReferenceDateTime.ToString(dateCaptionDateTimeFormat),
                        fontSize, commonDimension.Width * seriesOrderLookup.Count - captionMaxWidth - 10,
                        commonDimension.Height - 10);
            }
        }

        return tempStorageDirectory.FullName;
    }


    public static void ResizeAndCenterImage(string inputPath, string outputPath, int targetWidth,
        int targetHeight)
    {
        if (string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
            throw new ArgumentException("Invalid input file path", nameof(inputPath));

        using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        using var original = SKBitmap.Decode(inputStream);
        if (original == null) throw new InvalidOperationException("Unable to decode image.");

        var aspectRatio = (float)original.Width / original.Height;
        int newWidth, newHeight;

        if (targetWidth / (float)targetHeight > aspectRatio)
        {
            newHeight = targetHeight;
            newWidth = (int)(targetHeight * aspectRatio);
        }
        else
        {
            newWidth = targetWidth;
            newHeight = (int)(targetWidth / aspectRatio);
        }

        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resized);
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        var info = new SKImageInfo(targetWidth, targetHeight);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        var x = (targetWidth - newWidth) / 2;
        var y = (targetHeight - newHeight) / 2;

        canvas.DrawImage(image, new SKRect(x, y, x + newWidth, y + newHeight));

        using var data = surface.Snapshot().Encode(SKEncodedImageFormat.Jpeg, 100);
        data.SaveTo(outputStream);
    }

    public static void CombineImages(List<string> imagePaths, string outputPath, int targetWidth, int targetHeight)
    {
        if (imagePaths == null || imagePaths.Count == 0)
            throw new ArgumentException("Image paths list cannot be null or empty", nameof(imagePaths));

        var totalWidth = targetWidth * imagePaths.Count;
        var info = new SKImageInfo(totalWidth, targetHeight);

        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        for (var i = 0; i < imagePaths.Count; i++)
        {
            var imagePath = imagePaths[i];
            SKBitmap? bitmap = null;

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                using var inputStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                bitmap = SKBitmap.Decode(inputStream);
            }

            if (bitmap == null)
            {
                bitmap = new SKBitmap(targetWidth, targetHeight);
                using var canvasBitmap = new SKCanvas(bitmap);
                canvasBitmap.Clear(SKColors.Black);
            }

            var aspectRatio = (float)bitmap.Width / bitmap.Height;
            int newWidth, newHeight;

            if (targetWidth / (float)targetHeight > aspectRatio)
            {
                newHeight = targetHeight;
                newWidth = (int)(targetHeight * aspectRatio);
            }
            else
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / aspectRatio);
            }

            using var resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
            using var image = SKImage.FromBitmap(resized);
            var x = i * targetWidth + (targetWidth - newWidth) / 2;
            var y = (targetHeight - newHeight) / 2;

            canvas.DrawImage(image, new SKRect(x, y, x + newWidth, y + newHeight));
        }

        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var data = surface.Snapshot().Encode(SKEncodedImageFormat.Jpeg, 100);
        data.SaveTo(outputStream);
    }
}