namespace HarmonyPrompts.Core.Services;

public enum PasteResult { CopiedOnly, InputSent, InputUncertain, KeyReleaseUncertain, NoTarget, TargetGone, FocusDenied, InputBlocked, ClipboardError }
public enum PasteInputResult { Blocked, Sent, Uncertain, KeyReleaseUncertain }
public sealed record PasteTarget(nint WindowHandle, uint ProcessId, long ProcessStartTicks, string DisplayName, long WindowGeneration = 0);

public interface IPasteGuard : IDisposable { bool WasInterrupted { get; } }

public interface IPastePlatform
{
    Task<bool> CopyAsync(string text, CancellationToken cancellationToken);
    bool IsHarmonyForeground { get; }
    bool IsTargetValid(PasteTarget target);
    bool CanSendInput(PasteTarget target);
    IPasteGuard BeginGuard(PasteTarget target);
    void HideHarmony();
    bool TryActivate(PasteTarget target);
    nint ForegroundWindow { get; }
    bool HasHeldModifiers { get; }
    bool IsClipboardCurrent { get; }
    // Adapter rechecks identity, clipboard, modifiers and exact foreground
    // synchronously immediately before the single four-event SendInput call.
    PasteInputResult TrySendPaste(PasteTarget target, IPasteGuard guard);
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class PasteService(IPastePlatform platform)
{
    private int _busy;
    public async Task<PasteResult> ExecuteAsync(string text, PasteTarget? target, bool paste,
        CancellationToken cancellationToken = default)
    {
        // Never overlap clipboard/focus/input operations, including rapid clicks.
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return PasteResult.InputBlocked;
        try
        {
            using var guard = paste && target is not null ? platform.BeginGuard(target) : null;
            if (!await platform.CopyAsync(text, cancellationToken)) return PasteResult.ClipboardError;
            if (!paste) return PasteResult.CopiedOnly;
            if (target is null) return PasteResult.NoTarget;
            if (!platform.IsTargetValid(target)) return PasteResult.TargetGone;
            if (!platform.IsHarmonyForeground || guard!.WasInterrupted) return PasteResult.FocusDenied;
            if (!platform.CanSendInput(target)) return PasteResult.InputBlocked;
            // Keyboard shortcut modifiers must be released, not forcibly reset.
            for (var attempt = 0; platform.HasHeldModifiers && attempt < 12; attempt++)
            {
                if (guard.WasInterrupted) return PasteResult.FocusDenied;
                await platform.DelayAsync(TimeSpan.FromMilliseconds(25), cancellationToken);
            }
            if (platform.HasHeldModifiers) return PasteResult.InputBlocked;
            if (guard.WasInterrupted || !platform.IsHarmonyForeground) return PasteResult.FocusDenied;
            platform.HideHarmony();
            var activationAccepted = platform.TryActivate(target);
            if (!activationAccepted && platform.ForegroundWindow != target.WindowHandle) return PasteResult.FocusDenied;
            var stableSamples = 0;
            for (var attempt = 0; attempt < 16; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!platform.IsTargetValid(target)) return PasteResult.TargetGone;
                if (guard.WasInterrupted) return PasteResult.FocusDenied;
                if (platform.ForegroundWindow == target.WindowHandle) stableSamples++;
                else stableSamples = 0;
                if (stableSamples >= 3)
                {
                    if (!platform.IsClipboardCurrent || platform.HasHeldModifiers) return PasteResult.InputBlocked;
                    return platform.TrySendPaste(target, guard) switch
                    {
                        PasteInputResult.Sent => PasteResult.InputSent,
                        PasteInputResult.Uncertain => PasteResult.InputUncertain,
                        PasteInputResult.KeyReleaseUncertain => PasteResult.KeyReleaseUncertain,
                        _ => PasteResult.InputBlocked
                    };
                }
                await platform.DelayAsync(TimeSpan.FromMilliseconds(25), cancellationToken);
            }
            return PasteResult.FocusDenied;
        }
        catch (OperationCanceledException) { return PasteResult.InputBlocked; }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
