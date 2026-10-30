using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Windows.Services;

namespace HarmonyPrompts.PasteHarness;

// Local, interactive-desktop test tool. Never targets arbitrary user applications.
// It writes only synthetic text and prints only test names/results, not clipboard text.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var window = new TargetWindow(args.Length > 0 && args[0].StartsWith("--verify", StringComparison.Ordinal) ? "Driver" : "Target",
            args.Length > 1 && args[0] == "--target" && long.TryParse(args[1], out var owner) ? (nint)owner : 0);
        app.MainWindow = window;
        if (args.Length > 0 && args[0].StartsWith("--verify", StringComparison.Ordinal))
            window.Loaded += async (_, _) =>
            {
                var exit = 1;
                try
                {
                    await new DesktopChecks(window).RunAsync(args.Length > 1 ? Path.GetFullPath(args[1]) : null, args[0] == "--verify-app");
                    exit = 0;
                }
                catch (CheckFailure failure) { Console.WriteLine("FAIL " + failure.Message); }
                catch (Exception error) { Console.WriteLine("FAIL unexpected " + error.GetType().Name); Console.WriteLine(error.StackTrace); }
                finally { app.Shutdown(exit); }
            };
        return app.Run(window);
    }
}

internal sealed class TargetWindow : Window
{
    private readonly TextBlock _status = new() { Text = "Ready", Margin = new Thickness(0, 8, 0, 0) };
    private bool _clipboardLocked;
    internal TargetWindow(string role, nint driver = 0)
    {
        Title = $"Harmony Paste Harness — {role} (PID {Environment.ProcessId})";
        Width = 650; Height = 460;
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "Synthetic paste target — not a user document", FontWeight = FontWeights.Bold });
        var input = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 220, Margin = new Thickness(0, 12, 0, 8) };
        AutomationProperties.SetAutomationId(input, "TargetInput");
        AutomationProperties.SetName(input, "Synthetic target input");
        panel.Children.Add(input);
        var clear = new Button { Content = "Clear target", HorizontalAlignment = HorizontalAlignment.Left };
        clear.Click += (_, _) => { input.Clear(); input.Focus(); };
        panel.Children.Add(clear);
        var returnToDriver = new Button { Content = "Return to test driver", HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetAutomationId(returnToDriver, "ReturnToDriver");
        // The foreground test partner itself makes an ordinary foreground request.
        // No AttachThreadInput, foreground-lock changes, elevation or production bypass.
        returnToDriver.Click += (_, _) => { input.Focus(); if (driver != 0) Native.SetForegroundWindow(driver); };
        panel.Children.Add(returnToDriver);
        var busy = new Button { Content = "Hold clipboard for 8 seconds", HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetAutomationId(busy, "LockClipboard");
        busy.Click += async (_, _) =>
        {
            if (_clipboardLocked || !Native.OpenClipboard(new WindowInteropHelper(this).Handle)) { _status.Text = "Lock failed"; return; }
            _clipboardLocked = true; _status.Text = "Clipboard locked";
            try { await Task.Delay(8000); }
            finally { if (_clipboardLocked) Native.CloseClipboard(); _clipboardLocked = false; _status.Text = "Ready"; }
        };
        panel.Children.Add(busy);
        var dialog = new Button { Content = "Open unsupported modal dialog", HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetAutomationId(dialog, "UnsupportedDialog");
        dialog.Click += (_, _) => MessageBox.Show(this, "Synthetic unsupported target", "Harness dialog", MessageBoxButton.OK);
        panel.Children.Add(dialog);
        AutomationProperties.SetAutomationId(_status, "HarnessStatus");
        panel.Children.Add(_status);
        Content = panel;
        Loaded += (_, _) => input.Focus();
        Closed += (_, _) => { if (_clipboardLocked) Native.CloseClipboard(); _clipboardLocked = false; };
    }
}

internal sealed class DesktopChecks(TargetWindow driver)
{
    private readonly List<Process> _children = new();
    private string? _createdCollectionPath;
    private string? _seedPath;
    private nint DriverHandle => new WindowInteropHelper(driver).Handle;
    private void Pass(string name) => Console.WriteLine("PASS " + name);
    private static void Check(bool condition, string name) { if (!condition) throw new CheckFailure(name); }

    internal async Task RunAsync(string? appPath, bool appOnly = false)
    {
        Console.WriteLine("Interactive Windows desktop checks; leave these synthetic test windows in control until completion.");
        try
        {
            var targetProcess = Start(Environment.ProcessPath!, "--target " + DriverHandle);
            var targetWindow = await WindowAsync(targetProcess);
            var targetInput = Find(targetWindow, "TargetInput");
            if (appOnly)
            {
                Check(appPath is not null, "product executable path required");
                await CheckApplicationAsync(appPath!, targetProcess, targetInput);
                return;
            }
            using var focus = new WindowFocusService();
            Check(focus.IsTracking, "native foreground/destroy hooks installed");
            var clipboard = new ClipboardService(driver.Dispatcher);
            var platform = new WindowsPastePlatform(clipboard, focus, driver.Hide);
            var paste = new PasteService(platform);

            const string synthetic = "Harmony smoke 你好 👩🏽‍💻\r\nSecond line";
            Check(await clipboard.CopyAsync(synthetic, CancellationToken.None), "native clipboard payload/marker transaction");
            Check(clipboard.IsCurrent, "native clipboard sequence certificate");
            await WaitAsync(() => Native.ReadDword("CanIncludeInClipboardHistory") == 0 && Native.ReadDword("CanUploadToCloudClipboard") == 0,
                "raw clipboard history/cloud opt-out DWORDs");
            Pass("clipboard Unicode payload, operation marker, sequence and history/cloud opt-out formats");
            System.Windows.Clipboard.SetText("Synthetic competing clipboard writer");
            Check(!clipboard.IsCurrent, "external clipboard change invalidates certificate");
            Check(!await clipboard.CopyAsync("Synthetic\0truncation", CancellationToken.None), "embedded NUL is rejected without input");
            Pass("clipboard replacement invalidation and NUL fail-closed behavior");

            var failedFlush = new ClipboardService(driver.Dispatcher, data =>
            {
                System.Windows.Clipboard.SetDataObject(data, copy: false);
                throw new COMException("Synthetic publish-success/flush-failure");
            });
            Check(!await failedFlush.CopyAsync("Synthetic failed flush", CancellationToken.None) && !failedFlush.IsCurrent,
                "failed clipboard flush cannot certify a copy");
            await WaitAsync(() => Native.ReadDword("CanIncludeInClipboardHistory") == 0 && Native.ReadDword("CanUploadToCloudClipboard") == 0,
                "failed flush retains renderable privacy streams");
            using (var cancelFlush = new CancellationTokenSource())
            {
                var cancelledFlush = new ClipboardService(driver.Dispatcher, data =>
                {
                    System.Windows.Clipboard.SetDataObject(data, copy: false);
                    cancelFlush.Cancel();
                    throw new COMException("Synthetic cancelled flush");
                });
                var cancelled = false;
                try { await cancelledFlush.CopyAsync("Synthetic cancelled flush", cancelFlush.Token); }
                catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled && !cancelledFlush.IsCurrent, "cancelled flush never certifies a copy");
                await WaitAsync(() => Native.ReadDword("CanIncludeInClipboardHistory") == 0 && Native.ReadDword("CanUploadToCloudClipboard") == 0,
                    "cancelled flush retains renderable privacy streams");
            }
            Pass("OLE publication followed by flush failure/cancellation retains history/cloud exclusion streams");

            var target = await CaptureAsync(focus, targetProcess, targetInput);
            await ActivateDriverAsync();
            var result = await paste.ExecuteAsync(synthetic, target, true);
            Check(result == PasteResult.InputSent, $"guarded native Ctrl+V returned InputSent (actual {result})");
            await WaitAsync(() => Value(targetInput) == synthetic, "actual cross-process Unicode text arrival");
            Pass("real cross-process WPF target received Ctrl+V and exact synthetic Unicode text");

            var decoyProcess = Start(Environment.ProcessPath!, "--target " + DriverHandle);
            var decoyWindow = await WindowAsync(decoyProcess);
            var decoyInput = Find(decoyWindow, "TargetInput");
            SetValue(targetInput, "");
            target = await CaptureAsync(focus, targetProcess, targetInput);
            await ActivateDriverAsync();
            var interrupting = new DelayActionPlatform(platform, () => Native.SetForegroundWindow(decoyProcess.MainWindowHandle));
            result = await new PasteService(interrupting).ExecuteAsync("Synthetic must not be sent", target, true);
            Check(result == PasteResult.FocusDenied, "third-window switch must deny paste");
            Check(Value(targetInput) == "" && Value(decoyInput) == "", "no target or decoy input after interruption");
            Pass("real third-process foreground interruption sent no text to either window");

            target = await CaptureAsync(focus, targetProcess, targetInput);
            await ActivateDriverAsync();
            var replacing = new DelayActionPlatform(platform, () => System.Windows.Clipboard.SetText("Synthetic replacement"));
            result = await new PasteService(replacing).ExecuteAsync("Synthetic stale clipboard", target, true);
            Check(result == PasteResult.InputBlocked && Value(targetInput) == "", $"clipboard changed during activation blocks input (actual {result})");
            Pass("real clipboard replacement during activation blocks input");

            target = await CaptureAsync(focus, targetProcess, targetInput);
            Invoke(Find(targetWindow, "UnsupportedDialog"));
            await WaitAsync(() => Native.ClassName(Native.GetForegroundWindow()) == "#32770", "unsupported modal foreground");
            var modal = Native.GetForegroundWindow();
            await Task.Delay(150);
            await ActivateDriverAsync();
            Check(focus.SessionTarget is null, "unsupported foreground clears stale candidate");
            Native.PostMessage(modal, 0x0010, 0, 0);
            Pass("unsupported external dialog cannot resurrect an older paste candidate");

            Invoke(Find(targetWindow, "LockClipboard"));
            await WaitAsync(() => Find(targetWindow, "HarnessStatus").Current.Name == "Clipboard locked", "clipboard lock acquired");
            var timer = Stopwatch.StartNew();
            Check(!await clipboard.CopyAsync("Synthetic busy clipboard", CancellationToken.None), "busy clipboard must not report success");
            Check(timer.Elapsed < TimeSpan.FromSeconds(8), "clipboard busy handling is bounded");
            await WaitAsync(() => Find(targetWindow, "HarnessStatus").Current.Name == "Ready", "clipboard lock released", 10000);
            Pass("real clipboard contention returns a bounded error and recovers");

            target = await CaptureAsync(focus, targetProcess, targetInput);
            ((WindowPattern)targetWindow.GetCurrentPattern(WindowPattern.Pattern)).Close();
            await targetProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100);
            await ActivateDriverAsync();
            Check(!focus.IsValid(target), "destroyed target identity invalid");
            result = await paste.ExecuteAsync("Synthetic closed-target fallback", target, true);
            Check(result == PasteResult.TargetGone && clipboard.IsCurrent, "closed target keeps copied text without input");
            Pass("native destroy event invalidation and closed-target clipboard fallback");

            if (appPath is not null) await CheckApplicationAsync(appPath, decoyProcess, decoyInput);
            Console.WriteLine("PASS all requested controlled-desktop checks completed");
        }
        finally
        {
            foreach (var process in _children)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                process.Dispose();
            }
            if (_createdCollectionPath is not null && _seedPath is not null && File.Exists(_createdCollectionPath) && File.Exists(_seedPath) &&
                File.ReadAllBytes(_createdCollectionPath).AsSpan().SequenceEqual(File.ReadAllBytes(_seedPath)))
                File.Delete(_createdCollectionPath); // Only this run's unchanged synthetic default collection.
        }
    }

    private async Task CheckApplicationAsync(string path, Process external, AutomationElement externalInput)
    {
        var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HarmonyPrompts", "templates.json");
        // Never load, edit or echo an existing user's template collection.
        if (File.Exists(dataPath) || Process.GetProcessesByName("HarmonyPrompts").Length != 0)
        { Console.WriteLine("SKIP product UI smoke: existing user collection or app instance; preserve user state"); return; }
        _createdCollectionPath = dataPath;
        _seedPath = Path.Combine(Path.GetDirectoryName(path)!, "Resources", "templates.json");
        SetValue(externalInput, "");
        externalInput.SetFocus();
        Check(Native.SetForegroundWindow(external.MainWindowHandle), "controlled target foreground before app launch");
        var product = Start(path, "");
        var window = await WindowAsync(product);
        await WaitAsync(() => Find(window, "StatusMessage").Current.Name.StartsWith("Loaded 5", StringComparison.Ordinal), "product built-in templates loaded");
        var preview = Find(window, "PreviewBox");
        var language = window.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.NameProperty, "Language"),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
        Check(language is not null, "dynamic text field accessible");
        SetValue(language!, "C# smoke 你好");
        await WaitAsync(() => Value(preview).Contains("C# smoke 你好", StringComparison.Ordinal), "actual dynamic field updates preview");
        var expected = Value(preview);
        var search = Find(window, "SearchBox");
        SetValue(search, "not-a-template-smoke");
        await Task.Delay(100);
        Check(Value(preview) == expected, "filtered form values retained");
        SetValue(search, "");
        await Task.Delay(150);
        var list = Find(window, "TemplateList");
        Check(((SelectionPattern)list.GetCurrentPattern(SelectionPattern.Pattern)).Current.GetSelection().Length == 1, "visible selection restored after filter");
        Invoke(Find(window, "ReloadButton"));
        await Task.Delay(250);
        Check(Value(preview) == expected, "unchanged product reload retains typed fields");
        Invoke(Find(window, "CopyButton"));
        await WaitAsync(() => Find(window, "StatusMessage").Current.Name == "Copied to clipboard.", "product Copy command succeeds");
        Check(System.Windows.Clipboard.GetText() == expected, "actual product copied generated text");
        Pass("actual WPF app startup, text field, preview, search selection, unchanged reload and Copy");
        // Launch focus can change before the product captures its target. Do not
        // enable experimental input or assume a launched app retained our target.
        // The native checks above freeze a separately verified owned HWND/PID.
        Check(!Find(window, "PasteButton").Current.IsEnabled, "product Auto Paste must be disabled by default");
        Check(Value(externalInput) == "", "default product workflow sends no input");
        Pass("product Auto Paste is disabled by default; smoke never enables experimental global input");
        Native.PostMessage(product.MainWindowHandle, 0x0010, 0, 0);
        await Task.Delay(150);
        Check(!product.HasExited, "product close hides instead of exiting");
        Pass("product remains running for tray after window close");
        // App is an owned child of this smoke run; cleanup terminates it. The
        // tray Exit command, shell tray re-open and shutdown disposal remain manual.
    }

    private Process Start(string path, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo(path, arguments) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path)! })
            ?? throw new CheckFailure("controlled child process did not start");
        _children.Add(process);
        return process;
    }
    private async Task<PasteTarget> CaptureAsync(WindowFocusService focus, Process process, AutomationElement input)
    {
        input.SetFocus();
        Native.SetForegroundWindow(process.MainWindowHandle);
        await WaitAsync(() => Native.GetForegroundWindow() == process.MainWindowHandle, "controlled target foreground");
        await Task.Delay(120);
        focus.CaptureBeforeOpen();
        var target = focus.SessionTarget;
        Check(target is not null && target.ProcessId == process.Id && target.WindowHandle == process.MainWindowHandle,
            $"captured exact controlled HWND/PID (expected {process.Id}/{process.MainWindowHandle}, actual {target?.ProcessId}/{target?.WindowHandle}, foreground {Native.GetForegroundWindow()})");
        return target!;
    }
    private async Task ActivateDriverAsync()
    {
        driver.Show();
        var foreground = Native.GetForegroundWindow();
        foreach (var process in _children)
        {
            process.Refresh();
            if (!process.HasExited && process.MainWindowHandle == foreground && process.ProcessName.Contains("PasteHarness", StringComparison.Ordinal))
            {
                Invoke(Find(AutomationElement.FromHandle(foreground), "ReturnToDriver"));
                break;
            }
        }
        driver.Activate(); Native.SetForegroundWindow(DriverHandle);
        await WaitAsync(() => Native.GetForegroundWindow() == DriverHandle, "driver foreground");
        await Task.Delay(100);
    }
    private static async Task<AutomationElement> WindowAsync(Process process)
    {
        await WaitAsync(() => { process.Refresh(); return process.MainWindowHandle != 0; }, "controlled child main window");
        return AutomationElement.FromHandle(process.MainWindowHandle);
    }
    private static AutomationElement Find(AutomationElement root, string id) => root.FindFirst(TreeScope.Descendants,
        new PropertyCondition(AutomationElement.AutomationIdProperty, id)) ?? throw new CheckFailure("missing control " + id);
    private static string Value(AutomationElement input) => ((ValuePattern)input.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
    private static void SetValue(AutomationElement input, string value) => ((ValuePattern)input.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
    private static void Invoke(AutomationElement button) => ((InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    private static async Task WaitAsync(Func<bool> condition, string label, int milliseconds = 6000)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.ElapsedMilliseconds > milliseconds) throw new CheckFailure("timeout: " + label);
            await Task.Delay(40);
        }
    }
}

internal sealed class DelayActionPlatform(IPastePlatform inner, Action action) : IPastePlatform
{
    private bool _ran;
    public Task<bool> CopyAsync(string text, CancellationToken token) => inner.CopyAsync(text, token);
    public bool IsHarmonyForeground => inner.IsHarmonyForeground;
    public bool IsTargetValid(PasteTarget target) => inner.IsTargetValid(target);
    public bool CanSendInput(PasteTarget target) => inner.CanSendInput(target);
    public IPasteGuard BeginGuard(PasteTarget target) => inner.BeginGuard(target);
    public void HideHarmony() => inner.HideHarmony();
    public bool TryActivate(PasteTarget target) => inner.TryActivate(target);
    public nint ForegroundWindow => inner.ForegroundWindow;
    public bool HasHeldModifiers => inner.HasHeldModifiers;
    public bool IsClipboardCurrent => inner.IsClipboardCurrent;
    public PasteInputResult TrySendPaste(PasteTarget target, IPasteGuard guard) => inner.TrySendPaste(target, guard);
    public async Task DelayAsync(TimeSpan delay, CancellationToken token)
    { if (!_ran) { _ran = true; action(); } await inner.DelayAsync(delay, token); }
}

internal sealed class CheckFailure(string message) : Exception(message);
internal static class Native
{
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool PostMessage(nint hwnd, uint message, nint wparam, nint lparam);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool OpenClipboard(nint owner);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseClipboard();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterClipboardFormat(string name);
    [DllImport("user32.dll")] private static extern nint GetClipboardData(uint format);
    [DllImport("kernel32.dll")] private static extern nint GlobalLock(nint handle);
    [DllImport("kernel32.dll")] private static extern nuint GlobalSize(nint handle);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalUnlock(nint handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint hwnd, System.Text.StringBuilder text, int count);
    internal static string ClassName(nint hwnd) { var text = new System.Text.StringBuilder(256); GetClassName(hwnd, text, text.Capacity); return text.ToString(); }
    internal static int? ReadDword(string format)
    {
        if (!OpenClipboard(0)) return null;
        try
        {
            var memory = GetClipboardData(RegisterClipboardFormat(format));
            if (memory == 0 || GlobalSize(memory) < 4) return null;
            var pointer = GlobalLock(memory);
            if (pointer == 0) return null;
            try { return Marshal.ReadInt32(pointer); }
            finally { GlobalUnlock(memory); }
        }
        finally { CloseClipboard(); }
    }
}
