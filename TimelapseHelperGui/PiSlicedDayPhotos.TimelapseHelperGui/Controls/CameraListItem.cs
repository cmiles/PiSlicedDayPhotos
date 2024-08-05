using PointlessWaymarks.LlamaAspects;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[NotifyPropertyChanged]
public partial class CameraListItem
{
    public bool Selected { get; set; }
    public string CameraName { get; set; } = "(None)";
    public required int Order { get; set; }
    public int PhotoCount { get; set; }
    public DateTime? StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
}