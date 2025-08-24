using Altinn.Studio.Cli.Upgrade.Backend.v7Tov8.BackendUpgrade;
using Altinn.Studio.Cli.Upgrade.Frontend.Fev3Tov4.FrontendUpgrade;
using Altinn.Studio.Cli.Version;
using Spectre.Console.Cli;

namespace Altinn.Studio.Cli;

internal sealed class Program
{
    static async Task<int> Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("altinn-studio");

            config
                .AddCommand<VersionCommand>("version")
                .WithDescription("Print version of altinn-studio cli")
                .WithExample("version");

            config.AddBranch(
                "upgrade",
                upgrade =>
                {
                    upgrade.SetDescription("Upgrade an app");

                    upgrade
                        .AddCommand<BackendUpgradeCommand>("backend")
                        .WithDescription("Upgrade an app from app-lib-dotnet v7 to v8")
                        .WithExample("upgrade", "backend", "--folder", "/path/to/project")
                        .WithExample("upgrade", "backend", "--target-version", "8.7.0");

                    upgrade
                        .AddCommand<FrontendUpgradeCommand>("frontend")
                        .WithDescription("Upgrade an app from using App-Frontend v3 to v4")
                        .WithExample("upgrade", "frontend", "--folder", "/path/to/project")
                        .WithExample("upgrade", "frontend", "--target-version", "4");
                }
            );
        });

        return await app.RunAsync(args);
    }
}
