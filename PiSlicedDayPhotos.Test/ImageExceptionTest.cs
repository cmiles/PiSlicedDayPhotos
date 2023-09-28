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
        var imageErrorTestModelDifferenceMask = Path.Combine(AppContext.BaseDirectory, "Image-Error-Test-Model-Difference-Mask.png");

        var maskedDiff = Compare.CalcDiff(imageErrorTestModel, errorImageFileName, imageErrorTestModelDifferenceMask);
        Assert.That(maskedDiff.AbsoluteError, Is.EqualTo(0));
    }
}