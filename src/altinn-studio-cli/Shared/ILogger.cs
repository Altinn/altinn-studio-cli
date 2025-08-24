namespace Altinn.Studio.Cli.Shared;

/// <summary>
/// Interface for logging operations
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an error message
    /// </summary>
    /// <param name="message">The error message to log</param>
    void LogError(string message);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">The warning message to log</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs an information message
    /// </summary>
    /// <param name="message">The information message to log</param>
    void LogInfo(string message);

    /// <summary>
    /// Logs a success message
    /// </summary>
    /// <param name="message">The success message to log</param>
    void LogSuccess(string message);
}
