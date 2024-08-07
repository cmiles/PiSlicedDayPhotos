namespace PiSlicedDayPhotos.TimelapseHelperTools;

public class TimeDescriptionPhotoSet
{
    public required DateTime ReferenceDateTime { get; set; }

    public List<PiSlicedDayPhotoInformation> Photos { get; set; } = [];
}