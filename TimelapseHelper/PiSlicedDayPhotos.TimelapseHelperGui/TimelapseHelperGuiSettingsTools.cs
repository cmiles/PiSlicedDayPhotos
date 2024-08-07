using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PiSlicedDayPhotos.TimelapseHelperGui;

public static class TimelapseHelperGuiSettingsTools
{
    public static TimelapseHelperGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwPowerShellRunnerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new TimelapseHelperGuiSettings()));

            return new TimelapseHelperGuiSettings();
        }

        return JsonSerializer.Deserialize<TimelapseHelperGuiSettings>(
                   FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new TimelapseHelperGuiSettings();
    }

    public static async Task WriteSettings(TimelapseHelperGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PiSlicedDayPhotosTimelapseHelper.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
    }
}