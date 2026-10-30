using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Core.Storage;

namespace HarmonyPrompts.Tests;

public sealed class FileStorageTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "HarmonyPrompts-Tests", Guid.NewGuid().ToString("N"));
    public FileStorageTests() => Directory.CreateDirectory(_folder);
    private string UserPath => Path.Combine(_folder, "user", "templates.json");
    private string BundlePath => Path.Combine(_folder, "bundle.json");

    [Fact]
    public async Task RealFirstRunSeedSurvivesRestartAndNeverOverwritesCorruptExistingUserFile()
    {
        await File.WriteAllBytesAsync(BundlePath, TestData.Bytes("bundled"));
        var first = new TemplateRepository(UserPath, BundlePath);
        await first.LoadAsync();
        await first.SaveAsync(new[] { TestData.Template("custom") });
        var restarted = new TemplateRepository(UserPath, BundlePath);
        await restarted.LoadAsync();
        Assert.Equal("custom", Assert.Single(restarted.Current).Id);
        await File.WriteAllTextAsync(UserPath, "corrupt-user-data");
        await Assert.ThrowsAnyAsync<Exception>(() => restarted.LoadAsync());
        Assert.Equal("corrupt-user-data", await File.ReadAllTextAsync(UserPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(UserPath)!, "*.tmp"));
    }

    [Fact]
    public async Task ExistingEmptyArrayIsLoadedNotReplacedWithDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UserPath)!);
        await File.WriteAllTextAsync(UserPath, "[]");
        var repository = new TemplateRepository(UserPath, "missing-bundle-is-not-needed");
        await repository.LoadAsync();
        Assert.Empty(repository.Current);
        Assert.Equal("[]", await File.ReadAllTextAsync(UserPath));
    }

    [Fact]
    public async Task PreparedWriteIsInvisibleUntilCommitAndAbandonedWriteIsRemoved()
    {
        var storage = new FileTemplateStorage();
        await using (var prepared = await storage.PrepareWriteAsync(UserPath, TestData.Bytes("pending"), default))
        {
            Assert.False(File.Exists(UserPath));
            Assert.Single(Directory.GetFiles(Path.GetDirectoryName(UserPath)!, "*.tmp"));
        }
        Assert.False(File.Exists(UserPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(UserPath)!, "*.tmp"));
    }

    [Fact]
    public async Task DestinationCreatedDuringSeedWinsAndIsNeverOverwritten()
    {
        var storage = new FileTemplateStorage();
        await using var prepared = await storage.PrepareWriteAsync(UserPath, TestData.Bytes("seed"), default);
        await File.WriteAllBytesAsync(UserPath, TestData.Bytes("winner"));
        Assert.Throws<IOException>(() => prepared.Commit(overwrite: false));
        Assert.Equal("winner", TemplateJson.Deserialize(await File.ReadAllBytesAsync(UserPath))[0].Id);
    }

    [Fact]
    public async Task LockedDestinationFailsAtomicallyAndDoesNotLeaveTemporaryOrAdoptMemory()
    {
        if (!OperatingSystem.IsWindows()) return; // Windows FileShare semantics; Unix rename permits open files.
        await File.WriteAllBytesAsync(BundlePath, TestData.Bytes("old"));
        var repository = new TemplateRepository(UserPath, BundlePath);
        await repository.LoadAsync();
        var import = Path.Combine(_folder, "new.json");
        await File.WriteAllBytesAsync(import, TestData.Bytes("new"));
        using (var locked = new FileStream(UserPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await Assert.ThrowsAnyAsync<IOException>(() => repository.ImportAsync(import));
            Assert.Equal("old", Assert.Single(repository.Current).Id);
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(UserPath)!, "*.tmp"));
        }
        await repository.ImportAsync(import);
        Assert.Equal("new", Assert.Single(repository.Current).Id);
        Assert.Equal("new", TemplateJson.Deserialize(await File.ReadAllBytesAsync(UserPath))[0].Id);
    }

    [Fact]
    public async Task CancelledDiskWriteLeavesDestinationUnchangedAndCleansTemporary()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UserPath)!);
        await File.WriteAllBytesAsync(UserPath, TestData.Bytes("old"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var storage = new FileTemplateStorage();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.PrepareWriteAsync(UserPath, TestData.Bytes("new"), cts.Token));
        Assert.Equal("old", TemplateJson.Deserialize(await File.ReadAllBytesAsync(UserPath))[0].Id);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(UserPath)!, "*.tmp"));
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
