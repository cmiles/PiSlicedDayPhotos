namespace PiSlicedDayPhotos.Utility;

/// <summary>
/// Describes a Scheduled Photo - in the main photo worker the 'Kind' determines the arguments from the settings
/// file that are used to take the photo.
/// </summary>
public class ScheduledPhoto
{
    public PhotoKind Kind { get; init; }
    public DateTime ScheduledTime { get; init; }

    public string LibCameraParameters { get; init; } = string.Empty;
}