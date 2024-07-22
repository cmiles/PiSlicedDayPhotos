using System.Data;
using System.Diagnostics;
using Codeuctivity.SkiaSharpCompare;
using PiSlicedDayPhotos.Utility;
using SkiaSharp;

namespace PiSlicedDayPhotos.Test;

public class ImageExceptionTest
{
    [Test]
    public async Task CompareImageException()
    {
        string errorImageFileName;

        try
        {
            throw new DataException("This is a test exception",
                new InvalidCastException("Inner Exception"));
        }
        catch (Exception e)
        {
            errorImageFileName = Path.Combine(AppContext.BaseDirectory,
                $"Error-Test-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.jpg");

            await ExceptionTools
                .WriteExceptionToImage($"This is the test message" +
                                       $" provided to the {nameof(ExceptionTools.WriteExceptionToImage)} " +
                                       $"method.{Environment.NewLine}{Environment.NewLine}More user " +
                                       $"provided information.",
                    e, errorImageFileName, true);
        }

        Debug.WriteLine($"Error Image File Name: {errorImageFileName}");

        //The image mask allows the line number for the exception to change so that at least minor
        //changes in this file won't cause the test to fail.
        var imageErrorTestModel = Path.Combine(AppContext.BaseDirectory, "Image-Error-Test-Model.jpg");
        var imageErrorTestModelDifferenceMask =
            Path.Combine(AppContext.BaseDirectory, "Image-Error-Test-Model-Difference-Mask.png");

        var maskedDiff = Compare.CalcDiff(imageErrorTestModel, errorImageFileName, imageErrorTestModelDifferenceMask);

        //If the test is going to fail in the assert below calculate and save the difference between the test
        //and generated images as an image written to the AppContext.BaseDirectory. This 'visual error' is
        //probably the most likely to be useful to a developer.
        if (maskedDiff.AbsoluteError > 0)
        {
            await using var fileStreamDifferenceMask = File.Create(Path.Combine(AppContext.BaseDirectory,
                $"Image-Calculated-Difference-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.jpg"));
            using var maskImage = Compare.CalcDiffMaskImage(imageErrorTestModel, errorImageFileName);
            var encodedData = maskImage.Encode(SKEncodedImageFormat.Png, 100);
            encodedData.SaveTo(fileStreamDifferenceMask);
        }

        Assert.That(maskedDiff.AbsoluteError, Is.EqualTo(0));
    }
}