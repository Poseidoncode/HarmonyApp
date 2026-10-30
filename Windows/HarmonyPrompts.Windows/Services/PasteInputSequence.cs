using System;
using HarmonyPrompts.Core.Services;

namespace HarmonyPrompts.Windows.Services;

// Keep the native event-count/cleanup policy testable without loading WPF or user32.
internal readonly record struct PasteKey(ushort VirtualKey, bool IsKeyUp = false);

internal static class PasteInputSequence
{
    internal static PasteInputResult Send(Func<PasteKey[], uint> send)
    {
        var paste = new[] { new PasteKey(0x11), new PasteKey(0x56), new PasteKey(0x56, true), new PasteKey(0x11, true) };
        var inserted = send(paste);
        if (inserted == paste.Length) return PasteInputResult.Sent;
        // Release only keys this call may have injected down. Never retry V-down.
        // A zero return accepted no key-downs and needs no cleanup.
        if (inserted == 0) return PasteInputResult.Blocked;
        var release = inserted == 2
            ? new[] { new PasteKey(0x56, true), new PasteKey(0x11, true) }
            : new[] { new PasteKey(0x11, true) };
        if (send(release) != release.Length) return PasteInputResult.KeyReleaseUncertain;
        return inserted >= 2 ? PasteInputResult.Uncertain : PasteInputResult.Blocked;
    }
}
