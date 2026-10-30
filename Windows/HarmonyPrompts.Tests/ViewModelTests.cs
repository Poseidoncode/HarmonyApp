using System.Text.Json;
using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Core.Storage;
using HarmonyPrompts.Windows.Services;
using HarmonyPrompts.Windows.ViewModels;

namespace HarmonyPrompts.Tests;

public sealed class ViewModelTests
{
    private static MainViewModel Create(FakePastePlatform? platform = null, FakeInteraction? interaction = null) => new(
        new TemplateRepository(Path.Combine(Path.GetTempPath(), "harmony-viewmodel", "templates.json"), "not-used"),
        new PasteService(platform ?? new FakePastePlatform()), interaction ?? new FakeInteraction());

    [Fact]
    public void FirstTemplateIsSelectedAndTypingImmediatelyUpdatesPreviewAndStats()
    {
        var vm = Create();
        vm.ApplyTemplates(new[] { TestData.Template("A"), TestData.Template("B") });
        Assert.Equal("A", vm.SelectedTemplate!.Id);
        Assert.Equal("Hello initial {{unknown}}", vm.Preview);
        vm.Fields[0].Value = "你好 👨‍👩‍👧‍👦";
        Assert.Equal("Hello 你好 👨‍👩‍👧‍👦 {{unknown}}", vm.Preview);
        Assert.Contains("4 words", vm.StatsLabel);
        Assert.True(vm.CopyCommand.CanExecute(null));
    }
    [Fact]
    public void UnchangedReloadAndOtherTemplateChangesDoNotResetTypedValues()
    {
        var vm = Create();
        var a = TestData.Template("A");
        vm.ApplyTemplates(new[] { a, TestData.Template("B") });
        vm.Fields[0].Value = "typed";
        var oldField = vm.Fields[0];
        var clone = TemplateJson.Deserialize(TemplateJson.Serialize(new[] { a }))[0];
        vm.ApplyTemplates(new[] { clone, TestData.Template("B", "new") });
        Assert.Same(oldField, vm.Fields[0]);
        Assert.Equal("typed", vm.Fields[0].Value);
        Assert.Same(clone, vm.SelectedTemplate);
    }
    [Fact]
    public void SelectedTemplateContentChangeAndActualSwitchResetToNewInitialValues()
    {
        var vm = Create();
        var a = TestData.Template("A"); var b = TestData.Template("B", "B-value");
        vm.ApplyTemplates(new[] { a, b });
        vm.Fields[0].Value = "typed";
        vm.ApplyTemplates(new[] { TestData.Template("A", "updated"), b });
        Assert.Equal("updated", vm.Fields[0].Value);
        vm.SelectedTemplate = b;
        Assert.Equal("B-value", vm.Fields[0].Value);
        vm.SelectedTemplate = a;
        Assert.Equal("initial", vm.Fields[0].Value);
    }
    [Fact]
    public void SearchAndWpfTransientNullSelectionDoNotWipeActiveForm()
    {
        var vm = Create(); vm.ApplyTemplates(new[] { TestData.Template("Alpha"), TestData.Template("Beta") });
        vm.Fields[0].Value = "typed";
        vm.Search = "BETA";
        Assert.Equal("Beta", Assert.Single(vm.FilteredTemplates).Id);
        Assert.Null(vm.VisibleSelection);
        vm.VisibleSelection = null;
        vm.SelectedTemplate = null;
        Assert.Equal("Alpha", vm.SelectedTemplate!.Id); Assert.Equal("typed", vm.Fields[0].Value);
        vm.Search = "description";
        Assert.Equal(2, vm.FilteredTemplates.Count);
        Assert.Same(vm.SelectedTemplate, vm.VisibleSelection);
        Assert.Contains("2 of 2", vm.TemplateCount);
    }
    [Fact]
    public void EmptyCollectionClearsStalePreviewAndDisablesCopyAndPaste()
    {
        var vm = Create(); vm.EnableAutoPaste = true; vm.ApplyTemplates(new[] { TestData.Template("A") });
        vm.ApplyTemplates(Array.Empty<PromptTemplate>());
        Assert.Null(vm.SelectedTemplate); Assert.Empty(vm.Fields); Assert.Empty(vm.Preview);
        Assert.False(vm.CopyCommand.CanExecute(null)); Assert.False(vm.PasteCommand.CanExecute(null));
    }
    [Fact]
    public void FiveFieldTypesAndCheckboxCustomValuesAndRadioBindingsMatchModels()
    {
        foreach (var type in Enum.GetValues<FieldType>())
        {
            var field = new PromptField { Name = "x", Label = "X", Type = type, Options = new[] { "B", "A" },
                DefaultValue = type == FieldType.Checkbox ? "TRUE" : "A", CheckedValue = "on", UncheckedValue = "off" };
            var vm = new FieldViewModel(field);
            Assert.Equal(field.InitialValue(), vm.Value); Assert.Equal(type, vm.Type);
            if (type == FieldType.Checkbox)
            { Assert.True(vm.IsChecked); vm.IsChecked = false; Assert.Equal("off", vm.Value); vm.IsChecked = true; Assert.Equal("on", vm.Value); }
            if (type == FieldType.Radio)
            { Assert.True(vm.RadioOptions[1].IsSelected); vm.RadioOptions[0].IsSelected = true; Assert.Equal("B", vm.Value); Assert.False(vm.RadioOptions[1].IsSelected); }
        }
    }
    [Fact]
    public void RadioGroupsAreDistinctAndCheckboxBooleanDoesNotDependOnEqualCustomStrings()
    {
        var field = new PromptField { Name = "x", Label = "X", Type = FieldType.Checkbox, CheckedValue = "same", UncheckedValue = "same" };
        var vm = new FieldViewModel(field);
        Assert.False(vm.IsChecked); vm.IsChecked = true; Assert.True(vm.IsChecked); Assert.Equal("same", vm.Value);
        Assert.NotEqual(vm.GroupId, new FieldViewModel(field).GroupId);
    }
    [Fact]
    public async Task AutoPasteIsOffByDefaultAndRequiresExplicitSessionOptIn()
    {
        var platform = new FakePastePlatform();
        var vm = Create(platform, new FakeInteraction { PasteTarget = PasteServiceTests.Target });
        vm.ApplyTemplates(new[] { TestData.Template("A") });
        Assert.False(vm.EnableAutoPaste); Assert.False(vm.PasteCommand.CanExecute(null));
        Assert.True(vm.CopyCommand.CanExecute(null));
        await vm.PasteCommand.ExecuteAsync();
        Assert.Empty(platform.Events);
        new SettingsViewModel(vm).EnableAutoPaste = true;
        Assert.True(vm.PasteCommand.CanExecute(null));
        await vm.PasteCommand.ExecuteAsync();
        Assert.Equal(1, platform.Sends);
        vm.EnableAutoPaste = false;
        Assert.False(vm.PasteCommand.CanExecute(null));
        Assert.False(Create().EnableAutoPaste); // Not persisted to a new session.
    }
    [Fact]
    public async Task CopyReportsCopiedAndPasteDoesNotClaimConfirmedDelivery()
    {
        var platform = new FakePastePlatform(); var interaction = new FakeInteraction { PasteTarget = PasteServiceTests.Target };
        var vm = Create(platform, interaction); vm.EnableAutoPaste = true; vm.ApplyTemplates(new[] { TestData.Template("A") });
        await vm.CopyCommand.ExecuteAsync();
        Assert.Equal("Copied to clipboard.", vm.Message);
        await vm.PasteCommand.ExecuteAsync();
        Assert.Contains("input sent", vm.Message); Assert.Contains("delivery is not confirmed", vm.Message);
        Assert.Equal(vm.Preview, platform.CopiedText);
        Assert.Single(interaction.Notifications); Assert.False(interaction.Notifications[0].Warning);
    }
    [Fact]
    public async Task FailedKeyReleaseWarnsBeforeAnyManualPasteOrRetry()
    {
        var platform = new FakePastePlatform { KeyReleaseUncertain = true };
        var interaction = new FakeInteraction { PasteTarget = PasteServiceTests.Target };
        var vm = Create(platform, interaction); vm.EnableAutoPaste = true; vm.ApplyTemplates(new[] { TestData.Template("A") });
        await vm.PasteCommand.ExecuteAsync();
        Assert.Contains("Ctrl or V may remain down", vm.Message);
        Assert.Contains("press and release", vm.Message);
        Assert.Contains("Inspect the target", vm.Message);
        Assert.True(Assert.Single(interaction.Notifications).Warning);
        Assert.Equal(1, platform.Sends);
    }
    [Fact]
    public async Task NoTargetFallbackInstructsManualPasteWithoutInput()
    {
        var platform = new FakePastePlatform(); var vm = Create(platform); vm.EnableAutoPaste = true;
        vm.ApplyTemplates(new[] { TestData.Template("A") });
        await vm.PasteCommand.ExecuteAsync();
        Assert.Contains("Ctrl+V", vm.Message); Assert.Contains("no target", vm.Message); Assert.Equal(0, platform.Sends);
    }
    [Fact]
    public async Task BusyCommandsPreventOverlappingActionsAndFreezeText()
    {
        var platform = new FakePastePlatform(); var started = TestData.Signal(); var release = TestData.Signal();
        platform.OnCopy = async () => { started.TrySetResult(true); await release.Task; };
        var vm = Create(platform); vm.EnableAutoPaste = true; vm.ApplyTemplates(new[] { TestData.Template("A") });
        var original = vm.Preview; var copy = vm.CopyCommand.ExecuteAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(vm.IsBusy); Assert.False(vm.ImportCommand.CanExecute(null)); Assert.False(vm.PasteCommand.CanExecute(null));
        vm.Fields[0].Value = "changed while copying";
        release.TrySetResult(true); await copy;
        Assert.Equal(original, platform.CopiedText); Assert.False(vm.IsBusy);
    }
    [Fact]
    public void TextSizeIsClampedAndErrorMessagesDoNotEchoPromptOrExceptionContent()
    {
        var vm = Create(); vm.TextSize = 200; Assert.Equal(22, vm.TextSize); vm.TextSize = 1; Assert.Equal(12, vm.TextSize);
        const string sensitive = "TEST-PROMPT-NEVER-ECHO";
        Assert.DoesNotContain(sensitive, MainViewModel.Describe(new IOException(sensitive)));
        Assert.DoesNotContain(sensitive, MainViewModel.Describe(new JsonException(sensitive)));
        Assert.DoesNotContain(sensitive, MainViewModel.Describe(new InvalidOperationException(sensitive)));
    }

    private sealed class FakeInteraction : IUserInteraction
    {
        public PasteTarget? PasteTarget { get; set; }
        public List<(string Message, bool Warning)> Notifications { get; } = new();
        public string? PickImportPath() => null;
        public void OpenTemplatesFolder() { }
        public void ShowSettings() { }
        public void NotifyPasteResult(string message, bool warning) => Notifications.Add((message, warning));
    }
}
