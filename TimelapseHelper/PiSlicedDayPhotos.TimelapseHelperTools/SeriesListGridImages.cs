using SkiaSharp;

namespace PiSlicedDayPhotos.TimelapseHelperTools;

public static class SeriesListGridImages
{
    public const string ConsoleSettingsIdentifier = "grid";

    public static async Task<(string resultFile, bool errors, List<string> runLog)>
        ImageGridTimelapse(List<PiSlicedDayPhotoInformation> photos,
            Dictionary<int, string> orderedTimeDescriptionList, Dictionary<int, string> orderedSeriesList,
            int framerate, string ffmpegExe, IProgress<string> progress, bool writeDateTimeString,
            string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        var fileDirectory = ImageGridTimeDescriptionFiles(photos, orderedTimeDescriptionList, orderedSeriesList,
            progress, writeDateTimeString, dateCaptionDateTimeFormat, fontSize);

        var resultFile = $"Timelapse-Created-{DateTime.Now:yyyy-MM-dd HH-mm}.mp4";

        var command =
            $"""
             cd '{fileDirectory}'
             {ffmpegExe} -framerate {framerate} -i z_PiSlicedDayGrid--%06d.jpg -s:v 3840x2160 -c:v libx264 -crf 17 -r 30 '{resultFile}'
             """;

        var runResult = await PowerShellRun.ExecuteScript(command, progress);

        return (Path.Combine(fileDirectory, resultFile), runResult.fatalErrors, runResult.runLog);
    }

    public static string ImageGridTimeDescriptionFiles(List<PiSlicedDayPhotoInformation> photos,
        Dictionary<int, string> orderedTimeDescriptionList, Dictionary<int, string> orderedSeriesList,
        IProgress<string> progress, bool writeDateTimeString,
        string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        progress.Report(
            $"Starting Time Description Image Grids - {photos.Count} Photos, {orderedTimeDescriptionList.Count} Time Descriptions");

        var photoList = photos.OrderBy(x => x.TakenOn).ToList();

        var lastIndex = 0;

        var descriptionGroups = new List<GridPhotoGroup>();

        var currentGroup = new GridPhotoGroup
            { Description = photoList[0].Description, ReferenceDateTime = photoList[0].TakenOn };

        foreach (var loopPhoto in photoList)
        {
            if (loopPhoto.Description.Equals(currentGroup.Description) &&
                Math.Abs(loopPhoto.TakenOn.Subtract(currentGroup.ReferenceDateTime).TotalMinutes) <= 2)
            {
                currentGroup.Photos.Add(loopPhoto);
                continue;
            }

            descriptionGroups.Add(currentGroup);

            currentGroup = new GridPhotoGroup
                { Description = loopPhoto.Description, ReferenceDateTime = loopPhoto.TakenOn };
            currentGroup.Photos.Add(loopPhoto);
        }

        descriptionGroups = descriptionGroups.OrderBy(x => x.ReferenceDateTime).ToList();

        foreach (var loopOrder in orderedTimeDescriptionList.OrderBy(x => x.Key))
        {
            if (descriptionGroups.First().Description.Equals(loopOrder.Value))
                lastIndex = loopOrder.Key;
            break;
        }

        if (lastIndex == 0) lastIndex = orderedTimeDescriptionList.Last().Key;
        else lastIndex--;

        var descriptionSets = new List<List<GridPhotoGroup?>>();

        var currentSet = new List<GridPhotoGroup?>();

        progress.Report("Assembling Photo Groups");

        foreach (var loopPhotos in descriptionGroups)
        {
            var loopPhotoIndex = orderedTimeDescriptionList.Single(x => x.Value.Equals(loopPhotos.Description)).Key;

            if (loopPhotoIndex <= lastIndex)
            {
                descriptionSets.Add(currentSet);
                currentSet = [loopPhotos];
                lastIndex = loopPhotoIndex;
                continue;
            }

            currentSet.Add(loopPhotos);
            lastIndex = loopPhotoIndex;
        }

        var commonDimension = photos.GroupBy(x => new { x.Width, x.Height })
            .OrderByDescending(x => x.Count()).First().Key;

        descriptionSets = descriptionSets.Where(x => x.Any(y => y != null)).OrderBy(x => x.First()!.ReferenceDateTime)
            .ToList();

        progress.Report(
            $"Assembled {descriptionSets.Count} Sets of Images, Single Photo Dimensions {commonDimension.Width}x{commonDimension.Height}");

        var counter = 0;

        var outputDirectory = FileLocationTools.UniqueTimeLapseStorageDirectory();

        var captionMaxWidth = 0;
        if (writeDateTimeString)
        {
            var captionString = new List<string>();
            foreach (var loopSet in descriptionSets)
            {
                var referenceGroup = loopSet.FirstOrDefault(x => x != null);
                if (referenceGroup == null) continue;
                captionString.Add(referenceGroup.ReferenceDateTime.ToString(dateCaptionDateTimeFormat));
            }

            captionMaxWidth = PiSlicedDayPhotoTools.CalculateMaxDimensionsForText(captionString, fontSize).maxWidth;
        }

        foreach (var loopDescriptionSet in descriptionSets)
        {
            if (counter == 0 || counter % 10 == 0)
                progress.Report($"Processing and Writing Files - Set {counter} of {descriptionSets.Count}");

            var fileList = new List<string>();

            foreach (var loopDescriptionOrder in orderedTimeDescriptionList.OrderBy(x => x.Key))
            {
                var descriptionSet =
                    loopDescriptionSet.FirstOrDefault(
                        x => x != null && x.Description.Equals(loopDescriptionOrder.Value));

                foreach (var loopSeriesOrder in orderedSeriesList.OrderBy(x => x.Key))
                {
                    var loopPhoto = descriptionSet?.Photos.FirstOrDefault(x => x.Series.Equals(loopSeriesOrder.Value));
                    fileList.Add(loopPhoto?.FileName ?? string.Empty);
                }
            }

            var referenceDescriptionSet = loopDescriptionSet.FirstOrDefault(x => x != null);

            if (referenceDescriptionSet == null) continue;

            var outputPath = Path.Combine(outputDirectory.FullName,
                $"z_PiSlicedDayGrid--{counter++:D6}.jpg");

            var caption = writeDateTimeString
                ? referenceDescriptionSet.ReferenceDateTime.ToString(dateCaptionDateTimeFormat)
                : string.Empty;

            CombineImages(fileList, commonDimension.Width, commonDimension.Height, orderedTimeDescriptionList.Count, orderedSeriesList.Count, caption,
                fontSize, commonDimension.Width * orderedSeriesList.Count - captionMaxWidth - 10,
                commonDimension.Height * orderedTimeDescriptionList.Count - 10, outputPath);
        }

        progress.Report($"Returning Output Directory - {outputDirectory.FullName}");

        return outputDirectory.FullName;
    }

    private static void CombineImages(List<string> imagePaths, int imageWidth, int imageHeight, int rows, int columns,
        string captionString, int fontSize, int captionX, int captionY, string outputFile)
    {
        var canvasWidth = columns * imageWidth;
        var canvasHeight = rows * imageHeight;

        using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        for (var i = 0; i < imagePaths.Count; i++)
        {
            var row = i / columns;
            var col = i % columns;

            if (row >= rows)
                break;

            DrawImage(canvas, imagePaths[i], imageWidth, imageHeight, col * imageWidth, row * imageHeight);
        }

        if (!string.IsNullOrWhiteSpace(captionString))
        {
            // Define the paint for the text
            using var paint = new SKPaint();
            paint.Color = SKColors.White;
            paint.IsAntialias = true;
            paint.TextSize = fontSize;
            paint.Typeface = SKTypeface.FromFamilyName("Arial");

            // Draw the text on the image
            canvas.DrawText(captionString, captionX, captionY, paint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        using var stream = File.OpenWrite(outputFile);
        data.SaveTo(stream);
    }

    private static void DrawImage(SKCanvas canvas, string imagePath, int width, int height, int xOffset, int yOffset)
    {
        if (string.IsNullOrEmpty(imagePath))
            return;

        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap != null)
        {
            var resizedBitmap = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
            var x = xOffset + (width - resizedBitmap.Width) / 2;
            var y = yOffset + (height - resizedBitmap.Height) / 2;

            var destRect = new SKRect(x, y, x + resizedBitmap.Width, y + resizedBitmap.Height);
            var srcRect = new SKRect(0, 0, resizedBitmap.Width, resizedBitmap.Height);

            canvas.DrawBitmap(resizedBitmap, srcRect, destRect);
        }
    }

    private class GridPhotoGroup
    {
        public required string Description { get; init; }
        public required DateTime ReferenceDateTime { get; init; }
        public List<PiSlicedDayPhotoInformation> Photos { get; } = [];
    }
}