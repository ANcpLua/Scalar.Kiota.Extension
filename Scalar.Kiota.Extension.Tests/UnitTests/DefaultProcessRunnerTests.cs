namespace Scalar.Kiota.Extension.Tests.UnitTests;

public class DefaultProcessRunnerTests
{
    [Test]
    [DisplayName("RunAsync_CompletesSuccessfully_WhenProcessExitsWithZero")]
    public async Task RunAsync_CompletesSuccessfully_WhenProcessExitsWithZero()
    {
        var sut = new DefaultProcessRunner();
        await Assert.That(async () => await sut.RunAsync("echo", "test")).ThrowsNothing();
    }

    [Test]
    [DisplayName("RunAsync_ThrowsInvalidOperationException_WhenProcessExitsWithNonZero")]
    public async Task RunAsync_ThrowsInvalidOperationException_WhenProcessExitsWithNonZero()
    {
        var sut = new DefaultProcessRunner();
        var exception = await Assert.That(async () => await sut.RunAsync("sh", "-c \"exit 1\""))
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("sh -c \"exit 1\" failed:");
    }

    [Test]
    [DisplayName("RunAsync_ThrowsInvalidOperationException_WithStandardError")]
    public async Task RunAsync_ThrowsInvalidOperationException_WithStandardError()
    {
        var sut = new DefaultProcessRunner();
        var exception = await Assert.That(async () =>
                await sut.RunAsync("sh", "-c \"echo 'Error message' >&2; exit 1\""))
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("Error message");
    }

    [Test]
    [DisplayName("RunAsync_ExecutesInSpecifiedWorkingDirectory")]
    public async Task RunAsync_ExecutesInSpecifiedWorkingDirectory()
    {
        var sut = new DefaultProcessRunner();
        var uniqueDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(uniqueDir);

        try
        {
            // Create a marker file to verify we're in the right directory
            var markerFile = Path.Combine(uniqueDir, "marker.txt");
            await File.WriteAllTextAsync(markerFile, "test");

            // This command will fail if marker.txt doesn't exist in working directory
            await Assert.That(async () => await sut.RunAsync("cat", "marker.txt", uniqueDir)).ThrowsNothing();
        }
        finally
        {
            Directory.Delete(uniqueDir, true);
        }
    }
}
