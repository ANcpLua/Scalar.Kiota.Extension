namespace Scalar.Kiota.Extension.Tests.Mocks;

/// <summary>
/// Mock process runner that records calls instead of executing real processes.
/// </summary>
internal sealed class MockProcessRunner : IProcessRunner
{
    public List<(string FileName, string Arguments, string? WorkingDirectory)> RunCalls { get; } = [];
    public List<string> OpenUrlCalls { get; } = [];
    public bool ShouldThrowOnRun { get; set; }
    public string? ThrowOnFileName { get; set; }

    public Task RunAsync(string fileName, string arguments, string? workingDirectory = null)
    {
        RunCalls.Add((fileName, arguments, workingDirectory));

        if (ShouldThrowOnRun || (ThrowOnFileName != null && fileName == ThrowOnFileName))
            throw new InvalidOperationException($"Mock failure for {fileName}");

        return Task.CompletedTask;
    }

    public void OpenUrl(string url)
    {
        OpenUrlCalls.Add(url);
    }
}
