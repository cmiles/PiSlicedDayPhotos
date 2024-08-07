namespace PiSlicedDayPhotos.TimelapseHelperTools;

public class PiSlicedDayPhotoInformation
{
    public required string FileName { get; set; }
    public required DateTime TakenOn { get; set; }
    public required string Series { get; set; }
    public required string Description { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
}