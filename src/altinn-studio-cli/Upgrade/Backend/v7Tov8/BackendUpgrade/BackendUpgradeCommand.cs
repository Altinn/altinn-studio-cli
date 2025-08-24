using System.ComponentModel;
using Altinn.Studio.Cli.Shared;
using Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.CodeRewriters;
using Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.DockerfileRewriters;
using Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.ProcessRewriter;
using Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.ProjectRewriters;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.BackendUpgrade;

/// <summary>
/// Backend upgrade command for upgrading app-lib-dotnet in an Altinn 3 application
/// </summary>
[Description("Upgrade an app from app-lib-dotnet v7 to v8")]
public sealed class BackendUpgradeCommand : AsyncCommand<BackendUpgradeSettings>
{
    private static readonly ConsoleLogger _logger = new();

    public override async Task<int> ExecuteAsync(CommandContext context, BackendUpgradeSettings settings)
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

        var attr = File.GetAttributes(projectFolder);
        if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
        {
            _logger.LogError(
                $"Project folder {projectFolder} is a file. Please supply location of project with --folder [path/to/project]"
            );
            return CommandResult.GeneralError;
        }

        if (!Path.IsPathRooted(projectFolder))
        {
            projectFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectFolder));
        }

        var projectFile = Path.Combine(projectFolder, settings.ProjectFile);
        var processFile = Path.Combine(projectFolder, settings.ProcessFile);
        var appSettingsFolder = Path.Combine(projectFolder, settings.AppSettingsFolder);

        if (!File.Exists(projectFile))
        {
            _logger.LogError(
                $"Project file {projectFile} does not exist. Please supply location of project with --project [path/to/project.csproj]"
            );
            return CommandResult.GeneralError;
        }

        var projectChecks = new ProjectChecks.ProjectChecks(projectFile);
        if (!projectChecks.SupportedSourceVersion())
        {
            _logger.LogError(
                $"Version(s) in project file {projectFile} is not supported. Please upgrade to version 7.0.0 or higher."
            );
            return CommandResult.VersionError;
        }

        try
        {
            if (!settings.SkipCodeUpgrade)
            {
                returnCode = await UpgradeCode(projectFile);
            }

            if (!settings.SkipCsprojUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await UpgradeProjectFile(projectFile, settings.TargetVersion, settings.TargetFramework);
            }

            if (!settings.SkipDockerUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await UpgradeDockerfile(
                    Path.Combine(projectFolder, "Dockerfile"),
                    settings.TargetFramework
                );
            }

            if (!settings.SkipProcessUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await UpgradeProcess(processFile);
            }

            if (!settings.SkipAppSettingsUpgrade && returnCode == CommandResult.Success)
            {
                returnCode = await UpgradeAppSettings(appSettingsFolder);
            }

            if (returnCode == CommandResult.Success)
            {
                _logger.LogSuccess(
                    "Upgrade completed without errors. Please verify that the application is still working as expected."
                );
            }
            else
            {
                _logger.LogInfo("Upgrade completed with errors. Please check for errors in the log above.");
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error: {e}");
            return CommandResult.UnexpectedError;
        }

        return returnCode;
    }

    static async Task<int> UpgradeProjectFile(string projectFile, string targetVersion, string targetFramework)
    {
        _logger.LogInfo("Trying to upgrade nuget versions in project file");
        var rewriter = new ProjectFileRewriter(projectFile, targetVersion, targetFramework);
        await rewriter.Upgrade();
        _logger.LogInfo("Nuget versions upgraded");
        return CommandResult.Success;
    }

    static async Task<int> UpgradeDockerfile(string dockerFile, string targetFramework)
    {
        if (!File.Exists(dockerFile))
        {
            _logger.LogError(
                $"Dockerfile {dockerFile} does not exist. Please supply location of project with --dockerfile [path/to/Dockerfile]"
            );
            return CommandResult.GeneralError;
        }
        _logger.LogInfo("Trying to upgrade dockerfile");
        var rewriter = new DockerfileRewriter(dockerFile, targetFramework);
        await rewriter.Upgrade();
        _logger.LogInfo("Dockerfile upgraded");
        return CommandResult.Success;
    }

    static async Task<int> UpgradeCode(string projectFile)
    {
        _logger.LogInfo("Trying to upgrade references and using in code");

        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
        var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectFile);
        var comp = await project.GetCompilationAsync();
        if (comp is null)
        {
            _logger.LogError("Could not get compilation");
            return CommandResult.GeneralError;
        }
        foreach (var sourceTree in comp.SyntaxTrees)
        {
            var semanticModel = comp.GetSemanticModel(sourceTree);
            var root = await sourceTree.GetRootAsync();

            var typesRewriter = new TypesRewriter(semanticModel);
            var newSource = typesRewriter.Visit(root);

            var usingRewriter = new UsingRewriter();
            var newUsingSource = usingRewriter.Visit(newSource);

            var dataProcessorRewriter = new DataProcessorRewriter();
            var dataProcessorSource = dataProcessorRewriter.Visit(newUsingSource);

            var finalSource = dataProcessorSource;

            if (
                sourceTree.FilePath != null
                && (
                    sourceTree.FilePath.Contains("/models/", StringComparison.InvariantCultureIgnoreCase)
                    || sourceTree.FilePath.Contains("\\models\\", StringComparison.InvariantCultureIgnoreCase)
                )
            )
            {
                // Find all classes that are used in a List
                var classNamesInList = dataProcessorSource
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(p => p is { Type: GenericNameSyntax { Identifier.ValueText: "List" } })
                    .Select(p =>
                        ((GenericNameSyntax)p.Type)
                            .TypeArgumentList.Arguments.OfType<IdentifierNameSyntax>()
                            .FirstOrDefault()
                            ?.Identifier.ValueText
                    )
                    .OfType<string>()
                    .ToList();

                var modelRewriter = new ModelRewriter(classNamesInList);
                finalSource = modelRewriter.Visit(dataProcessorSource);
            }

            if (root != finalSource)
            {
                await File.WriteAllTextAsync(sourceTree.FilePath!, finalSource.ToFullString());
            }
        }

        _logger.LogInfo("References and using upgraded");
        return CommandResult.Success;
    }

    static async Task<int> UpgradeProcess(string processFile)
    {
        if (!File.Exists(processFile))
        {
            _logger.LogError(
                $"Process file {processFile} does not exist. Please supply location of project with --process [path/to/project.csproj]"
            );
            return CommandResult.GeneralError;
        }

        _logger.LogInfo("Trying to upgrade process file");
        ProcessUpgrader parser = new(processFile);
        parser.Upgrade();
        await parser.Write();
        var warnings = parser.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogInfo(warning);
        }

        _logger.LogInfo(
            warnings.Any()
                ? "Process file upgraded with warnings. Review the warnings above and make sure that the process file is still valid."
                : "Process file upgraded"
        );

        return CommandResult.Success;
    }

    static async Task<int> UpgradeAppSettings(string appSettingsFolder)
    {
        if (!Directory.Exists(appSettingsFolder))
        {
            _logger.LogError(
                $"App settings folder {appSettingsFolder} does not exist. Please supply location with --appsettings-folder [path/to/appsettings]"
            );
            return CommandResult.GeneralError;
        }

        if (Directory.GetFiles(appSettingsFolder, "appsettings*.json", SearchOption.TopDirectoryOnly).Length == 0)
        {
            _logger.LogError($"No appsettings*.json files found in {appSettingsFolder}");
            return CommandResult.GeneralError;
        }

        _logger.LogInfo("Trying to upgrade appsettings*.json files");
        AppSettingsRewriter.AppSettingsRewriter rewriter = new(appSettingsFolder);
        rewriter.Upgrade();
        await rewriter.Write();
        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            _logger.LogInfo(warning);
        }

        _logger.LogInfo(
            warnings.Any()
                ? "AppSettings files upgraded with warnings. Review the warnings above and make sure that the appsettings files are still valid."
                : "AppSettings files upgraded"
        );

        return CommandResult.Success;
    }
}
