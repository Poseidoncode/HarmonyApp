using System.Collections.Concurrent;
using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Core.Storage;

namespace HarmonyPrompts.Tests;

public sealed class RepositoryTests
{
    private static (TemplateRepository Repository, ControlledStorage Storage) Create()
    {
        var storage = new ControlledStorage();
        storage.Files["bundle"] = TestData.Bytes("default");
        var repository = new TemplateRepository(Path.Combine(Path.GetTempPath(), "harmony-test-memory", "templates.json"), "bundle", storage);
        return (repository, storage);
    }

    [Fact]
    public async Task FirstLoadSeedsButSubsequentLoadsNeverOverwriteUserFile()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        Assert.Equal("default", Assert.Single(repository.Current).Id);
        storage.Files[repository.TemplatesPath] = TestData.Bytes("user");
        await repository.LoadAsync();
        Assert.Equal("user", Assert.Single(repository.Current).Id);
        Assert.Equal(1, storage.Seeds);
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task InvalidImportPreservesMemoryAndDisk(string json)
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        var old = storage.Files[repository.TemplatesPath].ToArray();
        storage.Files["bad"] = System.Text.Encoding.UTF8.GetBytes(json);
        await Assert.ThrowsAnyAsync<Exception>(() => repository.ImportAsync("bad"));
        Assert.Equal("default", Assert.Single(repository.Current).Id);
        Assert.Equal(old, storage.Files[repository.TemplatesPath]);
        Assert.Empty(storage.Commits);
    }

    [Fact]
    public async Task SlowOldImportCannotReplaceNewerCompletedImport()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files["a"] = TestData.Bytes("A"); storage.Files["b"] = TestData.Bytes("B");
        var started = TestData.Signal(); var release = TestData.Signal();
        storage.BeforeRead = async path => { if (path == "a") { started.TrySetResult(true); await release.Task; } };
        var a = repository.ImportAsync("a");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var b = await repository.ImportAsync("b");
        release.TrySetResult(true);
        Assert.Equal(RepositoryOutcome.Applied, b.Outcome);
        Assert.Equal(RepositoryOutcome.Superseded, (await a).Outcome);
        Assert.Equal("B", Assert.Single(repository.Current).Id);
        Assert.Equal(new[] { "B" }, storage.Commits);
    }

    [Fact]
    public async Task SlowOldImportCannotOverwriteLaterValidatedSave()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files["a"] = TestData.Bytes("A");
        var started = TestData.Signal(); var release = TestData.Signal();
        storage.BeforeRead = async path => { if (path == "a") { started.TrySetResult(true); await release.Task; } };
        var a = repository.ImportAsync("a");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await repository.SaveAsync(new[] { TestData.Template("B") });
        release.TrySetResult(true);
        Assert.Equal(RepositoryOutcome.Superseded, (await a).Outcome);
        Assert.Equal("B", Assert.Single(repository.Current).Id);
        Assert.Equal(new[] { "B" }, storage.Commits);
    }

    [Fact]
    public async Task NewerInvalidImportStillInvalidatesOlderPendingImportLikeSwiftTicket()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files["a"] = TestData.Bytes("A"); storage.Files["bad"] = "[]"u8.ToArray();
        var started = TestData.Signal(); var release = TestData.Signal();
        storage.BeforeRead = async path => { if (path == "a") { started.TrySetResult(true); await release.Task; } };
        var a = repository.ImportAsync("a");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.ThrowsAsync<TemplateValidationException>(() => repository.ImportAsync("bad"));
        release.TrySetResult(true);
        Assert.Equal(RepositoryOutcome.Superseded, (await a).Outcome);
        Assert.Equal("default", Assert.Single(repository.Current).Id);
    }

    [Fact]
    public async Task ImportSupersededAfterPreparingNeverCommitsAndCleansTemporary()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files["a"] = TestData.Bytes("A"); storage.Files["b"] = TestData.Bytes("B");
        var preparedA = TestData.Signal(); var releaseA = TestData.Signal();
        var readB = TestData.Signal();
        storage.BeforePrepare = async data =>
        { if (TemplateJson.Deserialize(data)[0].Id == "A") { preparedA.TrySetResult(true); await releaseA.Task; } };
        storage.BeforeRead = path => { if (path == "b") readB.TrySetResult(true); return Task.CompletedTask; };
        var a = repository.ImportAsync("a");
        await preparedA.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var b = repository.ImportAsync("b");
        await readB.Task.WaitAsync(TimeSpan.FromSeconds(10));
        releaseA.TrySetResult(true);
        Assert.Equal(RepositoryOutcome.Superseded, (await a).Outcome);
        await b;
        Assert.Equal(new[] { "B" }, storage.Commits);
        Assert.Equal(2, storage.DisposedWrites);
    }

    [Fact]
    public async Task EarlierLoadDoesNotOverwriteNewerImportAndQueuedReloadSeesCommittedDisk()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        var started = TestData.Signal(); var release = TestData.Signal();
        storage.BeforePrepare = async _ => { started.TrySetResult(true); await release.Task; };
        storage.Files["b"] = TestData.Bytes("B");
        var import = repository.ImportAsync("b");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var reload = repository.LoadAsync();
        release.TrySetResult(true);
        await import;
        Assert.Equal(RepositoryOutcome.Superseded, (await reload).Outcome);
        Assert.Equal("B", Assert.Single(repository.Current).Id);
        Assert.Equal("B", TemplateJson.Deserialize(storage.Files[repository.TemplatesPath])[0].Id);
    }

    [Fact]
    public async Task SavesAreFifoSnapshotsAndReloadCannotReapplyStaleMemory()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        var started = TestData.Signal(); var release = TestData.Signal();
        storage.BeforePrepare = async data =>
        { if (TemplateJson.Deserialize(data)[0].Id == "A") { started.TrySetResult(true); await release.Task; } };
        var mutable = new List<HarmonyPrompts.Core.Models.PromptTemplate> { TestData.Template("A") };
        var a = repository.SaveAsync(mutable);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        mutable[0] = TestData.Template("mutated-after-call");
        var b = repository.SaveAsync(new[] { TestData.Template("B") });
        var reload = repository.LoadAsync();
        release.TrySetResult(true);
        await Task.WhenAll(a, b, reload);
        Assert.Equal(new[] { "A", "B" }, storage.Commits);
        Assert.Equal("B", Assert.Single(repository.Current).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteFailuresPreserveMemoryAndDiskAndDoNotPoisonQueue(bool unauthorized)
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files["b"] = TestData.Bytes("B");
        storage.CommitError = unauthorized ? new UnauthorizedAccessException("test") : new IOException("test");
        await Assert.ThrowsAnyAsync<Exception>(() => repository.ImportAsync("b"));
        Assert.Equal("default", Assert.Single(repository.Current).Id);
        Assert.Equal("default", TemplateJson.Deserialize(storage.Files[repository.TemplatesPath])[0].Id);
        Assert.Equal(1, storage.DisposedWrites);
        storage.CommitError = null;
        await repository.ImportAsync("b");
        await repository.WhenIdleAsync();
        Assert.Equal("B", Assert.Single(repository.Current).Id);
    }

    [Fact]
    public async Task CancelledPreparedImportPreservesOriginalAndDisposesTemporary()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files["a"] = TestData.Bytes("A");
        using var cts = new CancellationTokenSource();
        storage.BeforePrepare = _ => { cts.Cancel(); return Task.CompletedTask; };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.ImportAsync("a", cts.Token));
        Assert.Equal("default", Assert.Single(repository.Current).Id);
        Assert.Empty(storage.Commits);
        Assert.Equal(1, storage.DisposedWrites);
    }

    [Fact]
    public async Task CorruptExistingFileIsNotSeededOrDeletedAndMemoryRemainsUsable()
    {
        var (repository, storage) = Create();
        await repository.LoadAsync();
        storage.Files[repository.TemplatesPath] = "bad"u8.ToArray();
        await Assert.ThrowsAnyAsync<Exception>(() => repository.LoadAsync());
        Assert.Equal("bad"u8.ToArray(), storage.Files[repository.TemplatesPath]);
        Assert.Equal("default", Assert.Single(repository.Current).Id);
        Assert.Equal(1, storage.Seeds);
    }

    private sealed class ControlledStorage : ITemplateStorage
    {
        public ConcurrentDictionary<string, byte[]> Files { get; } = new();
        public List<string> Commits { get; } = new();
        public Func<string, Task>? BeforeRead { get; set; }
        public Func<byte[], Task>? BeforePrepare { get; set; }
        public Exception? CommitError { get; set; }
        public int Seeds { get; private set; }
        public int DisposedWrites { get; private set; }
        public Task EnsureSeedAsync(string destination, string bundledPath, CancellationToken cancellationToken)
        { if (Files.TryAdd(destination, Files[bundledPath].ToArray())) Seeds++; return Task.CompletedTask; }
        public async Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken)
        {
            if (BeforeRead is not null) await BeforeRead(path);
            cancellationToken.ThrowIfCancellationRequested();
            return Files[path].ToArray();
        }
        public async Task<IPreparedTemplateWrite> PrepareWriteAsync(string path, byte[] data, CancellationToken cancellationToken)
        {
            if (BeforePrepare is not null) await BeforePrepare(data);
            return new Prepared(this, path, data.ToArray());
        }
        private sealed class Prepared(ControlledStorage owner, string path, byte[] data) : IPreparedTemplateWrite
        {
            public void Commit(bool overwrite)
            {
                if (owner.CommitError is { } error) throw error;
                owner.Files[path] = data;
                owner.Commits.Add(TemplateJson.Deserialize(data)[0].Id);
            }
            public ValueTask DisposeAsync() { owner.DisposedWrites++; return ValueTask.CompletedTask; }
        }
    }
}
