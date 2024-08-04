using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PiSlicedDayPhotos.TimelapseHelperGui;

public static class PowerShellRunnerGuiSettingTools
{
    public static PiSlicedDayPhotosTimelapseHelperGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwPowerShellRunnerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new PiSlicedDayPhotosTimelapseHelperGuiSettings()));

            return new PiSlicedDayPhotosTimelapseHelperGuiSettings();
        }

        return JsonSerializer.Deserialize<PiSlicedDayPhotosTimelapseHelperGuiSettings>(
                   FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new PiSlicedDayPhotosTimelapseHelperGuiSettings();
    }

    public static async Task WriteSettings(PiSlicedDayPhotosTimelapseHelperGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PiSlicedDayPhotosTimelapseHelper.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
    }
}