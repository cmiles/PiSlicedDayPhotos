using PointlessWaymarks.CommonTools;
using System.Diagnostics;

namespace PiSlicedDayPhotos.TimelapseHelperTools
{
    public static class FileLocationTools
    {
        public static DirectoryInfo DefaultStorageDirectory()
        {
            var directory =
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Pi Sliced-Day Photos"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo UniqueTimeLapseStorageDirectory()
        {
            var baseDirectory = DefaultStorageDirectory();
            var suggestedDirectory = Path.Combine(baseDirectory.FullName,
                $"TimeLapseTempStorage-{DateTime.Now:yyyy-MM-dd HH-mm-ss}");

            return UniqueFileTools.UniqueDirectory(suggestedDirectory);
        }
    }
}
