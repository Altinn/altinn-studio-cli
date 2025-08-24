namespace Altinn.Studio.Cli.Shared;

/// <summary>
/// Standardized command result with exit codes
/// </summary>
public static class CommandResult
{
    /// <summary>
    /// Success exit code (0)
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// General error exit code (1)
    /// </summary>
    public const int GeneralError = 1;

    /// <summary>
    /// Version/compatibility error exit code (2)
    /// </summary>
    public const int VersionError = 2;

    /// <summary>
    /// Unexpected/unhandled error exit code (3)
    /// </summary>
    public const int UnexpectedError = 3;
}
