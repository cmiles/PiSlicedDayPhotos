using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PiSlicedDayPhotos.TimelapseHelperGui.Controls;

public static class PowerShellRun
{
    public static async Task<(bool errors, List<string> runLog)> ExecuteScript(string scriptToRun,
        IProgress<string>? progress)
    {
        var initialSessionState = InitialSessionState.CreateDefault();
        // 2024-7-23: Leaving this code commented to consider - this would make this program's
        // PowerShell execution a lot more like what the user expects at a command prompt which
        // is good, but it also makes it a lot less portable. I wonder if the Vanilla runspace
        // is the better compromise?
        //initialSessionState.ImportPSModule(["*"]); // Attempt to preload all available modules
        //initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;

        // create Powershell runspace
        var runSpace = RunspaceFactory.CreateRunspace(initialSessionState);

        // open it
        runSpace.Open();

        // create a pipeline and feed it the script text
        var pipeline = runSpace.CreatePipeline();
        pipeline.Commands.AddScript(scriptToRun);
        pipeline.Input.Close();


        var returnLog = new ConcurrentBag<(DateTime, string)>();
        var errorData = false;

        pipeline.Output.DataReady += (_, _) =>
        {
            Collection<PSObject> psObjects = pipeline.Output.NonBlockingRead();
            foreach (var psObject in psObjects)
            {
                returnLog.Add((DateTime.UtcNow, $"{DateTime.Now:G}>> {psObject.ToString()}"));
                progress?.Report(psObject.ToString());
            }
        };

        pipeline.StateChanged += (_, eventArgs) =>
        {
            returnLog.Add((DateTime.UtcNow,
                $"{DateTime.Now:G}>> State: {eventArgs.PipelineStateInfo.State} {eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty}"));

            progress?.Report(
                $"Pipeline State: {eventArgs.PipelineStateInfo.State} {eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty}");
        };

        pipeline.Error.DataReady += (_, _) =>
        {
            Collection<object> errorObjects = pipeline.Error.NonBlockingRead();
            if (errorObjects.Count == 0) return;

            errorData = true;
            foreach (var errorObject in errorObjects)
            {
                var errorString = errorObject.ToString();
                returnLog.Add((DateTime.UtcNow, $"{DateTime.Now:G}>> Error: {errorString}"));
                if (!string.IsNullOrWhiteSpace(errorString))
                    progress?.Report(errorString);
            }
        };
        pipeline.InvokeAsync();

        await Task.Delay(200);

        while (pipeline.PipelineStateInfo.State == PipelineState.Running) await Task.Delay(250);

        return (errorData || pipeline.HadErrors, returnLog.OrderBy(x => x.Item1).Select(x => x.Item2).ToList());

        //TODO: Error return handling
    }
}