namespace PiSlicedDayPhotos.TimelapseHelperTools;

public class TimelapseHelperConsoleSettings
{
    public required string TimelapseType { get; set; }
    public string? StartOn { get; set; }
    public string? EndOn { get; set; }
    public required string InputDirectory { get; set; }
    public required string SeriesList { get; set; }
    public required string TimeDescriptionList { get; set; }
    public int Framerate { get; set; }
    public bool CaptionWithDateTime { get; set; }
    public string? CaptionFormatString { get; set; }
    public int? CaptionFontSize { get; set; }
    public required string FfmpegExe { get; set; }
}