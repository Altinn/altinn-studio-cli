using System.ComponentModel;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.BackendUpgrade;

/// <summary>
/// Settings for the backend upgrade command
/// </summary>
public sealed class BackendUpgradeSettings : UpgradeSettings
{
    [Description("The project file to read relative to --folder")]
    [CommandOption("--project")]
    public string ProjectFile { get; init; } = "App/App.csproj";

    [Description("The process file to read relative to --folder")]
    [CommandOption("--process")]
    public string ProcessFile { get; init; } = "App/config/process/process.bpmn";

    [Description("The folder where the appsettings.*.json files are located")]
    [CommandOption("--appsettings-folder")]
    public string AppSettingsFolder { get; init; } = "App";

    [Description("The target version to upgrade to")]
    [CommandOption("--target-version")]
    public string TargetVersion { get; init; } = "8.7.0";

    [Description("The target dotnet framework version to upgrade to")]
    [CommandOption("--target-framework")]
    public string TargetFramework { get; init; } = "net8.0";

    [Description("Skip code upgrade")]
    [CommandOption("--skip-code-upgrade")]
    public bool SkipCodeUpgrade { get; init; }

    [Description("Skip csproj upgrade")]
    [CommandOption("--skip-csproj-upgrade")]
    public bool SkipCsprojUpgrade { get; init; }

    [Description("Skip dockerfile upgrade")]
    [CommandOption("--skip-dockerfile-upgrade")]
    public bool SkipDockerUpgrade { get; init; }

    [Description("Skip process upgrade")]
    [CommandOption("--skip-process-upgrade")]
    public bool SkipProcessUpgrade { get; init; }

    [Description("Skip appsettings upgrade")]
    [CommandOption("--skip-appsettings-upgrade")]
    public bool SkipAppSettingsUpgrade { get; init; }
}
