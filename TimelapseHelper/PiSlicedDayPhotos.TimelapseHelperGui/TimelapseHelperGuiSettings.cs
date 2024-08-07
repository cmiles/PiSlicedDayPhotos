using Metalama.Patterns.Observability;
using PointlessWaymarks.LlamaAspects;

namespace PiSlicedDayPhotos.TimelapseHelperGui;

[Observable]
public partial class TimelapseHelperGuiSettings
{
    public string? LastInputDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PiSlicedDayPhotos";
    public string? FfmpegExecutableDirectory { get; set; } = @"E:\ffmpeg";
}