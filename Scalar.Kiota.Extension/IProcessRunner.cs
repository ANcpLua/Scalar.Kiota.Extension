namespace Scalar.Kiota.Extension;

/// <summary>
/// Abstraction for running external processes, enabling testability.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process and waits for completion.
    /// </summary>
    Task RunAsync(string fileName, string arguments, string? workingDirectory = null);
}
