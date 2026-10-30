using HarmonyPrompts.Core.Services;

namespace HarmonyPrompts.Tests;

public sealed class PasteServiceTests
{
    internal static PasteTarget Target { get; } = new((nint)42, 100, 123456, "Test target");

    [Fact]
    public async Task CopyOnlyNeverActivatesHidesOrSendsInput()
    {
        var platform = new FakePastePlatform();
        Assert.Equal(PasteResult.CopiedOnly, await new PasteService(platform).ExecuteAsync("你好 🧪", Target, false));
        Assert.Equal("你好 🧪", platform.CopiedText);
        Assert.Equal(0, platform.Hides); Assert.Equal(0, platform.Activations); Assert.Equal(0, platform.Sends);
    }
    [Fact]
    public async Task HappyPathCopiesThenRequiresThreeStableExactForegroundSamples()
    {
        var platform = new FakePastePlatform();
        Assert.Equal(PasteResult.InputSent, await new PasteService(platform).ExecuteAsync("test", Target, true));
        Assert.Equal(new[] { "copy", "hide", "activate", "send" }, platform.Events);
        Assert.Equal(2, platform.Delays); Assert.Equal(1, platform.Sends);
        Assert.True(platform.Guard.Disposed);
    }
    [Theory]
    [InlineData("clipboard", PasteResult.ClipboardError)]
    [InlineData("no-target", PasteResult.NoTarget)]
    [InlineData("identity", PasteResult.TargetGone)]
    [InlineData("not-harmony", PasteResult.FocusDenied)]
    [InlineData("interrupted", PasteResult.FocusDenied)]
    [InlineData("permission", PasteResult.InputBlocked)]
    [InlineData("held-shortcut", PasteResult.InputBlocked)]
    [InlineData("activation", PasteResult.FocusDenied)]
    [InlineData("clipboard-changed", PasteResult.InputBlocked)]
    [InlineData("input-failure", PasteResult.InputBlocked)]
    public async Task SafetyFailuresNeverReportInputSent(string scenario, PasteResult expected)
    {
        var platform = new FakePastePlatform();
        switch (scenario)
        {
            case "clipboard": platform.CopySucceeds = false; break;
            case "identity": platform.TargetValid = false; break;
            case "not-harmony": platform.IsHarmonyForeground = false; break;
            case "interrupted": platform.Guard.WasInterrupted = true; break;
            case "permission": platform.InputPermitted = false; break;
            case "held-shortcut": platform.HasHeldModifiers = true; break;
            case "activation": platform.ActivationSucceeds = false; break;
            case "clipboard-changed": platform.IsClipboardCurrent = false; break;
            case "input-failure": platform.InputSucceeds = false; break;
        }
        Assert.Equal(expected, await new PasteService(platform).ExecuteAsync("test", scenario == "no-target" ? null : Target, true));
        Assert.Equal(scenario == "input-failure" ? 1 : 0, platform.Sends);
        Assert.InRange(platform.Delays, 0, 28);
        if (scenario != "clipboard") Assert.Equal("test", platform.CopiedText);
    }
    [Fact]
    public async Task ShortcutIsAllowedToReleaseNaturallyBeforeActivation()
    {
        var platform = new FakePastePlatform { HasHeldModifiers = true };
        platform.OnDelay = () => { platform.HasHeldModifiers = false; return Task.CompletedTask; };
        Assert.Equal(PasteResult.InputSent, await new PasteService(platform).ExecuteAsync("test", Target, true));
        Assert.Equal(3, platform.Delays);
    }
    [Theory]
    [InlineData("closed", PasteResult.TargetGone)]
    [InlineData("third-app", PasteResult.FocusDenied)]
    [InlineData("clipboard", PasteResult.InputBlocked)]
    [InlineData("modifier", PasteResult.InputBlocked)]
    [InlineData("cancelled", PasteResult.InputBlocked)]
    public async Task ChangesDuringFocusSettleFailClosed(string scenario, PasteResult expected)
    {
        var platform = new FakePastePlatform();
        using var cts = new CancellationTokenSource();
        platform.OnDelay = () =>
        {
            switch (scenario)
            {
                case "closed": platform.TargetValid = false; break;
                case "third-app": platform.ForegroundWindow = (nint)99; platform.Guard.WasInterrupted = true; break;
                case "clipboard": platform.IsClipboardCurrent = false; break;
                case "modifier": platform.HasHeldModifiers = true; break;
                case "cancelled": cts.Cancel(); break;
            }
            return Task.CompletedTask;
        };
        Assert.Equal(expected, await new PasteService(platform).ExecuteAsync("test", Target, true, cts.Token));
        Assert.Equal(0, platform.Sends);
        Assert.Equal("test", platform.CopiedText);
        Assert.True(platform.Guard.Disposed);
    }
    [Fact]
    public async Task ActivationAcceptedButWrongWindowNeverReceivesInputAndWaitIsBounded()
    {
        var platform = new FakePastePlatform { ActivationSetsForeground = false };
        Assert.Equal(PasteResult.FocusDenied, await new PasteService(platform).ExecuteAsync("test", Target, true));
        Assert.Equal(16, platform.Delays); Assert.Equal(0, platform.Sends);
    }
    [Fact]
    public async Task ActivationReturnFalseCanProceedOnlyIfExactTargetIsAlreadyForeground()
    {
        var platform = new FakePastePlatform { ActivationSucceeds = false, ForegroundWindow = Target.WindowHandle };
        Assert.Equal(PasteResult.InputSent, await new PasteService(platform).ExecuteAsync("test", Target, true));
    }
    [Fact]
    public async Task FinalAdapterGateCanRejectWithoutRetryingOrReportingSuccess()
    {
        var platform = new FakePastePlatform { InputSucceeds = false };
        Assert.Equal(PasteResult.InputBlocked, await new PasteService(platform).ExecuteAsync("test", Target, true));
        Assert.Equal(1, platform.Sends);
    }
    [Fact]
    public async Task PartialInputIsUncertainAndIsNeverAutomaticallyRetried()
    {
        var platform = new FakePastePlatform { InputUncertain = true };
        Assert.Equal(PasteResult.InputUncertain, await new PasteService(platform).ExecuteAsync("test", Target, true));
        Assert.Equal(1, platform.Sends);
    }
    [Fact]
    public async Task IncompleteNativeKeyReleasePropagatesAsExplicitUncertainty()
    {
        var platform = new FakePastePlatform { KeyReleaseUncertain = true };
        Assert.Equal(PasteResult.KeyReleaseUncertain, await new PasteService(platform).ExecuteAsync("test", Target, true));
        Assert.Equal(1, platform.Sends);
    }
    [Fact]
    public async Task RapidSecondOperationCannotOverwriteFirstClipboardOrOverlapInput()
    {
        var platform = new FakePastePlatform();
        var started = TestData.Signal(); var release = TestData.Signal();
        platform.OnCopy = async () => { started.TrySetResult(true); await release.Task; };
        var paste = new PasteService(platform);
        var first = paste.ExecuteAsync("first", Target, true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(PasteResult.InputBlocked, await paste.ExecuteAsync("second", Target, true));
        release.TrySetResult(true);
        Assert.Equal(PasteResult.InputSent, await first);
        Assert.Equal("first", platform.CopiedText);
        Assert.Single(platform.Events, item => item == "copy");
    }
}

internal sealed class FakePastePlatform : IPastePlatform
{
    public List<string> Events { get; } = new();
    public FakeGuard Guard { get; } = new();
    public bool CopySucceeds { get; set; } = true;
    public bool TargetValid { get; set; } = true;
    public bool InputPermitted { get; set; } = true;
    public bool ActivationSucceeds { get; set; } = true;
    public bool ActivationSetsForeground { get; set; } = true;
    public bool InputSucceeds { get; set; } = true;
    public bool InputUncertain { get; set; }
    public bool KeyReleaseUncertain { get; set; }
    public bool IsHarmonyForeground { get; set; } = true;
    public bool HasHeldModifiers { get; set; }
    public bool IsClipboardCurrent { get; set; } = true;
    public nint ForegroundWindow { get; set; } = (nint)7;
    public Func<Task>? OnCopy { get; set; }
    public Func<Task>? OnDelay { get; set; }
    public string? CopiedText { get; private set; }
    public int Sends { get; private set; }
    public int Delays { get; private set; }
    public int Hides { get; private set; }
    public int Activations { get; private set; }
    public async Task<bool> CopyAsync(string text, CancellationToken cancellationToken)
    { Events.Add("copy"); if (OnCopy is not null) await OnCopy(); if (CopySucceeds) CopiedText = text; return CopySucceeds; }
    public bool IsTargetValid(PasteTarget target) => TargetValid;
    public bool CanSendInput(PasteTarget target) => InputPermitted;
    public IPasteGuard BeginGuard(PasteTarget target) => Guard;
    public void HideHarmony() { Hides++; Events.Add("hide"); }
    public bool TryActivate(PasteTarget target)
    { Activations++; Events.Add("activate"); if (ActivationSucceeds && ActivationSetsForeground) ForegroundWindow = target.WindowHandle; return ActivationSucceeds; }
    public PasteInputResult TrySendPaste(PasteTarget target, IPasteGuard guard)
    {
        Sends++; Events.Add("send");
        if (KeyReleaseUncertain) return PasteInputResult.KeyReleaseUncertain;
        if (InputUncertain) return PasteInputResult.Uncertain;
        return InputSucceeds && !guard.WasInterrupted && TargetValid && IsClipboardCurrent && !HasHeldModifiers && ForegroundWindow == target.WindowHandle
            ? PasteInputResult.Sent : PasteInputResult.Blocked;
    }
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    { Delays++; if (OnDelay is not null) await OnDelay(); cancellationToken.ThrowIfCancellationRequested(); }
    internal sealed class FakeGuard : IPasteGuard
    {
        public bool WasInterrupted { get; set; }
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}
