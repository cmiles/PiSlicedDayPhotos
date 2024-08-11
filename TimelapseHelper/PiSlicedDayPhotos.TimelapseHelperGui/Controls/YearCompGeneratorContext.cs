using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Metalama.Patterns.Observability;
using Ookii.Dialogs.Wpf;
using PiSlicedDayPhotos.TimelapseHelperTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[Observable]
[GenerateStatusCommands]
public partial class YearCompGeneratorContext
{
    public YearCompGeneratorContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public required ObservableCollection<PiSlicedDayPhotoInformation> SourcePhotos { get; set; }
    public required ObservableCollection<PiSlicedDayPhotoInformation> SelectedPhotos { get; set; }
    public required ObservableCollection<SeriesListItem> SeriesItems { get; set; }
    public required ObservableCollection<TimeDescriptionListItem> TimeDescriptionItems { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string SourceFolder { get; set; } = string.Empty;
    public int NumberOfSelectedPhotos { get; set; }
    public DateTime? SelectedPhotosStartOn { get; set; }
    public DateTime? SelectedPhotosEndOn { get; set; }
    public required BoolDataEntryNoChangeIndicatorContext WriteCaptionDataEntry { get; set; }
    public required StringDataEntryNoChangeIndicatorContext CaptionFormatEntry { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> FrameRateDataEntry { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<int> CaptionFontSizeEntry { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<DateTime?> MainTimelineStartsOnEntry { get; set; }
    public required ConversionDataEntryNoChangeIndicatorContext<DateTime?> MainTimelineEndsOnEntry { get; set; }
    public string? CaptionFormatSample { get; set; } = string.Empty;

    public static async Task<YearCompGeneratorContext> CreateInstance(
        StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var factorySeriesItems = new ObservableCollection<SeriesListItem>();
        var factoryTimeDescriptionItems = new ObservableCollection<TimeDescriptionListItem>();
        var factorySourcePhotos = new ObservableCollection<PiSlicedDayPhotoInformation>();
        var factorySelectedPhotos = new ObservableCollection<PiSlicedDayPhotoInformation>();

        var factoryStartsOnEntry =
            await ConversionDataEntryNoChangeIndicatorContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        factoryStartsOnEntry.Title = "Main Year After";
        factoryStartsOnEntry.HelpText = "Only include photos taken on or after this date for the main (last) timeline of the comp series.";

        var factoryEndsOnEntry =
            await ConversionDataEntryNoChangeIndicatorContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        factoryEndsOnEntry.Title = "Main Year Before";
        factoryEndsOnEntry.HelpText = "Only include photos taken on or before this date for the main (last) timeline of the comp series.";

        var factoryFrameRateEntry = await ConversionDataEntryNoChangeIndicatorContext<int>.CreateInstance(
            ConversionDataEntryHelpers
                .IntConversion);
        factoryFrameRateEntry.Title = "Frame Rate";
        factoryFrameRateEntry.HelpText = "Frames per Second for the Timelapse";
        factoryFrameRateEntry.UserText = "6";

        var factoryCaptionFontSizeEntry = await ConversionDataEntryNoChangeIndicatorContext<int>.CreateInstance(
            ConversionDataEntryHelpers
                .IntConversion);
        factoryCaptionFontSizeEntry.Title = "Caption Font Size";
        factoryCaptionFontSizeEntry.HelpText = "Font Size for the Caption";
        factoryCaptionFontSizeEntry.UserText = "36";

        var factoryWriteCaptionEntry = await BoolDataEntryNoChangeIndicatorContext.CreateInstance();
        factoryWriteCaptionEntry.Title = "Write Caption";
        factoryWriteCaptionEntry.HelpText =
            "If checked a caption with the photo date will be added to the image/timelapse - use the Caption format string to control what is written.";
        factoryWriteCaptionEntry.UserValue = true;

        var factoryCaptionFormatEntry = StringDataEntryNoChangeIndicatorContext.CreateInstance();
        factoryCaptionFormatEntry.Title = "Caption Format";
        factoryCaptionFormatEntry.HelpText =
            "A string that will be used to format the photo datetime for the caption.";
        factoryCaptionFormatEntry.UserValue = "yyyy MMMM";

        var factoryYearComparisonEntry = await BoolDataEntryNoChangeIndicatorContext.CreateInstance();
        factoryYearComparisonEntry.Title = "Year Comparison";
        factoryYearComparisonEntry.HelpText =
            "If checked the timelapse will be created with a comparison of the same day from the previous year.";

        var newControl = new YearCompGeneratorContext
        {
            SeriesItems = factorySeriesItems,
            TimeDescriptionItems = factoryTimeDescriptionItems,
            SourcePhotos = factorySourcePhotos,
            SelectedPhotos = factorySelectedPhotos,
            StatusContext = statusContext,
            MainTimelineStartsOnEntry = factoryStartsOnEntry,
            MainTimelineEndsOnEntry = factoryEndsOnEntry,
            FrameRateDataEntry = factoryFrameRateEntry,
            CaptionFormatEntry = factoryCaptionFormatEntry,
            WriteCaptionDataEntry = factoryWriteCaptionEntry,
            CaptionFontSizeEntry = factoryCaptionFontSizeEntry
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        newControl.BuildCommands();
        var settings = TimelapseHelperGuiSettingsTools.ReadSettings();
        newControl.SourceFolder = settings.LastInputDirectory ?? string.Empty;

        newControl.MainTimelineStartsOnEntry.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConversionDataEntryContext<DateTime?>.UserValue))
                newControl.StatusContext.RunNonBlockingTask(newControl.UpdateSelectedPhotos);
        };

        newControl.MainTimelineEndsOnEntry.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConversionDataEntryContext<DateTime?>.UserValue))
                newControl.StatusContext.RunNonBlockingTask(newControl.UpdateSelectedPhotos);
        };

        newControl.CaptionFormatEntry.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StringDataEntryContext.UserValue))
                newControl.StatusContext.RunNonBlockingTask(newControl.UpdateCaptionFormatSample);
        };

        newControl.StatusContext.RunFireAndForgetNonBlockingTask(newControl.UpdateCaptionFormatSample);

        return newControl;
    }

    private async Task UpdateCaptionFormatSample()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var captionFormat = CaptionFormatEntry.UserValue;

        try
        {
            CaptionFormatSample = DateTime.Now.ToString(captionFormat);
        }
        catch (Exception)
        {
            CaptionFormatSample = "(Error in Format!)";
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(SourceFolder))
            StatusContext.RunBlockingTask(UpdateSourcePhotoInformation);
    }

    private async Task UpdateSourcePhotoInformation()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SourcePhotos.Clear();
        SelectedPhotos.Clear();
        SeriesItems.Clear();
        TimeDescriptionItems.Clear();

        if (string.IsNullOrWhiteSpace(SourceFolder) || !Directory.Exists(SourceFolder)) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var sourcePhotos = PiSlicedDayPhotoTools.ProcessDirectory(SourceFolder, StatusContext.ProgressTracker());

        var series = sourcePhotos.Select(x => x.Series).Distinct().OrderBy(x => x).ToList();
        var timeDescriptions = sourcePhotos.Select(x => x.Description).Distinct().OrderBy(x => x).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        series.ForEach(x => SeriesItems.Add(new SeriesListItem
        {
            SeriesName = x,
            PhotoCount = sourcePhotos.Count(y => y.Series.Equals(x)),
            StartsOn = sourcePhotos.Where(y => y.Series.Equals(x)).MinBy(y => y.TakenOn)?.TakenOn,
            EndsOn = sourcePhotos.Where(y => y.Series.Equals(x)).MaxBy(y => y.TakenOn)?.TakenOn
        }));

        foreach (var loopSeriesItems in SeriesItems)
            loopSeriesItems.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SeriesListItem.Selected))
                    StatusContext.RunNonBlockingTask(UpdateSelectedPhotos);
            };

        timeDescriptions.ForEach(x => TimeDescriptionItems.Add(new TimeDescriptionListItem
        {
            TimeDescription = x,
            PhotoCount = sourcePhotos.Count(y => y.Description.Equals(x)),
            StartsOn = sourcePhotos.Where(y => y.Description.Equals(x)).MinBy(y => y.TakenOn)?.TakenOn,
            EndsOn = sourcePhotos.Where(y => y.Description.Equals(x)).MaxBy(y => y.TakenOn)?.TakenOn
        }));

        foreach (var loopDescriptions in TimeDescriptionItems)
            loopDescriptions.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SeriesListItem.Selected))
                {
                    //Set other TimeDescription items to Selected = false
                    if (loopDescriptions.Selected)
                        foreach (var loopTimeDescriptionItems in TimeDescriptionItems)
                            if (loopTimeDescriptionItems != loopDescriptions)
                                loopTimeDescriptionItems.Selected = false;
                    StatusContext.RunNonBlockingTask(UpdateSelectedPhotos);
                }
            };

        sourcePhotos.ForEach(x => SourcePhotos.Add(x));

        NumberOfSelectedPhotos = 0;
        SelectedPhotosStartOn = null;
        SelectedPhotosEndOn = null;
    }

    private async Task UpdateSelectedPhotos()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MainTimelineStartsOnEntry.HasValidationIssues || MainTimelineStartsOnEntry.UserValue is null)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SelectedPhotos.Clear();

            return;
        }

        if (MainTimelineEndsOnEntry.HasValidationIssues || MainTimelineEndsOnEntry.UserValue is null)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            SelectedPhotos.Clear();

            return;
        }

        var selectedSeriesNames = SelectedSeriesItems().Select(x => x.SeriesName).ToList();
        var selectedTimeDescriptionNames = SelectedTimeDescriptionItems().Select(x => x.TimeDescription).ToList();

        var mainPhotos = SourcePhotos.Where(x =>
            selectedSeriesNames.Contains(x.Series) &&
            selectedTimeDescriptionNames.Contains(x.Description) &&
            x.TakenOn >= MainTimelineStartsOnEntry.UserValue.Value &&
            x.TakenOn <= MainTimelineEndsOnEntry.UserValue.Value).ToList();

        var compPhotos = SourcePhotos.Where(x =>
            selectedSeriesNames.Contains(x.Series) &&
            selectedTimeDescriptionNames.Contains(x.Description) &&
            x.TakenOn >= MainTimelineStartsOnEntry.UserValue.Value.AddDays(-366) &&
            x.TakenOn <= MainTimelineEndsOnEntry.UserValue.Value.AddDays(-362)).ToList();

        var selectedPhotos = mainPhotos.Concat(compPhotos).ToList();

        NumberOfSelectedPhotos = selectedPhotos.Count;
        SelectedPhotosStartOn = selectedPhotos.MinBy(x => x.TakenOn)?.TakenOn;
        SelectedPhotosEndOn = selectedPhotos.MaxBy(x => x.TakenOn)?.TakenOn;

        await ThreadSwitcher.ResumeForegroundAsync();

        SelectedPhotos.Clear();
        selectedPhotos.OrderBy(x => x.TakenOn).ThenBy(x => x.Series).ThenBy(x => x.Description).ToList()
            .ForEach(x => SelectedPhotos.Add(x));
    }

    [BlockingCommand]
    public async Task ChooseSourceFolder()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var settings = TimelapseHelperGuiSettingsTools.ReadSettings();
        var lastDirectory =
            !string.IsNullOrWhiteSpace(settings.LastInputDirectory) && Directory.Exists(settings.LastInputDirectory)
                ? settings.LastInputDirectory.Trim()
                : string.Empty;

        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = true };

        if (!string.IsNullOrWhiteSpace(lastDirectory)) folderPicker.SelectedPath = $"{lastDirectory}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        if (!folderPicker.SelectedPaths.Any())
        {
            StatusContext.ToastWarning("No directories selected?");
            return;
        }

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPaths[0]);

        settings.LastInputDirectory = selectedDirectory.FullName;

        await TimelapseHelperGuiSettingsTools.WriteSettings(settings);

        SourceFolder = selectedDirectory.FullName;
    }

    public List<SeriesListItem> SelectedSeriesItems()
    {
        if (!SeriesItems.Any()) return new List<SeriesListItem>();

        return SeriesItems.Where(x => x.Selected).ToList();
    }

    public List<TimeDescriptionListItem> SelectedTimeDescriptionItems()
    {
        if (!TimeDescriptionItems.Any()) return new List<TimeDescriptionListItem>();

        return TimeDescriptionItems.Where(x => x.Selected).ToList();
    }

    [BlockingCommand]
    public async Task WriteTimelapseFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var shouldRunCheck = await CheckCanCreateTimelapseAndFfmpegExe();

        if (!shouldRunCheck.Item1) return;

        var selectedSeriesNames = SelectedSeriesItems().Select(x => x.SeriesName).ToList();
        var selectedTimeDescriptions = SelectedTimeDescriptionItems().Select(x => x.TimeDescription).ToList();

        StatusContext.Progress($"Cameras: {string.Join(", ", selectedSeriesNames)}");
        StatusContext.Progress($"Time Descriptions: {string.Join(", ", selectedTimeDescriptions)}");

        var seriesOrder = new Dictionary<string, int>();

        foreach (var loopSelectedSeriesNames in selectedSeriesNames)
            seriesOrder.Add(loopSelectedSeriesNames,
                SeriesItems.IndexOf(SeriesItems.First(x => x.SeriesName == loopSelectedSeriesNames)));

        var result = YearCompSingleTimeDescription.YearCompSingleTimeDescriptionTimelapseFiles(SelectedPhotos.ToList(),
            MainTimelineStartsOnEntry.UserValue.Value, MainTimelineEndsOnEntry.UserValue.Value,
            FrameRateDataEntry.UserValue, seriesOrder, shouldRunCheck.Item2, StatusContext.ProgressTracker(),
            WriteCaptionDataEntry.UserValue, CaptionFormatEntry.UserValue, CaptionFontSizeEntry.UserValue);

        if (Directory.Exists(result)) await OpenExplorerWindowForDirectory(result);
    }

    private async Task<(bool, string)> CheckCanCreateTimelapseAndFfmpegExe()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MainTimelineStartsOnEntry.HasValidationIssues || MainTimelineStartsOnEntry.UserValue is null)
        {
            StatusContext.ToastError("Please fill in a Start Date/Time for the most recent year photo set");
            return (false, string.Empty);
        }

        if (MainTimelineEndsOnEntry.HasValidationIssues || MainTimelineEndsOnEntry.UserValue is null)
        {
            StatusContext.ToastError("Please fill in an End Date/Time for the most recent year photo set");
            return (false, string.Empty);
        }

        if (SelectedSeriesItems().Count == 0)
        {
            StatusContext.ToastError("No Series Selected?");
            return (false, string.Empty);
        }

        if (SelectedTimeDescriptionItems().Count == 0)
        {
            StatusContext.ToastError("No Time Descriptions Selected?");
            return (false, string.Empty);
        }

        if (SelectedTimeDescriptionItems().Count > 1)
        {
            StatusContext.ToastError("Please select a single Time Description");
            return (false, string.Empty);
        }

        if (FrameRateDataEntry.HasValidationIssues)
        {
            StatusContext.ToastError("Frame Rate Entry Issues?");
            return (false, string.Empty);
        }

        if (!SelectedPhotos.Any())
        {
            StatusContext.ToastError("The current settings don't include any photos?");
            return (false, string.Empty);
        }

        var settings = TimelapseHelperGuiSettingsTools.ReadSettings();

        var ffmpegDirectory = string.IsNullOrWhiteSpace(settings.FfmpegExecutableDirectory) ||
                              !Directory.Exists(settings.FfmpegExecutableDirectory)
            ? string.Empty
            : settings.FfmpegExecutableDirectory;
        var ffmpegExe = string.IsNullOrWhiteSpace(ffmpegDirectory)
            ? "ffmpeg"
            : Path.Combine(ffmpegDirectory, "ffmpeg.exe");

        if (!File.Exists(ffmpegExe))
        {
            StatusContext.ToastError("FFMPEG Executable Not Found?");
            return (false, string.Empty);
        }

        return (true, ffmpegExe);
    }

    [BlockingCommand]
    public async Task CreateTimelapse()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var shouldRunCheck = await CheckCanCreateTimelapseAndFfmpegExe();

        if (!shouldRunCheck.Item1) return;

        var selectedSeriesNames = SelectedSeriesItems().Select(x => x.SeriesName).ToList();
        var selectedTimeDescriptions = SelectedTimeDescriptionItems().Select(x => x.TimeDescription).ToList();

        StatusContext.Progress($"Cameras: {string.Join(", ", selectedSeriesNames)}");
        StatusContext.Progress($"Time Descriptions: {string.Join(", ", selectedTimeDescriptions)}");

        var seriesOrder = new Dictionary<string, int>();

        foreach (var loopSelectedSeriesNames in selectedSeriesNames)
            seriesOrder.Add(loopSelectedSeriesNames,
                SeriesItems.IndexOf(SeriesItems.First(x => x.SeriesName == loopSelectedSeriesNames)));

        var result = await YearCompSingleTimeDescription.YearCompSingleTimeDescriptionTimelapse(SelectedPhotos.ToList(),
            MainTimelineStartsOnEntry.UserValue.Value, MainTimelineEndsOnEntry.UserValue.Value,
            FrameRateDataEntry.UserValue, seriesOrder, shouldRunCheck.Item2, StatusContext.ProgressTracker(),
            WriteCaptionDataEntry.UserValue, CaptionFormatEntry.UserValue, CaptionFontSizeEntry.UserValue);

        if (File.Exists(result.resultFile)) await OpenExplorerWindowForFile(result.resultFile);

        if (result.errors)
            await StatusContext.ShowMessageWithOkButton("Error Creating Timelapse",
                string.Join(Environment.NewLine, result.runLog));
    }

    public static async Task OpenExplorerWindowForFile(string fileName)
    {
        if (!File.Exists(fileName)) return;
        //Clean up file path so it can be navigated OK
        var cleanedPath = Path.GetFullPath(fileName);

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo("explorer.exe", $"/select, \"{cleanedPath}\"")
        {
            UseShellExecute = true,
            Verb = "open"
        };

        Process.Start(ps);
    }

    public static async Task OpenExplorerWindowForDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo
        {
            FileName = directoryPath,
            UseShellExecute = true,
            Verb = "open"
        };

        Process.Start(ps);
    }
}