using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyPrompts.Core.Models;

namespace HarmonyPrompts.Windows.ViewModels;

public sealed class FieldViewModel : ObservableObject
{
    private string _value;
    private bool _isChecked;
    public PromptField Field { get; }
    public string Name => Field.Name;
    public string Label => Field.Label;
    public string Help => Field.Placeholder ?? "";
    public FieldType Type => Field.Type;
    public string GroupId { get; } = Guid.NewGuid().ToString("N");
    public IReadOnlyList<string> Options => Field.Options ?? Array.Empty<string>();
    public IReadOnlyList<RadioOptionViewModel> RadioOptions { get; }
    public event EventHandler? ValueChanged;

    public FieldViewModel(PromptField field)
    {
        Field = field;
        _value = field.InitialValue();
        _isChecked = string.Equals(field.DefaultValue, "true", StringComparison.OrdinalIgnoreCase);
        RadioOptions = Options.Select(option => new RadioOptionViewModel(this, option)).ToArray();
    }

    public string Value
    {
        get => _value;
        set
        {
            if (!Set(ref _value, value ?? "")) return;
            foreach (var option in RadioOptions) option.NotifySelection();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (!Set(ref _isChecked, value)) return;
            Value = value ? Field.CheckedValue ?? "true" : Field.UncheckedValue ?? "";
        }
    }
}

public sealed class RadioOptionViewModel(FieldViewModel owner, string text) : ObservableObject
{
    public string Text => text;
    public string GroupId => owner.GroupId;
    public bool IsSelected
    {
        get => owner.Value == text;
        set { if (value) owner.Value = text; }
    }
    internal void NotifySelection() => Raise(nameof(IsSelected));
}
