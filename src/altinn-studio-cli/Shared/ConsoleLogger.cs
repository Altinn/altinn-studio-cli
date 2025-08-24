using Spectre.Console;

namespace Altinn.Studio.Cli.Shared;

/// <summary>
/// Console implementation of ILogger using Spectre.Console for rich output
/// </summary>
public class ConsoleLogger : ILogger
{
    public void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error: {message.EscapeMarkup()}[/]");
    }

    public void LogWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]Warning: {message.EscapeMarkup()}[/]");
    }

    public void LogInfo(string message)
    {
        AnsiConsole.WriteLine(message);
    }

    public void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{message.EscapeMarkup()}[/]");
    }
}
