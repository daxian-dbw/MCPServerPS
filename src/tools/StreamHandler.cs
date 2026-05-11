using System.Management.Automation;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MCPServerPS.Tools;

internal class StreamHandler
{
    private readonly ILogger _logger;

    internal StreamHandler(ILogger logger)
    {
        _logger = logger;
    }

    public void DebugDataAdding(object sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is DebugRecord record)
        {
            _logger.LogDebug("DEBUG: {DebugMessage}", record.Message);
        }
    }

    public void ErrorDataAdding(object sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is ErrorRecord record)
        {
            string errorMessage = FormatErrorRecord(record);
            _logger.LogError("ERROR: {ErrorMessage}", errorMessage);
        }
    }

    public void InformationDataAdding(object sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is InformationRecord record)
        {
            _logger.LogInformation("INFORMATION: {InformationMessage}", record.MessageData);
        }
    }

    public void ProgressDataAdding(object sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is ProgressRecord record)
        {
            _logger.LogInformation("PROGRESS: {ProgressMessage}", record.StatusDescription);
        }
    }

    public void VerboseDataAdding(object sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is VerboseRecord record)
        {
            _logger.LogInformation("VERBOSE: {VerboseMessage}", record.Message);
        }
    }

    public void WarningDataAdding(object sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is WarningRecord record)
        {
            _logger.LogWarning("WARNING: {WarningMessage}", record.Message);
        }
    }

    private static string FormatErrorRecord(ErrorRecord error)
    {
        string positionMessage = error.InvocationInfo?.PositionMessage;
        string scriptStackTrace = error.ScriptStackTrace;
        string errorMessage = PSToolUtils.FormatException(error.Exception);

        StringBuilder message = new(errorMessage);
        if (!string.IsNullOrEmpty(positionMessage))
        {
            message.Append('\n').Append(positionMessage);
        }

        if (!string.IsNullOrEmpty(scriptStackTrace))
        {
            message.Append('\n').Append(scriptStackTrace);
        }

        return message.ToString();
    }

    internal void RegisterStreamEvents(PowerShell pwsh)
    {
        pwsh.Streams.Debug.DataAdding += DebugDataAdding;
        pwsh.Streams.Error.DataAdding += ErrorDataAdding;
        pwsh.Streams.Information.DataAdding += InformationDataAdding;
        pwsh.Streams.Progress.DataAdding += ProgressDataAdding;
        pwsh.Streams.Verbose.DataAdding += VerboseDataAdding;
        pwsh.Streams.Warning.DataAdding += WarningDataAdding;
    }

    internal void UnregisterStreamEvents(PowerShell pwsh)
    {
        pwsh.Streams.Debug.DataAdding -= DebugDataAdding;
        pwsh.Streams.Error.DataAdding -= ErrorDataAdding;
        pwsh.Streams.Information.DataAdding -= InformationDataAdding;
        pwsh.Streams.Progress.DataAdding -= ProgressDataAdding;
        pwsh.Streams.Verbose.DataAdding -= VerboseDataAdding;
        pwsh.Streams.Warning.DataAdding -= WarningDataAdding;
    }
}
