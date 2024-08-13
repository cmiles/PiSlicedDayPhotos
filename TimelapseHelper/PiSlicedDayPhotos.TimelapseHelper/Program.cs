using System.Text.Json;
using PiSlicedDayPhotos.TimelapseHelper;
using PiSlicedDayPhotos.TimelapseHelperTools;
using PointlessWaymarks.CommonTools;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.WriteLine("Please provide a settings file as the only argument to this program.");
    return -1;
}

var rawFileName = args[0].Trim();

if (!File.Exists(rawFileName))
{
    Console.WriteLine($"Settings File {rawFileName} Not Found.");
    return -1;
}

var settings = JsonSerializer.Deserialize<TimelapseHelperConsoleSettings>(File.OpenRead(rawFileName));

if (settings is null)
{
    Console.WriteLine($"Could not parse {rawFileName} as a valid Json settings file.");
    return -1;
}

List<string> timelapseTypes =
[
    SeriesListGridImages.ConsoleSettingsIdentifier, SingleTimeDescription.ConsoleSettingsIdentifier,
    YearCompSingleTimeDescription.ConsoleSettingsIdentifier
];

if (string.IsNullOrWhiteSpace(settings.TimelapseType))
{
    Console.WriteLine($"TimelapseType can not be blank and must be one of {string.Join(", ", timelapseTypes)}");
    return -1;
}

if (!timelapseTypes.Contains(settings.TimelapseType, StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"TimelapseType must be one of {string.Join(", ", timelapseTypes)}");
    return -1;
}

if (!Directory.Exists(settings.InputDirectory))
{
    Console.WriteLine($"The input directory {settings.InputDirectory} can not be found.");
    return -1;
}

DateTime? startTime = null;

if (!string.IsNullOrWhiteSpace(settings.StartOn))
{
    startTime = DateTimeRecognizerTools.GetDateTime(settings.StartOn, true);
    if (startTime is null)
    {
        Console.WriteLine($"The StartOn {settings.StartOn} could not be translated to a DateTime.");
        return -1;
    }
}

DateTime? endTime = null;

if (!string.IsNullOrWhiteSpace(settings.EndOn))
{
    endTime = DateTimeRecognizerTools.GetDateTime(settings.EndOn, false);
    if (endTime is null)
    {
        Console.WriteLine($"The EndOn {settings.EndOn} could not be translated to a DateTime.");
        return -1;
    }
}

static List<string> SplitString(string input)
{
    if (string.IsNullOrEmpty(input)) return [];

    if (input.Contains(","))
        return [..input.Split(',')];
    else
        return [input];
}

if (string.IsNullOrWhiteSpace(settings.SeriesList))
{
    Console.WriteLine("SeriesList can not be blank.");
    return -1;
}

var seriesList = SplitString(settings.SeriesList);

if (string.IsNullOrWhiteSpace(settings.TimeDescriptionList))
{
    Console.WriteLine("TimeDescriptionList can not be blank.");
    return -1;
}

var timeDescriptionList = SplitString(settings.TimeDescriptionList);

if (settings.Framerate < 1 || settings.Framerate > 30)
{
    Console.WriteLine($"Framerate must be between 1 and 30 - input was {settings.Framerate}.");
    return -1;
}

if (settings.CaptionWithDateTime && string.IsNullOrWhiteSpace(settings.CaptionFormatString))
{
    Console.WriteLine("CaptionWithDateTime is true but CaptionFormatString is blank.");
    return -1;
}

if (settings is { CaptionWithDateTime: true, CaptionFontSize: null or < 1 })
{
    Console.WriteLine("CaptionWithDateTime is true but CaptionFontSize is blank.");
    return -1;
}

if (string.IsNullOrWhiteSpace(settings.FfmpegExe) || !File.Exists(settings.FfmpegExe))
{
    Console.WriteLine($"FfmpegExe must be a valid path to an ffmpeg executable - input was {settings.FfmpegExe}.");
    return -1;
}


var consoleProgress = new ConsoleProgress();

var directoryPhotos = PiSlicedDayPhotoTools.ProcessDirectory(settings.InputDirectory, consoleProgress);

var photos = directoryPhotos
    .Where(x => seriesList.Contains(x.Series, StringComparer.OrdinalIgnoreCase))
    .Where(x => timeDescriptionList.Contains(x.Description, StringComparer.OrdinalIgnoreCase))
    .Where(x => startTime is null || x.TakenOn >= startTime)
    .Where(x => endTime is null || x.TakenOn <= endTime)
    .OrderBy(x => x.TakenOn)
    .ToList();

if (!photos.Any())
{
    Console.WriteLine("No photos found for the given criteria.");
    return -1;
}

static Dictionary<int, string> TransformListToDictionary(List<string> list)
{
    var dictionary = new Dictionary<int, string>();
    for (var i = 0; i < list.Count; i++) dictionary[i] = list[i];

    return dictionary;
}

if (settings.TimelapseType.Equals(SeriesListGridImages.ConsoleSettingsIdentifier, StringComparison.OrdinalIgnoreCase))
{
    var result = await SeriesListGridImages.ImageGridTimelapse(photos, TransformListToDictionary(seriesList),
        TransformListToDictionary(timeDescriptionList), settings.Framerate, settings.FfmpegExe, consoleProgress,
        settings.CaptionWithDateTime, settings.CaptionFormatString!, settings.CaptionFontSize!.Value);

    if (string.IsNullOrWhiteSpace(result.resultFile) || !File.Exists(result.resultFile)) return -1;

    Console.WriteLine("Grid Timelapse Created:");
    Console.WriteLine($"{result.resultFile}");

    return 0;
}
else if (settings.TimelapseType.Equals(YearCompSingleTimeDescription.ConsoleSettingsIdentifier,
             StringComparison.OrdinalIgnoreCase))
{
    if (startTime == null || endTime == null)
    {
        Console.WriteLine("YearComp Timelapse requires a StartOn and EndOn date.");
        return -1;
    }

    var previousYearFiles = directoryPhotos
        .Where(x => seriesList.Contains(x.Series, StringComparer.OrdinalIgnoreCase))
        .Where(x => timeDescriptionList.Contains(x.Description, StringComparer.OrdinalIgnoreCase))
        .Where(x => x.TakenOn >= startTime.Value.AddDays(-367))
        .Where(x => x.TakenOn <= endTime.Value.AddDays(-362))
        .OrderBy(x => x.TakenOn)
        .ToList();

    var allPhotos = photos.Concat(previousYearFiles).OrderBy(x => x.TakenOn).ToList();

    var result = await YearCompSingleTimeDescription.YearCompSingleTimeDescriptionTimelapse(allPhotos, startTime.Value,
        endTime.Value, TransformListToDictionary(seriesList),
        settings.Framerate, settings.FfmpegExe, consoleProgress,
        settings.CaptionWithDateTime, settings.CaptionFormatString!, settings.CaptionFontSize!.Value);

    if (string.IsNullOrWhiteSpace(result.resultFile) || !File.Exists(result.resultFile)) return -1;

    Console.WriteLine("Year Comp Single Time Description Timelapse Created:");
    Console.WriteLine($"{result.resultFile}");

    return 0;
}
else if (settings.TimelapseType.Equals(SingleTimeDescription.ConsoleSettingsIdentifier,
             StringComparison.OrdinalIgnoreCase))
{
    var result = await SingleTimeDescription.SingleTimeDescriptionTimelapse(photos,
        TransformListToDictionary(seriesList),
        settings.Framerate, settings.FfmpegExe, consoleProgress,
        settings.CaptionWithDateTime, settings.CaptionFormatString!, settings.CaptionFontSize!.Value);

    if (string.IsNullOrWhiteSpace(result.resultFile) || !File.Exists(result.resultFile)) return -1;

    Console.WriteLine("Single Time Description Timelapse Created:");
    Console.WriteLine($"{result.resultFile}");

    return 0;
}
else
{
    Console.WriteLine($"TimelapseType {settings.TimelapseType} is not supported.");
    return -1;
}

return 0;