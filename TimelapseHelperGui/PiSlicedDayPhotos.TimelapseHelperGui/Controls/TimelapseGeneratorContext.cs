using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TimelapseGeneratorContext
{
    public TimelapseGeneratorContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public bool CameraCombinedMode { get; set; }
    public bool SliceCombinedMode { get; set; }
    public required ObservableCollection<PiSlicedDayPhotoInformation> SourcePhotos { get; set; }
    public required ObservableCollection<PiSlicedDayPhotoInformation> SelectedPhotos { get; set; }
    public required ObservableCollection<CameraListItem> CameraItems { get; set; }
    public required ObservableCollection<TimeDescriptionListItem> TimeDescriptionItems { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string SourceFolder { get; set; } = string.Empty;
    public int FrameRate { get; set; } = 6;
    public int NumberOfSelectedPhotos { get; set; }
    public DateTime? SelectedPhotosStartOn { get; set; }
    public DateTime? SelectedPhotosEndOn { get; set; }

    public required ConversionDataEntryContext<DateTime?> TimeLapseStartsOnEntry { get; set; }
    public required ConversionDataEntryContext<DateTime?> TimeLapseEndsOnEntry { get; set; }

    public static async Task<TimelapseGeneratorContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var factoryCameraItems = new ObservableCollection<CameraListItem>();
        var factoryTimeDescriptionItems = new ObservableCollection<TimeDescriptionListItem>();
        var factorySourcePhotos = new ObservableCollection<PiSlicedDayPhotoInformation>();
        var factorySelectedPhotos = new ObservableCollection<PiSlicedDayPhotoInformation>();

        var factoryStartsOnEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        factoryStartsOnEntry.Title = "Photos After - Blank will start with the Earliest Possible Photo";
        factoryStartsOnEntry.HelpText = "Only include photos taken on or after this date.";

        var factoryEndsOnEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        factoryEndsOnEntry.Title = "Photos Before - Blank will start with the Last Possible Photo";
        factoryEndsOnEntry.HelpText = "Only include photos taken on or before this date.";

        var newControl = new TimelapseGeneratorContext
        {
            CameraItems = factoryCameraItems,
            TimeDescriptionItems = factoryTimeDescriptionItems,
            SourcePhotos = factorySourcePhotos,
            SelectedPhotos = factorySelectedPhotos,
            StatusContext = statusContext,
            TimeLapseStartsOnEntry = factoryStartsOnEntry,
            TimeLapseEndsOnEntry = factoryEndsOnEntry
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        newControl.BuildCommands();
        var settings = TimelapseHelperGuiSettingsTools.ReadSettings();
        newControl.SourceFolder = settings.LastInputDirectory ?? string.Empty;

        newControl.TimeLapseStartsOnEntry.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ConversionDataEntryContext<DateTime?>.UserValue))
                newControl.StatusContext.RunNonBlockingTask(newControl.UpdateSelectedPhotos);
        };

        newControl.TimeLapseEndsOnEntry.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ConversionDataEntryContext<DateTime?>.UserValue))
                newControl.StatusContext.RunNonBlockingTask(newControl.UpdateSelectedPhotos);
        };

        return newControl;
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
        CameraItems.Clear();
        TimeDescriptionItems.Clear();

        if (string.IsNullOrWhiteSpace(SourceFolder) || !Directory.Exists(SourceFolder)) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var sourceDirectory = new DirectoryInfo(SourceFolder);

        var allImages = sourceDirectory.EnumerateFiles("*--*.jpg").ToList();

        var sourcePhotos = new List<PiSlicedDayPhotoInformation>();

        foreach (var loopImage in allImages)
        {
            var initialSplit = Path.GetFileNameWithoutExtension(loopImage.FullName).Split("--");

            var dateString = initialSplit[0].Substring(0, 16);
            var descriptionString = initialSplit[0].Substring(17, initialSplit[0].Length - 17);
            var cameraString = initialSplit[1];

            sourcePhotos.Add(new PiSlicedDayPhotoInformation
            {
                FileName = loopImage.FullName,
                TakenOn = DateTime.ParseExact(dateString, "yyyy-MM-dd-HH-mm", CultureInfo.InvariantCulture),
                Camera = cameraString,
                Description = descriptionString
            });
        }

        var cameras = sourcePhotos.Select(x => x.Camera).Distinct().OrderBy(x => x).ToList();
        var timeDescriptions = sourcePhotos.Select(x => x.Description).Distinct().OrderBy(x => x).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        var defaultCameraOrder = 0;
        cameras.ForEach(x => CameraItems.Add(new CameraListItem
        {
            CameraName = x, PhotoCount = sourcePhotos.Count(y => y.Camera.Equals(x)), Order = defaultCameraOrder++,
            StartsOn = sourcePhotos.Where(y => y.Camera.Equals(x)).MinBy(y => y.TakenOn)?.TakenOn,
            EndsOn = sourcePhotos.Where(y => y.Camera.Equals(x)).MaxBy(y => y.TakenOn)?.TakenOn
        }));

        foreach (var loopCameraItems in CameraItems)
            loopCameraItems.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(CameraListItem.Selected))
                    StatusContext.RunNonBlockingTask(UpdateSelectedPhotos);
            };

        timeDescriptions.ForEach(x => TimeDescriptionItems.Add(new TimeDescriptionListItem
        {
            TimeDescription = x, PhotoCount = sourcePhotos.Count(y => y.Description.Equals(x)),
            StartsOn = sourcePhotos.Where(y => y.Description.Equals(x)).MinBy(y => y.TakenOn)?.TakenOn,
            EndsOn = sourcePhotos.Where(y => y.Description.Equals(x)).MaxBy(y => y.TakenOn)?.TakenOn
        }));

        foreach (var loopDescriptions in TimeDescriptionItems)
            loopDescriptions.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(CameraListItem.Selected))
                    StatusContext.RunNonBlockingTask(UpdateSelectedPhotos);
            };

        sourcePhotos.ForEach(x => SourcePhotos.Add(x));

        NumberOfSelectedPhotos = 0;
        SelectedPhotosStartOn = null;
        SelectedPhotosEndOn = null;
    }

    private async Task UpdateSelectedPhotos()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selectedCameraNames = SelectedCameraItems().Select(x => x.CameraName).ToList();
        var selectedTimeDescriptionNames = SelectedTimeDescriptionItems().Select(x => x.TimeDescription).ToList();

        var selectedPhotos = SourcePhotos.Where(x =>
            selectedCameraNames.Contains(x.Camera) &&
            selectedTimeDescriptionNames.Contains(x.Description)).ToList();

        if (TimeLapseStartsOnEntry is { HasValidationIssues: false, UserValue: not null })
            selectedPhotos = selectedPhotos.Where(x => x.TakenOn >= TimeLapseStartsOnEntry.UserValue.Value).ToList();

        if (TimeLapseEndsOnEntry is { HasValidationIssues: false, UserValue: not null })
            selectedPhotos = selectedPhotos.Where(x => x.TakenOn <= TimeLapseEndsOnEntry.UserValue.Value).ToList();

        NumberOfSelectedPhotos = selectedPhotos.Count;
        SelectedPhotosStartOn = selectedPhotos.MinBy(x => x.TakenOn)?.TakenOn;
        SelectedPhotosEndOn = selectedPhotos.MaxBy(x => x.TakenOn)?.TakenOn;

        await ThreadSwitcher.ResumeForegroundAsync();

        SelectedPhotos.Clear();
        selectedPhotos.OrderBy(x => x.TakenOn).ThenBy(x => x.Camera).ThenBy(x => x.Description).ToList()
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

    public List<CameraListItem> SelectedCameraItems()
    {
        if (!CameraItems.Any()) return new List<CameraListItem>();

        return CameraItems.Where(x => x.Selected).ToList();
    }

    public List<TimeDescriptionListItem> SelectedTimeDescriptionItems()
    {
        if (!TimeDescriptionItems.Any()) return new List<TimeDescriptionListItem>();

        return TimeDescriptionItems.Where(x => x.Selected).ToList();
    }


    [BlockingCommand]
    public async Task CreateTimelapse()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedCameraItems().Count == 0)
        {
            StatusContext.ToastWarning("No Cameras Selected?");
            return;
        }

        if (SelectedTimeDescriptionItems().Count == 0)
        {
            StatusContext.ToastWarning("No Time Descriptions Selected?");
            return;
        }

        var selectedCameraNames = SelectedCameraItems().Select(x => x.CameraName).ToList();
        var selectedTimeDescriptions = SelectedTimeDescriptionItems().Select(x => x.TimeDescription).ToList();

        var timeDescriptionGroups = new List<TimeDescriptionGroup>();

        foreach (var loopDescription in selectedTimeDescriptions)
        {
            var group = new TimeDescriptionGroup { TimeDescription = loopDescription };
            timeDescriptionGroups.Add(group);

            var relatedPhotos = SourcePhotos
                .Where(x => x.Description == loopDescription && selectedCameraNames.Contains(x.Camera)).ToList();

            foreach (var photo in relatedPhotos)
            {
                var existingSet = group.PhotoSets.FirstOrDefault(x =>
                    Math.Abs(x.ReferenceDateTime.Subtract(photo.TakenOn.Date).TotalMinutes) <= 2);

                if (existingSet == null)
                {
                    existingSet = new TimeDescriptionPhotoSet { ReferenceDateTime = photo.TakenOn.Date };
                    group.PhotoSets.Add(existingSet);
                }

                existingSet.Photos.Add(photo);
            }
        }

        var cameraOrderLookup = CameraItems.ToDictionary(x => x.CameraName, x => x.Order);

        if ((!CameraCombinedMode && !SliceCombinedMode) ||
            (selectedCameraNames.Count == 1 && selectedTimeDescriptions.Count == 1))
        {
            var orderedPhotos = timeDescriptionGroups.SelectMany(x => x.PhotoSets).OrderBy(x => x.ReferenceDateTime)
                .SelectMany(x => x.Photos.OrderBy(y => cameraOrderLookup[y.Camera]).Select(y => y)).ToList();

            var tempStorageDirectory = FileHelpers.UniqueTimeLapseStorageDirectory();

            var counter = 1;

            foreach (var loopTimelapsePhotos in orderedPhotos)
                File.Copy(loopTimelapsePhotos.FileName,
                    Path.Combine(tempStorageDirectory.FullName, $"z_timelapse--{counter++:D6}.jpg"));

            var settings = TimelapseHelperGuiSettingsTools.ReadSettings();

            var ffmpegDirectory = string.IsNullOrWhiteSpace(settings.FfmpegExecutableDirectory) ||
                                  !Directory.Exists(settings.FfmpegExecutableDirectory)
                ? string.Empty
                : settings.FfmpegExecutableDirectory;
            var ffmpegExe = string.IsNullOrWhiteSpace(ffmpegDirectory)
                ? "ffmpeg"
                : Path.Combine(ffmpegDirectory, "ffmpeg.exe");

            var resultFile = $"Timelapse-Created-{DateTime.Now:yyyy-MM-dd HH-mm}.mp4";

            var command =
                $"""
                 cd '{tempStorageDirectory.FullName}'
                 {ffmpegExe} -framerate {FrameRate} -i z_timelapse--%06d.jpg -r 30 '{resultFile}'
                 """;

            var runResult = await PowerShellRun.ExecuteScript(command, StatusContext.ProgressTracker());

            if (runResult.Item1)
            {
                await StatusContext.ShowMessageWithOkButton($"Error Creating Timelapse",
                    string.Join(Environment.NewLine, runResult.runLog));
                return;
            }

            var argument = $"/select, \"{resultFile}\"";

            await ThreadSwitcher.ResumeForegroundAsync();
            Process.Start("explorer.exe", argument);
        }
    }

    public class TimeDescriptionGroup
    {
        public required string TimeDescription { get; set; }

        public List<TimeDescriptionPhotoSet> PhotoSets { get; set; } = [];
    }

    public class TimeDescriptionPhotoSet
    {
        public required DateTime ReferenceDateTime { get; set; }

        public List<PiSlicedDayPhotoInformation> Photos { get; set; } = [];
    }
}