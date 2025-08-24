using System.Reflection;
using Altinn.Studio.Cli.Shared;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli.Version;

/// <summary>
/// Version command settings
/// </summary>
public sealed class VersionSettings : CommandSettings { }

/// <summary>
/// Version command implementation
/// </summary>
public sealed class VersionCommand : Command<VersionSettings>
{
    private static readonly ConsoleLogger _logger = new();

    public override int Execute(CommandContext context, VersionSettings settings)
    {
        var version =
            Assembly
                .GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "Unknown";

        _logger.LogInfo($"altinn-studio cli v{version}");
        return CommandResult.Success;
    }
}
