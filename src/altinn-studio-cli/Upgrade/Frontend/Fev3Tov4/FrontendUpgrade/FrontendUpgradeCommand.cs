using System.ComponentModel;
using Altinn.Studio.Cli.Shared;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.Checks;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.CustomReceiptRewriter;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.FooterRewriter;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.IndexFileRewriter;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.LayoutRewriter;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.LayoutSetRewriter;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.SchemaRefRewriter;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.SettingsWriter;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.FrontendUpgrade;

/// <summary>
/// Frontend upgrade command for upgrading from v3 to v4
/// </summary>
[Description("Upgrade an app from using App-Frontend v3 to v4")]
public sealed class FrontendUpgradeCommand : AsyncCommand<FrontendUpgradeSettings>
{
    private static readonly ConsoleLogger _logger = new();

    public override async Task<int> ExecuteAsync(CommandContext context, FrontendUpgradeSettings settings)
    {
        var returnCode = CommandResult.Success;
        var projectFolder = settings.ProjectFolder;

        if (projectFolder == "CurrentDirectory")
            projectFolder = Directory.GetCurrentDirectory();

        if (!Directory.Exists(projectFolder))
        {
            _logger.LogError(
                $"{projectFolder} does not exist. Please supply location of project with --folder [path/to/project]"
            );
            return CommandResult.GeneralError;
        }

        if (!Path.IsPathRooted(projectFolder))
        {
            projectFolder = Path.Combine(Directory.GetCurrentDirectory(), projectFolder);
        }

        var applicationMetadataFile = Path.Combine(projectFolder, settings.ApplicationMetadataFile);
        var uiFolder = Path.Combine(projectFolder, settings.UiFolder);
        var textsFolder = Path.Combine(projectFolder, settings.TextsFolder);
        var indexFile = Path.Combine(projectFolder, settings.IndexFile);

        try
        {
            if (!settings.SkipIndexFileUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await IndexFileUpgrade(indexFile, settings.TargetVersion);
            }

            if (!settings.SkipLayoutSetUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await LayoutSetUpgrade(uiFolder, settings.LayoutSetName, applicationMetadataFile);
            }

            if (
                !settings.SkipCustomReceiptUpgrade
                && returnCode == CommandResult.Success
                && settings.ReceiptLayoutSetName != null
            )
            {
                returnCode = await CustomReceiptUpgrade(uiFolder, settings.ReceiptLayoutSetName);
            }

            if (!settings.SkipSettingsUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await CreateMissingSettings(uiFolder);
            }

            if (!settings.SkipLayoutUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await LayoutUpgrade(uiFolder, settings.ConvertGroupTitles);
            }

            if (!settings.SkipFooterUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await FooterUpgrade(uiFolder);
            }

            if (!settings.SkipSchemaRefUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await SchemaRefUpgrade(
                    settings.TargetVersion,
                    uiFolder,
                    applicationMetadataFile,
                    textsFolder
                );
            }

            if (!settings.SkipChecks && returnCode == CommandResult.Success)
            {
                returnCode = await RunChecks(textsFolder);
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error: {e}");
            return CommandResult.UnexpectedError;
        }

        return returnCode;
    }

    private static async Task<int> IndexFileUpgrade(string indexFile, string targetVersion)
    {
        if (!File.Exists(indexFile))
        {
            _logger.LogError(
                $"Index.cshtml file {indexFile} does not exist. Please supply location of project with --index-file [path/to/Index.cshtml]"
            );
            return CommandResult.GeneralError;
        }

        var rewriter = new IndexFileUpgrader(indexFile, targetVersion);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }

        _logger.LogInfo(
            warnings.Any() ? "Index.cshtml upgraded with warnings. Review the warnings above." : "Index.cshtml upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> LayoutSetUpgrade(
        string uiFolder,
        string layoutSetName,
        string applicationMetadataFile
    )
    {
        if (File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            _logger.LogInfo("Project already using layout sets. Skipping layout set upgrade.");
            return CommandResult.Success;
        }

        if (!Directory.Exists(uiFolder))
        {
            _logger.LogError(
                $"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]"
            );
            return CommandResult.GeneralError;
        }

        if (!File.Exists(applicationMetadataFile))
        {
            _logger.LogError(
                $"Application metadata file {applicationMetadataFile} does not exist. Please supply location of project with --application-metadata [path/to/applicationmetadata.json]"
            );
            return CommandResult.GeneralError;
        }

        var rewriter = new LayoutSetUpgrader(uiFolder, layoutSetName, applicationMetadataFile);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }
        _logger.LogInfo(
            warnings.Any() ? "Layout-sets upgraded with warnings. Review the warnings above." : "Layout sets upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> CustomReceiptUpgrade(string uiFolder, string receiptLayoutSetName)
    {
        if (!Directory.Exists(uiFolder))
        {
            _logger.LogError(
                $"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]"
            );
            return CommandResult.GeneralError;
        }

        if (!File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            _logger.LogError("Converting to layout sets is required before upgrading custom receipt.");
            return CommandResult.GeneralError;
        }

        if (Directory.Exists(Path.Combine(uiFolder, receiptLayoutSetName)))
        {
            _logger.LogInfo(
                $"A layout set with the name {receiptLayoutSetName} already exists. Skipping custom receipt upgrade."
            );
            return CommandResult.Success;
        }

        var rewriter = new CustomReceiptUpgrader(uiFolder, receiptLayoutSetName);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }
        _logger.LogInfo(
            warnings.Any()
                ? "Custom receipt upgraded with warnings. Review the warnings above."
                : "Custom receipt upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> CreateMissingSettings(string uiFolder)
    {
        if (!Directory.Exists(uiFolder))
        {
            _logger.LogError(
                $"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]"
            );
            return CommandResult.GeneralError;
        }

        if (!File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            _logger.LogError("Converting to layout sets is required before upgrading settings.");
            return CommandResult.GeneralError;
        }

        var rewriter = new SettingsCreator(uiFolder);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }
        _logger.LogInfo(
            warnings.Any()
                ? "Layout settings upgraded with warnings. Review the warnings above."
                : "Layout settings upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> LayoutUpgrade(string uiFolder, bool convertGroupTitles)
    {
        if (!Directory.Exists(uiFolder))
        {
            _logger.LogError(
                $"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]"
            );
            return CommandResult.GeneralError;
        }

        if (!File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            _logger.LogError("Converting to layout sets is required before upgrading layouts.");
            return CommandResult.GeneralError;
        }

        var rewriter = new LayoutUpgrader(uiFolder, convertGroupTitles);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }

        _logger.LogInfo(
            warnings.Any() ? "Layout files upgraded with warnings. Review the warnings above." : "Layout files upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> FooterUpgrade(string uiFolder)
    {
        if (!Directory.Exists(uiFolder))
        {
            _logger.LogError(
                $"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]"
            );
            return CommandResult.GeneralError;
        }

        if (!File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            _logger.LogError("Converting to layout sets is required before upgrading footer.");
            return CommandResult.GeneralError;
        }

        var rewriter = new FooterUpgrader(uiFolder);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }

        _logger.LogInfo(
            warnings.Any() ? "Footer upgraded with warnings. Review the warnings above." : "Footer upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> SchemaRefUpgrade(
        string targetVersion,
        string uiFolder,
        string applicationMetadataFile,
        string textsFolder
    )
    {
        if (!Directory.Exists(uiFolder))
        {
            _logger.LogError(
                $"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]"
            );
            return CommandResult.GeneralError;
        }

        if (!File.Exists(applicationMetadataFile))
        {
            _logger.LogError(
                $"Application metadata file {applicationMetadataFile} does not exist. Please supply location of project with --application-metadata [path/to/applicationmetadata.json]"
            );
            return CommandResult.GeneralError;
        }

        if (!Directory.Exists(textsFolder))
        {
            _logger.LogError(
                $"Texts folder {textsFolder} does not exist. Please supply location of project with --texts-folder [path/to/texts/]"
            );
            return CommandResult.GeneralError;
        }

        var rewriter = new SchemaRefUpgrader(targetVersion, uiFolder, applicationMetadataFile, textsFolder);
        rewriter.Upgrade();
        await rewriter.Write();

        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }

        _logger.LogInfo(
            warnings.Any()
                ? "Schema references upgraded with warnings. Review the warnings above."
                : "Schema references upgraded"
        );
        return CommandResult.Success;
    }

    private static async Task<int> RunChecks(string textsFolder)
    {
        if (!Directory.Exists(textsFolder))
        {
            _logger.LogError(
                $"Texts folder {textsFolder} does not exist. Please supply location of project with --texts-folder [path/to/texts/]"
            );
            return CommandResult.GeneralError;
        }

        _logger.LogInfo("Running checks...");
        var checker = new Checker(textsFolder);
        checker.CheckTextDataModelReferences();

        var warnings = checker.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogWarning(warning);
        }

        _logger.LogInfo(
            warnings.Any() ? "Checks completed with warnings. Review the warnings above." : "All checks passed"
        );
        return CommandResult.Success;
    }
}
