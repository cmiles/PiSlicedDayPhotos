namespace PiSlicedDayPhotos.Utility;

public class CustomTimeAndSettings
{
    public string Time { get; set; } = string.Empty;
    public string LibCameraParameters { get; set; } = string.Empty;
}

public class CustomTimeAndSettingsTranslated
{
    public DateTime Time { get; set; }
    public string LibCameraParameters { get; set; } = string.Empty;
}