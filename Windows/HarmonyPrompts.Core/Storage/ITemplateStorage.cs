namespace HarmonyPrompts.Core.Storage;

// A prepared write has not touched the destination yet. Commit is a single atomic
// filesystem operation, allowing the repository to recheck the generation ticket.
public interface IPreparedTemplateWrite : IAsyncDisposable
{
    void Commit(bool overwrite);
}

public interface ITemplateStorage
{
    Task EnsureSeedAsync(string destination, string bundledPath, CancellationToken cancellationToken);
    Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken);
    Task<IPreparedTemplateWrite> PrepareWriteAsync(string path, byte[] data, CancellationToken cancellationToken);
}
