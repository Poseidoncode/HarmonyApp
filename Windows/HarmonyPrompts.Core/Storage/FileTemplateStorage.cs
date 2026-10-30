namespace HarmonyPrompts.Core.Storage;

public sealed class FileTemplateStorage : ITemplateStorage
{
    public async Task EnsureSeedAsync(string destination, string bundledPath, CancellationToken cancellationToken)
    {
        if (File.Exists(destination)) return;
        var defaults = await File.ReadAllBytesAsync(bundledPath, cancellationToken).ConfigureAwait(false);
        await using var prepared = await PrepareWriteAsync(destination, defaults, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        try { prepared.Commit(overwrite: false); }
        catch (IOException) when (File.Exists(destination))
        {
            // Another instance/user created a file while we prepared the seed.
            // Never overwrite it, even if it is malformed.
        }
    }

    public Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken) =>
        File.ReadAllBytesAsync(path, cancellationToken);

    public async Task<IPreparedTemplateWrite> PrepareWriteAsync(string path, byte[] data, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".templates-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            return new PreparedWrite(temporary, path);
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class PreparedWrite(string temporary, string destination) : IPreparedTemplateWrite
    {
        public void Commit(bool overwrite)
        {
            // Do not fall back to delete+move: failure must preserve the old file.
            if (overwrite && File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination, overwrite: false);
        }

        public ValueTask DisposeAsync()
        {
            TryDelete(temporary);
            return ValueTask.CompletedTask;
        }
    }
}
