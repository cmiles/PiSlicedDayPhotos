using System.ComponentModel;
using System.IO;
using Metalama.Patterns.Observability;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[Observable]
public partial class AppSettingsContext
{
    public AppSettingsContext()
    {
        PropertyChanged += AppSettingsContext_PropertyChanged;
    }

    public required string ProgramUpdateLocation { get; set; }
    public bool ShowUpdateLocationExistsWarning { get; set; }
    public bool ShowFfmpegLocationWarning { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public required string FfmpegExecutableDirectory { get; set; }

    private void AppSettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (nameof(ProgramUpdateLocation).Equals(e.PropertyName))
        {
            ValidateProgramUpdateLocation();

#pragma warning disable CS4014
            if (!ShowUpdateLocationExistsWarning)
            {
                var currentSettings = TimelapseHelperGuiSettingsTools.ReadSettings();
                currentSettings.ProgramUpdateDirectory = ProgramUpdateLocation;
                //Allow call to continue without waiting and write settings
                TimelapseHelperGuiSettingsTools.WriteSettings(currentSettings);
            }

#pragma warning restore CS4014
        }

        if (nameof(FfmpegExecutableDirectory).Equals(e.PropertyName))
        {
            ValidateFfmpegLocation();

            if (!ShowFfmpegLocationWarning)
            {
#pragma warning disable CS4014
                var currentSettings = TimelapseHelperGuiSettingsTools.ReadSettings();
                currentSettings.ProgramUpdateDirectory = ProgramUpdateLocation;
                //Allow call to continue without waiting and write settings
                TimelapseHelperGuiSettingsTools.WriteSettings(currentSettings);
#pragma warning restore CS4014
            }
        }
    }

    public static async Task<AppSettingsContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factorySettings = TimelapseHelperGuiSettingsTools.ReadSettings();

        var factoryContext = new AppSettingsContext
        {
            StatusContext = statusContext,
            ProgramUpdateLocation = factorySettings.ProgramUpdateDirectory,
            FfmpegExecutableDirectory = factorySettings.FfmpegExecutableDirectory ?? string.Empty
        };

        factoryContext.ValidateProgramUpdateLocation();

        return factoryContext;
    }

    private void ValidateProgramUpdateLocation()
    {
        if (string.IsNullOrWhiteSpace(ProgramUpdateLocation)) ShowUpdateLocationExistsWarning = false;

        ShowUpdateLocationExistsWarning = !Directory.Exists(ProgramUpdateLocation);
    }

    private void ValidateFfmpegLocation()
    {
        if (string.IsNullOrWhiteSpace(FfmpegExecutableDirectory)
            || !Directory.Exists(FfmpegExecutableDirectory)
            || !File.Exists(Path.Combine(FfmpegExecutableDirectory, "ffmpeg.exe")))
        {
            ShowFfmpegLocationWarning = true;
            return;
        }

        ShowFfmpegLocationWarning = false;
    }
}