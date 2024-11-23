using Metalama.Patterns.Observability;

namespace PiSlicedDayPhotos.TimelapseHelperGui;

[Observable]
public partial class TimelapseHelperGuiSettings
{
    public string? LastInputDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications\PiSlicedDayPhotos.TimelapseHelperGuiInstallers";
    public string? FfmpegExecutableDirectory { get; set; } = @"E:\ffmpeg";
}