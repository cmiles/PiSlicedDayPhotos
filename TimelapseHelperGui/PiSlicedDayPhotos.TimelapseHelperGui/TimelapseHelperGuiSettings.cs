using PointlessWaymarks.LlamaAspects;

namespace PiSlicedDayPhotos.TimelapseHelperGui;

[NotifyPropertyChanged]
public partial class TimelapseHelperGuiSettings
{
    public string? LastInputDirectory { get; set; } = string.Empty;
    public string? LastOutputDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PiSlicedDayPhotos";
    public string? FfmpegExecutableDirectory { get; set; }
}