using SkiaSharp;

namespace PiSlicedDayPhotos.TimelapseHelperTools;

public static class SeriesListGridImages
{
    public static async Task<(string resultFile, bool errors, List<string> runLog)>
        ImageGridTimelapse(List<PiSlicedDayPhotoInformation> photos, Dictionary<int, string> orderedTimeDescriptionList,
            string ffmpegExe, int framerate, IProgress<string> progress, bool writeDateTimeString,
            string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        var fileDirectory = ImageGridTimeDescriptionFiles(photos, orderedTimeDescriptionList,
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
        Dictionary<int, string> orderedTimeDescriptionList, IProgress<string> progress, bool writeDateTimeString,
        string dateCaptionDateTimeFormat = "yyyy MMMM", int fontSize = 24)
    {
        progress.Report(
            $"Starting Time Description Image Grids - {photos.Count} Photos, {orderedTimeDescriptionList.Count} Time Descriptions");

        var orderedList = photos.OrderBy(x => x.TakenOn).ToList();

        var lastIndex = 0;

        foreach (var loopOrder in orderedTimeDescriptionList)
            if (orderedList.First().Description.Equals(loopOrder.Value))
                lastIndex = loopOrder.Key;

        if (lastIndex == 0) lastIndex = orderedTimeDescriptionList.Last().Key;
        else lastIndex--;

        var sets = new List<List<PiSlicedDayPhotoInformation?>>();

        var currentSet = new List<PiSlicedDayPhotoInformation?>();

        progress.Report("Assembling Photo Groups");

        foreach (var loopPhotos in orderedList)
        {
            var loopPhotoIndex = orderedTimeDescriptionList.Single(x => x.Value.Equals(loopPhotos.Description)).Key;

            if (loopPhotoIndex <= lastIndex)
            {
                sets.Add(currentSet);
                currentSet = [loopPhotos];
                lastIndex = loopPhotoIndex;
                continue;
            }

            currentSet.Add(loopPhotos);
            lastIndex = loopPhotoIndex;
        }

        var cubeSize = (int)Math.Ceiling(Math.Sqrt(orderedTimeDescriptionList.Count));

        var commonDimension = photos.GroupBy(x => new { x.Width, x.Height })
            .OrderByDescending(x => x.Count()).First().Key;

        sets = sets.Where(x => x.Any(y => y != null)).OrderBy(x => x.First(y => y != null)!.TakenOn).ToList();

        progress.Report(
            $"Assembled {sets.Count} Sets of Images, Cube Size {cubeSize} photos Wide/High, Single Photo Dimensions {commonDimension.Width}x{commonDimension.Height}");

        var counter = 0;

        var outputDirectory = FileLocationTools.UniqueTimeLapseStorageDirectory();

        var captionMaxWidth = 0;
        if (writeDateTimeString)
        {
            var captionString = new List<string>();
            foreach (var loopSet in sets)
            {
                var referencePhoto = loopSet.FirstOrDefault(x => x != null);
                if (referencePhoto == null) continue;
                captionString.Add(referencePhoto.TakenOn.ToString(dateCaptionDateTimeFormat));
            }

            captionMaxWidth = PiSlicedDayPhotoTools.CalculateMaxDimensionsForText(captionString, fontSize).maxWidth;
        }

        foreach (var loopSet in sets)
        {
            if (counter == 0 || counter % 10 == 0)
                progress.Report($"Processing and Writing Files - Set {counter} of {sets.Count}");

            var fileList = new List<string>();

            foreach (var loopOrder in orderedTimeDescriptionList)
            {
                var loopPhoto = loopSet.FirstOrDefault(x => x != null && x.Description.Equals(loopOrder.Value));
                fileList.Add(loopPhoto?.FileName ?? string.Empty);
            }

            var referencePhoto = loopSet.FirstOrDefault(x => x != null);

            if (referencePhoto == null) continue;

            var outputPath = Path.Combine(outputDirectory.FullName,
                $"z_PiSlicedDayGrid--{counter++:D6}.jpg");

            CombineImages(fileList, commonDimension.Width, commonDimension.Height, cubeSize, cubeSize, outputPath);

            if (writeDateTimeString)
            {
                var caption = referencePhoto.TakenOn.ToString(dateCaptionDateTimeFormat);
                PiSlicedDayPhotoTools.AddTextToImage(outputPath, caption, fontSize,
                    commonDimension.Width * cubeSize - captionMaxWidth - 10, commonDimension.Height * cubeSize - 10);
            }
        }

        progress.Report($"Returning Output Directory - {outputDirectory.FullName}");

        return outputDirectory.FullName;
    }

    public static void CombineImages(List<string> imagePaths, int imageWidth, int imageHeight, int rows, int columns,
        string outputFile)
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

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
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
}