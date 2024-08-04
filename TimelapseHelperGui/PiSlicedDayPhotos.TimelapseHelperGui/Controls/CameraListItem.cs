using PointlessWaymarks.LlamaAspects;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[NotifyPropertyChanged]
public partial class CameraListItem
{
    public bool IsSelected { get; set; }
    public string CameraName { get; set; } = "(None)";
    public required int Order { get; set; }
}