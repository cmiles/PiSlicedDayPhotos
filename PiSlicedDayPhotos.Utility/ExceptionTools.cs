using Serilog;
using SkiaSharp;
using Topten.RichTextKit;

namespace PiSlicedDayPhotos.Utility;

public static class ExceptionTools
{
    /// <summary>
    ///     Writes an exception to an image file - when a user is only looking at image output from a program
    ///     this may be an effective way to alert/inform the user about the error. WARNING If all information from the
    ///     exception is written to the image information about the program/system could be accidentally exposed!!!!
    ///     This method traps and silently logs all exceptions.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    /// <param name="fullFileName"></param>
    /// <param name="writeAllExceptionInformation"></param>
    /// <returns></returns>
    public static async Task WriteExceptionToImage(string message, Exception? exception, string fullFileName,
        bool writeAllExceptionInformation)
    {
        try
        {
            await WriteExceptionToImageInner(message, exception, fullFileName, writeAllExceptionInformation);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error Writing Error to Image");
        }
    }

    /// <summary>
    ///     Writes an exception to an image file - in most cases you should use the public WriteExceptionToImage which
    ///     will silently trap and log errors - it is unlikely that you want the execution of your program to stop
    ///     because an image error message could not be written??
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    /// <param name="fullFileName"></param>
    /// <param name="writeAllExceptionInformation"></param>
    /// <returns></returns>
    private static async Task WriteExceptionToImageInner(string message, Exception? exception, string fullFileName,
        bool writeAllExceptionInformation)
    {
        // Create a RichString
        var rs = new RichString()
            .Alignment(TextAlignment.Center)
            .FontFamily("Segoe UI")
            .FontSize(26)
            .TextColor(SKColors.Black)
            .MarginBottom(16)
            .Add("Pi Sliced Day Photo Error", fontSize: 24, fontWeight: 700)
            .Paragraph().Alignment(TextAlignment.Left)
            .FontSize(18)
            .Add("The Pi Sliced Day Photo program encountered an error - this could be temporary, but may need " +
                 "attention... The programs logs may have more information.");

        if (!string.IsNullOrWhiteSpace(message))
        {
            rs.Paragraph().Alignment(TextAlignment.Left)
                .FontSize(18)
                .MarginTop(48)
                .MarginBottom(64)
                .Add(message);
        }

        if (writeAllExceptionInformation)
            while (exception != null)
            {
                rs.Paragraph()
                    .Alignment(TextAlignment.Left)
                    .MarginTop(10)
                    .MarginRight(10)
                    .MarginBottom(10)
                    .MarginLeft(10)
                    .FontSize(16)
                    .Add(exception.Message)
                    .Paragraph()
                    .MarginTop(10)
                    .MarginRight(10)
                    .MarginBottom(10)
                    .MarginLeft(20)
                    .FontSize(14)
                    .Add(exception.ToString());

                exception = exception.InnerException;
            }

        rs.MaxWidth = 984;

        var imageInfo = new SKImageInfo(1024, (int)rs.MeasuredHeight + 40);
        using var surface = SKSurface.Create(imageInfo);
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        rs.Paint(canvas, new SKPoint(20, 20));

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        await using var stream = File.OpenWrite(fullFileName);

        data.SaveTo(stream);
    }
}