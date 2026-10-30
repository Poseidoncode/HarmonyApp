using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Core.Storage;
using HarmonyPrompts.Windows.Services;

namespace HarmonyPrompts.Windows.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly TemplateRepository _repository;
    private readonly PasteService _paste;
    private readonly IUserInteraction _interaction;
    private IReadOnlyList<PromptTemplate> _templates = Array.Empty<PromptTemplate>();
    private PromptTemplate? _selected;
    private PromptTemplate? _visibleSelected;
    private IReadOnlyList<FieldViewModel> _fields = Array.Empty<FieldViewModel>();
    private string _search = "";
    private string _preview = "";
    private string _message = "Choose a template. All data stays on this computer.";
    private bool _busy;
    private bool _enableAutoPaste;
    private double _textSize = 14;
    private PromptMeasurement _stats;
    public AsyncCommand ImportCommand { get; }
    public AsyncCommand ReloadCommand { get; }
    public AsyncCommand CopyCommand { get; }
    public AsyncCommand PasteCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public string TemplatesPath => _repository.TemplatesPath;
    public bool IsBusy { get => _busy; private set { if (Set(ref _busy, value)) RefreshCommands(); } }
    public string Message { get => _message; private set => Set(ref _message, value); }
    public string Preview { get => _preview; private set => Set(ref _preview, value); }
    public string StatsLabel => $"{_stats.Chars:N0} characters · {_stats.Words:N0} words";
    public string TemplateCount => $"{FilteredTemplates.Count} of {_templates.Count} templates";
    public bool HasTemplate => _selected is not null;
    // Fail closed until the real-application acceptance matrix is signed off.
    // Session-only opt-in; no stored preference can silently enable input at startup.
    public bool EnableAutoPaste
    {
        get => _enableAutoPaste;
        set
        {
            if (!Set(ref _enableAutoPaste, value)) return;
            Raise(nameof(AutoPasteNotice));
            PasteCommand.Refresh();
        }
    }
    public string AutoPasteNotice => EnableAutoPaste
        ? "EXPERIMENTAL Auto Paste is on for this session. Best-effort focus checks cannot guarantee the destination. Use Copy + manual Ctrl+V for sensitive text."
        : "Auto Paste is OFF pending safety acceptance. Use Copy + manual Ctrl+V. Experimental session-only opt-in is available in Settings.";
    public IReadOnlyList<FieldViewModel> Fields => _fields;
    public string TargetLabel => _interaction.PasteTarget is { } target
        ? $"Auto Paste target: {target.DisplayName} (PID {target.ProcessId})"
        : "No Auto Paste target — focus an input in another app, then reopen Harmony.";
    public double TextSize { get => _textSize; set => Set(ref _textSize, Math.Clamp(value, 12, 22)); }

    public string Search
    {
        get => _search;
        set
        {
            if (!Set(ref _search, value)) return;
            Raise(nameof(FilteredTemplates));
            Raise(nameof(TemplateCount));
            RestoreVisibleSelection();
        }
    }
    public IReadOnlyList<PromptTemplate> FilteredTemplates => _templates.Where(template =>
        template.Name.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
        template.Description.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToArray();

    // List selection may be null while search hides the retained active form.
    public PromptTemplate? VisibleSelection
    {
        get => _visibleSelected;
        set { if (Set(ref _visibleSelected, value) && value is not null) Select(value); }
    }

    public PromptTemplate? SelectedTemplate
    {
        get => _selected;
        set
        {
            // WPF temporarily clears selection when filtering/replacing ItemsSource.
            // That must not wipe the active form or any typed values.
            if (value is not null) Select(value);
        }
    }

    public MainViewModel(TemplateRepository repository, PasteService paste, IUserInteraction interaction)
    {
        _repository = repository;
        _paste = paste;
        _interaction = interaction;
        ImportCommand = new AsyncCommand(() => RunAsync(async () =>
        {
            var path = _interaction.PickImportPath();
            if (path is null) return;
            var result = await _repository.ImportAsync(path);
            ApplyTemplates(_repository.Current);
            Message = result.Outcome == RepositoryOutcome.Applied
                ? $"Imported {_templates.Count} template(s). Previous collection replaced and saved."
                : "A newer import superseded this request.";
        }), () => !IsBusy);
        ReloadCommand = new AsyncCommand(LoadAsync, () => !IsBusy);
        CopyCommand = new AsyncCommand(() => CopyAsync(false), () => !IsBusy && HasTemplate);
        PasteCommand = new AsyncCommand(() => CopyAsync(true), () => !IsBusy && HasTemplate && EnableAutoPaste);
        OpenFolderCommand = new RelayCommand(() => RunAction(_interaction.OpenTemplatesFolder), () => !IsBusy);
        SettingsCommand = new RelayCommand(() => RunAction(_interaction.ShowSettings), () => !IsBusy);
    }

    public Task LoadAsync() => RunAsync(async () =>
    {
        await _repository.LoadAsync();
        ApplyTemplates(_repository.Current);
        Message = $"Loaded {_templates.Count} template(s). Import JSON to replace the collection.";
    });

    public void RefreshTarget() => Raise(nameof(TargetLabel));

    public void ApplyTemplates(IReadOnlyList<PromptTemplate> templates)
    {
        var next = templates.FirstOrDefault(template => template.Id == _selected?.Id) ?? templates.FirstOrDefault();
        _templates = templates;
        Select(next);
        Raise(nameof(FilteredTemplates));
        Raise(nameof(TemplateCount));
        Raise(nameof(SelectedTemplate));
        RestoreVisibleSelection();
    }

    private void RestoreVisibleSelection()
    {
        var visible = FilteredTemplates.FirstOrDefault(template => ReferenceEquals(template, _selected));
        if (ReferenceEquals(visible, _visibleSelected)) return;
        _visibleSelected = visible;
        Raise(nameof(VisibleSelection));
    }

    private void Select(PromptTemplate? template)
    {
        if (TemplateJson.Equivalent(_selected, template))
        {
            _selected = template;
            Raise(nameof(SelectedTemplate));
            RestoreVisibleSelection();
            return;
        }
        foreach (var field in _fields) field.ValueChanged -= OnFieldChanged;
        _selected = template;
        _fields = template?.Fields.Select(field => new FieldViewModel(field)).ToArray() ?? Array.Empty<FieldViewModel>();
        foreach (var field in _fields) field.ValueChanged += OnFieldChanged;
        Raise(nameof(SelectedTemplate));
        Raise(nameof(Fields));
        Raise(nameof(HasTemplate));
        UpdatePreview();
        RefreshCommands();
        RestoreVisibleSelection();
    }

    private void OnFieldChanged(object? sender, EventArgs args) => UpdatePreview();
    private void UpdatePreview()
    {
        Preview = _selected is null ? "" : PromptRenderer.Render(_selected.Template,
            _fields.ToDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal));
        _stats = PromptStats.Measure(Preview);
        Raise(nameof(StatsLabel));
    }

    private Task CopyAsync(bool paste) => RunAsync(async () =>
    {
        // Freeze both text and target on the explicit user action, not after retry/delay.
        var target = _interaction.PasteTarget;
        var result = await _paste.ExecuteAsync(Preview, target, paste);
        Message = result switch
        {
            PasteResult.CopiedOnly => "Copied to clipboard.",
            PasteResult.InputSent => $"Ctrl+V input sent to {target?.DisplayName}. Check the target field; delivery is not confirmed.",
            PasteResult.InputUncertain => "Some input may already have reached the target. Inspect it before retrying to avoid a duplicate paste.",
            PasteResult.KeyReleaseUncertain => "Windows did not confirm key release. Ctrl or V may remain down: press and release Ctrl and V to reset their state before continuing. Inspect the target before retrying; no automatic retry was made.",
            PasteResult.NoTarget => "Copied — no target. Click your intended input, then paste manually with Ctrl+V.",
            PasteResult.TargetGone => "Copied — target closed or changed identity. Paste manually with Ctrl+V.",
            PasteResult.FocusDenied => "Copied — focus could not be confirmed or changed. Paste manually with Ctrl+V.",
            PasteResult.InputBlocked => "Automatic input unavailable. Use Copy, then paste manually with Ctrl+V.",
            _ => "Clipboard unavailable. Nothing was sent. Close the app holding the clipboard, then retry Copy."
        };
        if (paste) _interaction.NotifyPasteResult(Message, result != PasteResult.InputSent);
    });

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(); }
        catch (Exception error) { Message = Describe(error); }
        finally { IsBusy = false; }
    }
    private void RunAction(Action action)
    {
        try { action(); }
        catch (Exception error) { Message = Describe(error); }
    }
    public static string Describe(Exception error) => error switch
    {
        TemplateValidationException validation => validation.Message,
        JsonException json => $"Invalid template JSON (line {(json.LineNumber ?? 0) + 1}, byte {json.BytePositionInLine ?? 0}). Check required properties and lowercase field types. Existing data was preserved.",
        UnauthorizedAccessException => "Access denied. Existing data was preserved. Choose a readable JSON file and check the templates folder permissions.",
        IOException => "File access failed or the file is in use. Existing data was preserved. Close other editors and retry.",
        OperationCanceledException => "Operation cancelled. Existing data was preserved.",
        _ => "The operation could not be completed. Existing data was preserved; retry or reopen Harmony."
    };
    private void RefreshCommands()
    {
        ImportCommand?.Refresh(); ReloadCommand?.Refresh(); CopyCommand?.Refresh(); PasteCommand?.Refresh();
        OpenFolderCommand?.Refresh(); SettingsCommand?.Refresh();
    }
}
