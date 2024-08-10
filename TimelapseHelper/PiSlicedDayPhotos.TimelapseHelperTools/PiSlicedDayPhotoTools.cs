using System.Globalization;
using SkiaSharp;

namespace PiSlicedDayPhotos.TimelapseHelperTools;

public static class PiSlicedDayPhotoTools
{
    public static List<PiSlicedDayPhotoInformation> ProcessDirectory(string directoryPath, IProgress<string> progress)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var sourcePhotos = new List<PiSlicedDayPhotoInformation>();

        progress.Report($"Processing Directory: {directoryPath}");

        var allImages = new DirectoryInfo(directoryPath).EnumerateFiles("*--*.jpg").ToList();

        var counter = 0;

        foreach (var loopImage in allImages)
        {
            if (++counter == 1 || counter % 50 == 0)
                progress.Report($"Processing Image {counter} of {allImages.Count}");

            var initialSplit = Path.GetFileNameWithoutExtension(loopImage.FullName).Split("--");

            var dateString = initialSplit[0].Substring(0, 16);
            var descriptionString = initialSplit[0].Substring(17, initialSplit[0].Length - 17);
            var seriesString = initialSplit[1];

            var dimensions = GetJpgDimensions(loopImage.FullName);

            sourcePhotos.Add(new PiSlicedDayPhotoInformation
            {
                FileName = loopImage.FullName,
                TakenOn = DateTime.ParseExact(dateString, "yyyy-MM-dd-HH-mm", CultureInfo.InvariantCulture),
                Series = seriesString,
                Description = descriptionString,
                Height = dimensions.height,
                Width = dimensions.width
            });
        }

        return sourcePhotos;
    }

    public static (int width, int height) GetJpgDimensions(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            throw new ArgumentException("Invalid file path", nameof(filePath));

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var codec = SKCodec.Create(stream);
        if (codec == null) throw new InvalidOperationException("Unable to read image codec.");

        return (codec.Info.Width, codec.Info.Height);
    }


    public static (int maxWidth, int maxHeight) CalculateMaxDimensionsForText(List<string> strings, float fontSize)
    {
        if (strings == null || strings.Count == 0)
            throw new ArgumentException("String list cannot be null or empty", nameof(strings));

        float maxWidth = 0;
        float maxHeight = 0;

        using (var paint = new SKPaint())
        {
            paint.TextSize = fontSize;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("Arial");

            foreach (var str in strings)
            {
                var bounds = new SKRect();
                paint.MeasureText(str, ref bounds);

                if (bounds.Width > maxWidth) maxWidth = bounds.Width;

                if (bounds.Height > maxHeight) maxHeight = bounds.Height;
            }
        }

        return ((int)Math.Ceiling(maxWidth), (int)Math.Ceiling(maxHeight));
    }

    public static void AddTextToImage(string imagePath, string text, float fontSize, float x, float y)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            throw new ArgumentException("Invalid image file path", nameof(imagePath));

        using var inputStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        using var original = SKBitmap.Decode(inputStream);
        if (original == null) throw new InvalidOperationException("Unable to decode image.");

        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        var canvas = surface.Canvas;
        canvas.DrawBitmap(original, 0, 0);

        // Define the paint for the text
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.IsAntialias = true;
            paint.TextSize = fontSize;
            paint.Typeface = SKTypeface.FromFamilyName("Arial");

            // Draw the text on the image
            canvas.DrawText(text, x, y, paint);
        }

        using (var outputStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
        using (var data = surface.Snapshot().Encode(SKEncodedImageFormat.Jpeg, 100))
        {
            data.SaveTo(outputStream);
        }
    }
}