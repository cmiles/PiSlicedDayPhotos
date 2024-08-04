using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TimelapseGeneratorContext
{
    public TimelapseGeneratorContext()
    {
        BuildCommands();
        PropertyChanged += OnPropertyChanged;
    }

    public bool CameraCombinedMode { get; set; }
    public bool SliceCombinedMode { get; set; }
    public required ObservableCollection<PiSlicedDayPhotoInformation> SourcePhotoInformation { get; set; }
    public required ObservableCollection<CameraListItem> CameraItems { get; set; }
    public required ObservableCollection<TimeDescriptionListItem> TimeDescriptionItems { get; set; }
    public List<CameraListItem> SelectedCameraItems { get; set; } = [];
    public List<TimeDescriptionListItem> SelectedTimeDescriptionItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }
    public string SourceFolder { get; set; } = string.Empty;

    public int FrameRate { get; set; } = 6;

    public static async Task CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var factoryCameraItems = new ObservableCollection<CameraListItem>();
        var factoryTimeDescriptionItems = new ObservableCollection<TimeDescriptionListItem>();
        var factorySourcePhotoInformation = new ObservableCollection<PiSlicedDayPhotoInformation>();

        var newControl = new TimelapseGeneratorContext
        {
            CameraItems = factoryCameraItems,
            TimeDescriptionItems = factoryTimeDescriptionItems,
            SourcePhotoInformation = factorySourcePhotoInformation,
            StatusContext = statusContext
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        var settings = TimelapseHelperGuiSettingsTools.ReadSettings();
        newControl.SourceFolder = settings.LastInputDirectory ?? string.Empty;
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
        SourcePhotoInformation.Clear();
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
        cameras.ForEach(x => CameraItems.Add(new CameraListItem { CameraName = x, Order = defaultCameraOrder++ }));
        timeDescriptions.ForEach(x => TimeDescriptionItems.Add(new TimeDescriptionListItem { TimeDescription = x }));
        sourcePhotos.ForEach(x => SourcePhotoInformation.Add(x));
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

    [BlockingCommand]
    public async Task CreateTimelapse()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedCameraItems.Count == 0)
        {
            StatusContext.ToastWarning("No Cameras Selected?");
            return;
        }

        if (SelectedTimeDescriptionItems.Count == 0)
        {
            StatusContext.ToastWarning("No Time Descriptions Selected?");
            return;
        }

        var selectedCameraNames = SelectedCameraItems.Select(x => x.CameraName).ToList();
        var selectedTimeDescriptions = SelectedTimeDescriptionItems.Select(x => x.TimeDescription).ToList();

        var timeDescriptionGroups = new List<TimeDescriptionGroup>();

        foreach (var loopDescription in selectedTimeDescriptions)
        {
            var group = new TimeDescriptionGroup { TimeDescription = loopDescription };
            timeDescriptionGroups.Add(group);

            var relatedPhotos = SourcePhotoInformation.Where(x => x.Description == loopDescription).ToList();

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

            var command =
                $"-framerate {FrameRate} -i z_timelapse--%06d.jpg -r 30 Timelapse-Created {DateTime.Now:yyyy-MM-dd HH-mm}.mp4";

            await ProcessTools.Execute(ffmpegExe, command, tempStorageDirectory.FullName,
                StatusContext.ProgressTracker());
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