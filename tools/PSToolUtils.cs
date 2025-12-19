using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;

namespace MCPServerPS.Tools;

internal class PSToolUtils
{
    internal static Dictionary<string, PSObject> ConvertArgs(PowerShell pwsh, IReadOnlyDictionary<string, JsonElement> argDict)
    {
        Dictionary<string, PSObject> realArgs = null;
        if (argDict is { })
        {
            realArgs = [];
            foreach (var kvp in argDict)
            {
                PSObject value = pwsh
                    .AddCommand("ConvertFrom-Json")
                    .AddParameter("InputObject", kvp.Value.GetRawText())
                    .AddParameter("Depth", 5)
                    .AddParameter("NoEnumerate", true)
                    .Execute().FirstOrDefault();

                realArgs.Add(kvp.Key, value);
            }
        }

        return realArgs;
    }

    internal static CallToolResult GetErrorResult(string toolName, Exception ex)
    {
        string message = FormatException(ex);

        string error = $"""
            Failed to run the tool '{toolName}' due to the following error:
            ```
            {message}
            ```
            Check to see if it's caused by the passed-in command name or parameter name(s), and if so, please try again.
            """;

        return new CallToolResult()
        {
            Content = [new TextContentBlock { Text = error }],
            IsError = true
        };
    }

    internal static string FormatException(Exception ex)
    {
        StringBuilder sb = null;
        string message = ex.Message;

        while (ex.InnerException is { })
        {
            sb ??= new(message, capacity: message.Length * 3);
            if (sb[^1] is not '\n')
            {
                sb.Append('\n');
            }

            sb.Append($"   Inner -> {ex.InnerException.Message}");
            ex = ex.InnerException;
        }

        if (sb is not null)
        {
            message = sb.ToString();
        }

        return message;
    }

    internal static Tool CreateToolForScriptOrFunction(PowerShell pwsh, CommandInfo commandInfo)
    {
        var scriptInfo = commandInfo as ExternalScriptInfo;
        var funcInfo = scriptInfo is null ? commandInfo as FunctionInfo : null;

        if (scriptInfo is null && funcInfo is null)
        {
            throw new NotSupportedException("We only support .ps1 scripts or module functions as MCP server tools.");
        }

        string kind = scriptInfo is { } ? "script" : "function";
        string name = commandInfo.Name;

        if (commandInfo.ParameterSets.Count > 1)
        {
            throw new InvalidDataException($"The {kind} '{name}' cannot have more than 1 parameter set.");
        }

        dynamic help = pwsh
            .AddCommand("Get-Help")
            .AddParameter("Name", scriptInfo?.Source ?? funcInfo.Name)
            .AddParameter("Full")
            .Execute<PSObject>() ?? throw new InvalidDataException($"The {kind} '{name}' has no comment based help defined.");

        string toolName = Path.GetFileNameWithoutExtension(name).Replace('-', '_');
        string toolDescription = ValueOf<string>(help.description?[0]?.Text) ?? throw new InvalidDataException($"No description found for the {kind} '{name}'.");
        PSObject[] parameters = ValueOf<PSObject[]>(help.parameters?.parameter) ?? Array.Empty<PSObject>();

        JsonObject schema = new()
        {
            { "type", "object" },
            { "additionalProperties", false },
            { "$schema", "http://json-schema.org/draft-07/schema#" }
        };

        JsonObject parameterSchemas = [];
        JsonArray requiredProperties = null;

        foreach (dynamic parameter in parameters)
        {
            ParameterMetadata paramInfo = commandInfo.Parameters[ValueOf<string>(parameter.name)];
            string paramDescription = ValueOf<string>(parameter.description?[0]?.Text)
                ?? throw new InvalidDataException($"No description found for the parameter '{paramInfo.Name}'.");

            var paramAttr = paramInfo.Attributes.OfType<ParameterAttribute>().FirstOrDefault();
            var paramType = paramInfo.ParameterType == typeof(SwitchParameter)
                ? typeof(bool)
                : paramInfo.ParameterType;

            object defaultValue = paramAttr?.Mandatory == true
                ? null
                : GetDefaultValue(
                    (scriptInfo?.ScriptBlock ?? funcInfo.ScriptBlock).Ast,
                    paramInfo.Name,
                    paramType);

            var paramSchema = AIJsonUtilities.CreateJsonSchema(
                type: paramType,
                description: paramDescription,
                hasDefaultValue: defaultValue is { },
                defaultValue: defaultValue);

            parameterSchemas.Add(paramInfo.Name, JsonSerializer.SerializeToNode(paramSchema));
            if (paramAttr?.Mandatory == true)
            {
                (requiredProperties ??= []).Add((JsonNode)paramInfo.Name);
            }
        }

        schema.Add("properties", parameterSchemas);
        if (requiredProperties is { })
        {
            schema.Add("required", requiredProperties);
        }

        return new Tool()
        {
            Name = toolName,
            Description = toolDescription,
            InputSchema = JsonSerializer.SerializeToElement(schema)
        };
    }

    private static T ValueOf<T>(dynamic value)
    {
        if (value is T ret)
        {
            return ret;
        }

        return LanguagePrimitives.ConvertTo<T>(value);
    }

    private static object GetDefaultValue(Ast ast, string paramName, Type paramType)
    {
        var parameters = ast switch
        {
            FunctionDefinitionAst fdAst => fdAst.Parameters ?? fdAst.Body.ParamBlock?.Parameters,
            ScriptBlockAst sbAst => sbAst.ParamBlock?.Parameters,
            _ => throw new UnreachableException("Code not reachable.")
        };

        var paramAst = parameters?.FirstOrDefault(pAst => pAst.Name.VariablePath.UserPath == paramName);
        if (paramAst is { } && paramAst.DefaultValue is ConstantExpressionAst constantAst)
        {
            return constantAst.Value;
        }

        if (paramType == typeof(string))
        {
            return string.Empty;
        }

        if (paramType == typeof(int) || paramType == typeof(long) || paramType == typeof(short))
        {
            return 0;
        }

        if (paramType == typeof(bool))
        {
            return false;
        }

        return typeof(PSToolUtils)
            .GetMethod(nameof(GetDefaultValueGeneric), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(paramType)
            .Invoke(null, null);
    }

    private static T GetDefaultValueGeneric<T>() => default;
}
