namespace PiSlicedDayPhotos.TimelapseHelperTools;

public class TimeDescriptionGroup
{
    public required string TimeDescription { get; set; }

    public List<TimeDescriptionPhotoSet> PhotoSets { get; set; } = [];
}