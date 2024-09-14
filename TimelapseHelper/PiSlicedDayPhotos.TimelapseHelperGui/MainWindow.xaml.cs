using Metalama.Patterns.Observability;
using PiSlicedDayPhotos.TimelapseHelperGui.Controls;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PiSlicedDayPhotos.TimelapseHelperGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[Observable]
[GenerateStatusCommands]
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pi Sliced Day Photos - Timelapse Helper");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext();

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext(StatusContext);

        BuildCommands();

        StatusContext.RunFireAndForgetBlockingTask(async () => { await CheckForProgramUpdate(currentDateVersion); });
        StatusContext.RunFireAndForgetBlockingTask(Setup);
    }

    public TimelapseSingleTimeDescriptionGeneratorContext? SingleTimeDescriptionContext { get; set; }
    public YearCompGeneratorContext? YearCompContext { get; set; }
    public GridImageGeneratorContext? GridImageContext { get; set; }
    public AppSettingsContext? SettingsContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string InfoTitle { get; set; }

    private async Task Setup()
    {
        SingleTimeDescriptionContext = await TimelapseSingleTimeDescriptionGeneratorContext.CreateInstance(StatusContext);
        YearCompContext = await YearCompGeneratorContext.CreateInstance(StatusContext);
        GridImageContext = await GridImageGeneratorContext.CreateInstance(StatusContext);
        SettingsContext = await AppSettingsContext.CreateInstance(StatusContext);
    }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = TimelapseHelperGuiSettingsTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = await ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PiSlicedDayPhotosTimelapseHelperSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile ?? string.Empty}");

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }
}