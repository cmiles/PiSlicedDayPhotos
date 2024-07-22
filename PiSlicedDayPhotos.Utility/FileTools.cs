namespace PiSlicedDayPhotos.Utility;

public static class FileTools
{
    public static string SanitizeForFileName(this string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return sanitized;
    }
}