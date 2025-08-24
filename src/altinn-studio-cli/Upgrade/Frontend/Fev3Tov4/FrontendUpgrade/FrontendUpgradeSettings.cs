using System.ComponentModel;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.FrontendUpgrade;

/// <summary>
/// Settings for the frontend upgrade command
/// </summary>
public sealed class FrontendUpgradeSettings : UpgradeSettings
{
    [Description("The target version to upgrade to")]
    [CommandOption("--target-version")]
    public string TargetVersion { get; init; } = "4";

    [Description("The name of the Index.cshtml file relative to --folder")]
    [CommandOption("--index-file")]
    public string IndexFile { get; init; } = "App/views/Home/Index.cshtml";

    [Description("Skip Index.cshtml upgrade")]
    [CommandOption("--skip-index-file-upgrade")]
    public bool SkipIndexFileUpgrade { get; init; }

    [Description("The folder containing layout files relative to --folder")]
    [CommandOption("--ui-folder")]
    public string UiFolder { get; init; } = "App/ui/";

    [Description("The folder containing text files relative to --folder")]
    [CommandOption("--texts-folder")]
    public string TextsFolder { get; init; } = "App/config/texts/";

    [Description("The name of the layout set to be created")]
    [CommandOption("--layout-set-name")]
    public string LayoutSetName { get; init; } = "form";

    [Description("The path of the applicationmetadata.json file relative to --folder")]
    [CommandOption("--application-metadata")]
    public string ApplicationMetadataFile { get; init; } = "App/config/applicationmetadata.json";

    [Description("Skip layout set upgrade")]
    [CommandOption("--skip-layout-set-upgrade")]
    public bool SkipLayoutSetUpgrade { get; init; }

    [Description("Skip layout settings upgrade")]
    [CommandOption("--skip-settings-upgrade")]
    public bool SkipSettingsUpgrade { get; init; }

    [Description("Skip layout files upgrade")]
    [CommandOption("--skip-layout-upgrade")]
    public bool SkipLayoutUpgrade { get; init; }

    [Description("Convert 'title' in repeating groups to 'summaryTitle'")]
    [CommandOption("--convert-group-titles")]
    public bool ConvertGroupTitles { get; init; }

    [Description("Skip schema reference upgrade")]
    [CommandOption("--skip-schema-ref-upgrade")]
    public bool SkipSchemaRefUpgrade { get; init; }

    [Description("The name of the layout set to be created for the custom receipt")]
    [CommandOption("--receipt-layout-set-name")]
    public string? ReceiptLayoutSetName { get; init; } = "receipt";

    [Description("Skip custom receipt upgrade")]
    [CommandOption("--skip-custom-receipt-upgrade")]
    public bool SkipCustomReceiptUpgrade { get; init; }

    [Description("Skip footer upgrade")]
    [CommandOption("--skip-footer-upgrade")]
    public bool SkipFooterUpgrade { get; init; }

    [Description("Skip checks")]
    [CommandOption("--skip-checks")]
    public bool SkipChecks { get; init; }
}
