using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Windows.Native;

namespace HarmonyPrompts.Windows.Services;

public sealed class WindowFocusService : IDisposable
{
    private readonly uint _ourPid = (uint)Environment.ProcessId;
    private readonly Win32.WinEventProc _callback;
    private readonly Dictionary<nint, long> _windowGenerations = new();
    private long _nextGeneration;
    private nint _foregroundHook;
    private nint _destroyHook;
    private PasteTarget? _lastExternal;
    private PasteTarget? _sessionTarget;
    private bool _inHarmony;
    private Guard? _guard;
    public bool IsTracking => _foregroundHook != 0 && _destroyHook != 0;
    public PasteTarget? SessionTarget => _sessionTarget;
    public event EventHandler? TargetChanged;

    public WindowFocusService()
    {
        _callback = OnWindowEvent;
        _foregroundHook = Win32.SetWinEventHook(Win32.EventSystemForeground, Win32.EventSystemForeground, 0, _callback, 0, 0, 0);
        _destroyHook = Win32.SetWinEventHook(Win32.EventObjectDestroy, Win32.EventObjectDestroy, 0, _callback, 0, 0, 0);
        Observe(Win32.GetForegroundWindow());
    }

    public bool IsHarmony(nint window)
    {
        Win32.GetWindowThreadProcessId(window, out var pid);
        return pid == _ourPid;
    }

    // Called immediately before tray/lifecycle code shows any Harmony window.
    // Recognized shell transitions can preserve a candidate; arbitrary failed
    // captures must clear it instead of resurrecting an older application's HWND.
    public void CaptureBeforeOpen()
    {
        Observe(Win32.GetForegroundWindow());
        _sessionTarget = IsTracking ? _lastExternal : null;
        TargetChanged?.Invoke(this, EventArgs.Empty);
    }

    public IPasteGuard BeginGuard(PasteTarget target)
    {
        _guard?.Dispose();
        var guard = new Guard(this, target.WindowHandle);
        _guard = guard;
        guard.Observe(Win32.GetForegroundWindow());
        return guard;
    }

    private void OnWindowEvent(nint hook, uint eventType, nint hwnd, int objectId, int childId, uint threadId, uint time)
    {
        // Out-of-context callbacks arrive on the registering UI message loop.
        // Keep the delegate rooted and never propagate an exception into native code.
        try
        {
            if (eventType == Win32.EventSystemForeground && _foregroundHook != 0) Observe(hwnd);
            else if (eventType == Win32.EventObjectDestroy && _destroyHook != 0 && objectId == 0 && childId == 0)
            {
                _windowGenerations.Remove(hwnd);
                if (_lastExternal?.WindowHandle == hwnd) _lastExternal = null;
                if (_sessionTarget?.WindowHandle == hwnd)
                { _sessionTarget = null; TargetChanged?.Invoke(this, EventArgs.Empty); }
                _guard?.OnDestroyed(hwnd);
            }
        }
        catch (Exception)
        {
            _sessionTarget = null; _lastExternal = null;
            _windowGenerations.Clear();
            _guard?.Interrupt();
        }
    }

    private void Observe(nint hwnd)
    {
        _guard?.Observe(hwnd);
        if (IsHarmony(hwnd))
        {
            if (!_inHarmony)
            {
                _sessionTarget = IsTracking ? _lastExternal : null;
                TargetChanged?.Invoke(this, EventArgs.Empty);
            }
            _inHarmony = true;
            return;
        }
        _inHarmony = false;
        if (IsShellTransition(hwnd)) return;
        _lastExternal = Capture(hwnd);
        if (_lastExternal is null && _sessionTarget is not null)
        { _sessionTarget = null; TargetChanged?.Invoke(this, EventArgs.Empty); }
    }

    private static bool IsShellTransition(nint hwnd)
    {
        var name = new StringBuilder(256);
        Win32.GetClassName(hwnd, name, name.Capacity);
        return name.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW" or "NotifyIconOverflowWindow";
    }

    internal PasteTarget? Capture(nint hwnd)
    {
        if (hwnd == 0 || IsHarmony(hwnd) || !Win32.IsWindow(hwnd) || !Win32.IsWindowVisible(hwnd) || IsShellTransition(hwnd)) return null;
        var name = new StringBuilder(256);
        Win32.GetClassName(hwnd, name, name.Capacity);
        // External modal/file dialogs are deliberately not generic paste targets.
        // Harmony-owned dialogs were already recognized by PID above.
        if (name.ToString() == "#32770") return null;
        Win32.GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            using var process = Process.GetProcessById(checked((int)pid));
            using var ours = Process.GetCurrentProcess();
            if (process.HasExited || process.SessionId != ours.SessionId ||
                process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)) return null;
            if (!_windowGenerations.TryGetValue(hwnd, out var generation))
                _windowGenerations[hwnd] = generation = ++_nextGeneration;
            return new(hwnd, pid, process.StartTime.ToUniversalTime().Ticks, process.ProcessName, generation);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or Win32Exception or OverflowException)
        { return null; }
    }

    public bool IsValid(PasteTarget target)
    {
        var live = Capture(target.WindowHandle);
        return live is not null && live.WindowHandle == target.WindowHandle && live.ProcessId == target.ProcessId &&
            live.ProcessStartTicks == target.ProcessStartTicks && live.WindowGeneration == target.WindowGeneration && !Win32.IsIconic(target.WindowHandle);
    }

    public void Dispose()
    {
        _guard?.Dispose();
        if (_foregroundHook != 0) { Win32.UnhookWinEvent(_foregroundHook); _foregroundHook = 0; }
        if (_destroyHook != 0) { Win32.UnhookWinEvent(_destroyHook); _destroyHook = 0; }
        GC.KeepAlive(_callback);
    }

    private sealed class Guard(WindowFocusService owner, nint target) : IPasteGuard
    {
        public bool WasInterrupted { get; private set; }
        internal void Interrupt() => WasInterrupted = true;
        internal void OnDestroyed(nint hwnd) { if (hwnd == target) Interrupt(); }
        internal void Observe(nint hwnd)
        {
            // Even shell/zero foreground interrupts an in-flight paste.
            if (hwnd != target && !owner.IsHarmony(hwnd)) Interrupt();
        }
        public void Dispose() { if (ReferenceEquals(owner._guard, this)) owner._guard = null; }
    }
}
