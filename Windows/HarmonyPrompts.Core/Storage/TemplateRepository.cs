using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Core.Services;

namespace HarmonyPrompts.Core.Storage;

public enum RepositoryOutcome { Applied, Superseded }
public sealed record RepositoryResult(RepositoryOutcome Outcome, IReadOnlyList<PromptTemplate> Templates);

// No WPF/Win32 dependencies: the same production repository is fault-tested on net10.0.
public sealed class TemplateRepository(string templatesPath, string bundledPath, ITemplateStorage? storage = null)
{
    private readonly ITemplateStorage _storage = storage ?? new FileTemplateStorage();
    private readonly object _sync = new();
    private Task _tail = Task.CompletedTask;
    private long _importTicket;
    private long _revision;
    private IReadOnlyList<PromptTemplate> _current = Array.Empty<PromptTemplate>();
    public string TemplatesPath { get; } = Path.GetFullPath(templatesPath);
    public IReadOnlyList<PromptTemplate> Current { get { lock (_sync) return _current; } }

    public Task<RepositoryResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var revision = _revision;
            return Enqueue(async () =>
            {
                await _storage.EnsureSeedAsync(TemplatesPath, bundledPath, cancellationToken).ConfigureAwait(false);
                var data = await _storage.ReadAsync(TemplatesPath, cancellationToken).ConfigureAwait(false);
                var decoded = TemplateJson.Deserialize(data);
                lock (_sync)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (revision != _revision) return Result(RepositoryOutcome.Superseded);
                    _current = decoded;
                    _revision++;
                    return Result(RepositoryOutcome.Applied);
                }
            });
        }
    }

    public async Task<RepositoryResult> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        long ticket;
        lock (_sync) ticket = ++_importTicket;
        IReadOnlyList<PromptTemplate> decoded;
        byte[] normalized;
        try
        {
            var data = await _storage.ReadAsync(path, cancellationToken).ConfigureAwait(false);
            decoded = await Task.Run(() => TemplateJson.Deserialize(data, requireNonEmpty: true), cancellationToken).ConfigureAwait(false);
            normalized = TemplateJson.Serialize(decoded);
        }
        catch
        {
            lock (_sync)
                if (ticket != _importTicket) return Result(RepositoryOutcome.Superseded);
            throw;
        }
        return await Enqueue(async () =>
        {
            lock (_sync)
                if (ticket != _importTicket) return Result(RepositoryOutcome.Superseded);
            await using var prepared = await _storage.PrepareWriteAsync(TemplatesPath, normalized, cancellationToken).ConfigureAwait(false);
            lock (_sync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ticket != _importTicket) return Result(RepositoryOutcome.Superseded);
                // Linearization point: no newer import can start between the
                // final ticket check, atomic rename and memory adoption.
                prepared.Commit(overwrite: true);
                _current = decoded;
                _revision++;
                return Result(RepositoryOutcome.Applied);
            }
        }).ConfigureAwait(false);
    }

    public Task<RepositoryResult> SaveAsync(IReadOnlyList<PromptTemplate> templates, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            // Snapshot now, not when an earlier save eventually finishes.
            var data = TemplateJson.Serialize(templates);
            var snapshot = TemplateJson.Deserialize(data);
            // A newer validated save also supersedes imports still reading or
            // preparing outside the queue; otherwise an old read can land last.
            _importTicket++;
            return Enqueue(async () =>
            {
                await using var prepared = await _storage.PrepareWriteAsync(TemplatesPath, data, cancellationToken).ConfigureAwait(false);
                lock (_sync)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    prepared.Commit(overwrite: true);
                    _current = snapshot;
                    _revision++;
                    return Result(RepositoryOutcome.Applied);
                }
            });
        }
    }

    public Task WhenIdleAsync() { lock (_sync) return _tail; }
    private RepositoryResult Result(RepositoryOutcome outcome) => new(outcome, _current);

    private Task<RepositoryResult> Enqueue(Func<Task<RepositoryResult>> operation)
    {
        lock (_sync)
        {
            var prior = _tail;
            var task = Task.Run(async () =>
            {
                // A failed operation is reported to its caller, not allowed to
                // poison every subsequent load/import/save in the FIFO chain.
                try { await prior.ConfigureAwait(false); }
                catch (Exception) { }
                return await operation().ConfigureAwait(false);
            });
            _tail = task;
            return task;
        }
    }
}
