namespace PiSlicedDayPhotos.Utility;

public class PiSlicedDaySettings
{
    public int DaySlices { get; set; }
    public string LibCameraParametersDay { get; set; } = string.Empty;
    public string LibCameraParametersNight { get; set; } = string.Empty;
    public string LibCameraParametersSunrise { get; set; } = string.Empty;
    public string LibCameraParametersSunset { get; set; } = string.Empty;
    public string LibCameraParametersPostSunset { get; set; } = string.Empty;
    public bool LogFullExceptionsToImages { get; set; } = true;
    public int NightSlices { get; set; }
    public string PhotoNamePrefix { get; set; } = string.Empty;
    public string PhotoStorageDirectory { get; set; } = string.Empty;
    public string SunriseSunsetCsvFile { get; set; } = string.Empty;
    public List<CustomTimeAndSettings> CustomTimes { get; set; } = new();
}