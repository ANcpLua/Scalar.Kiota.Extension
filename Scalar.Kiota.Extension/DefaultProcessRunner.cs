using System.Diagnostics;

namespace Scalar.Kiota.Extension;

/// <summary>
/// Default implementation that runs actual processes.
/// </summary>
internal sealed class DefaultProcessRunner : IProcessRunner
{
    public async Task RunAsync(string fileName, string arguments, string? workingDirectory = null)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };
        process.Start();
        await process.WaitForExitAsync();
        if (process.ExitCode is not 0)
            throw new InvalidOperationException(
                $"{fileName} {arguments} failed: {await process.StandardError.ReadToEndAsync()}");
    }
}
