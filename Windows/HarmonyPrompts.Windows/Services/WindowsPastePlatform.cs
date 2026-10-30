using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Windows.Native;

namespace HarmonyPrompts.Windows.Services;

public sealed class WindowsPastePlatform(ClipboardService clipboard, WindowFocusService focus, Action hideHarmony) : IPastePlatform
{
    public Task<bool> CopyAsync(string text, CancellationToken cancellationToken) => clipboard.CopyAsync(text, cancellationToken);
    public bool IsHarmonyForeground => focus.IsHarmony(ForegroundWindow);
    public bool IsTargetValid(PasteTarget target) => focus.IsValid(target);
    public IPasteGuard BeginGuard(PasteTarget target) => focus.BeginGuard(target);
    public void HideHarmony() => hideHarmony();
    public nint ForegroundWindow => Win32.GetForegroundWindow();
    public bool TryActivate(PasteTarget target) => !IsTargetValid(target) ? false : Win32.SetForegroundWindow(target.WindowHandle);
    public bool IsClipboardCurrent => clipboard.IsCurrent;
    public bool HasHeldModifiers => IsDown(0x10) || IsDown(0x11) || IsDown(0x12) || IsDown(0x5B) || IsDown(0x5C) || IsDown(0x56);
    private static bool IsDown(int key) => (Win32.GetAsyncKeyState(key) & 0x8000) != 0;

    public bool CanSendInput(PasteTarget target)
    {
        if (!focus.IsTracking || !Win32.IsDefaultInputDesktop() || !IsTargetValid(target)) return false;
        var process = Win32.OpenProcess(0x1000, false, target.ProcessId);
        if (process == 0) return false;
        try
        {
            var ours = Win32.IntegrityLevel(Win32.GetCurrentProcess());
            var theirs = Win32.IntegrityLevel(process);
            return ours.HasValue && theirs.HasValue && theirs.Value <= ours.Value;
        }
        finally { Win32.CloseHandle(process); }
    }

    public PasteInputResult TrySendPaste(PasteTarget target, IPasteGuard guard)
    {
        if (guard.WasInterrupted || !CanSendInput(target) || HasHeldModifiers || !IsClipboardCurrent ||
            Win32.GetForegroundWindow() != target.WindowHandle) return PasteInputResult.Blocked;
        // No awaits, UI dispatch or focus tricks between final check and this call.
        return PasteInputSequence.Send(keys =>
        {
            var inputs = Array.ConvertAll(keys, key => Win32.Key(key.VirtualKey, key.IsKeyUp));
            return Win32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32.Input>());
        });
    }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}
