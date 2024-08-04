using PointlessWaymarks.LlamaAspects;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[NotifyPropertyChanged]
public partial class TimeDescriptionListItem
{
    public bool IsSelected { get; set; }
    public string TimeDescription { get; set; } = "(None)";
}