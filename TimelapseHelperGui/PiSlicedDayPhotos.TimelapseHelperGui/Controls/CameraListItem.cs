using Metalama.Patterns.Observability;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[Observable]
public partial class CameraListItem
{
    public bool Selected { get; set; }
    public string CameraName { get; set; } = "(None)";
    public int PhotoCount { get; set; }
    public DateTime? StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
}