using System.ComponentModel;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli.Shared;

/// <summary>
/// Base settings class with common options
/// </summary>
public abstract class BaseSettings : CommandSettings
{
    [Description("The project folder to read")]
    [CommandOption("-f|--folder")]
    public string ProjectFolder { get; init; } = "CurrentDirectory";
}
