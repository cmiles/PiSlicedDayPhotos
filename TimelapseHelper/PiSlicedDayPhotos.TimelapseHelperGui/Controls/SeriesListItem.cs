using Metalama.Patterns.Observability;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[Observable]
public partial class SeriesListItem
{
    public bool Selected { get; set; }
    public string SeriesName { get; set; } = "(None)";
    public int PhotoCount { get; set; }
    public DateTime? StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
}