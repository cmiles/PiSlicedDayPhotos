using System.Reflection;
using Serilog;
using Serilog.Formatting.Compact;

namespace PiSlicedDayPhotos.Utility;

public static class LogTools
{
    public static DateTime? GetBuildDate(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime;
    }

    public static DateTime? GetEntryAssemblyBuildDate()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return null;
            var attribute = entryAssembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public static LoggerConfiguration LogToConsole(this LoggerConfiguration toConfigure)
    {
        return toConfigure.MinimumLevel.Verbose().WriteTo.Console();
    }

    public static LoggerConfiguration LogToFileInProgramDirectory(this LoggerConfiguration toConfigure,
        string fileNameFragment)
    {
        var programDirectory = AppContext.BaseDirectory;

        return toConfigure.MinimumLevel.Verbose().WriteTo.File(new CompactJsonFormatter(),
            Path.Combine(programDirectory, $"1_Log-{fileNameFragment}.json"), rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
    }

    public static void StandardStaticLoggerForProgramDirectory(string fileNameFragment)
    {
        Log.Logger = new LoggerConfiguration().LogToConsole()
            .LogToFileInProgramDirectory(fileNameFragment).CreateLogger();

        try
        {
            Log.Information(
                $"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");
            Log.Information($"{GetEntryAssemblyBuildDate()}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}