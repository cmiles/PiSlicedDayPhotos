﻿namespace PiSlicedDayPhotos.Utility;

public class PiSlicedDaySettings
{
    public int DaySlices { get; set; }
    public int NightSlices { get; set; }
    public string PhotoNamePrefix { get; set; } = string.Empty;
    public string PhotoStorageDirectory { get; set; } = string.Empty;
    public string SunriseSunsetCsvFile { get; set; } = string.Empty;
}