using System.Windows;
using System.Windows.Controls;
using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Windows.ViewModels;

namespace HarmonyPrompts.Windows.Views;

public sealed class FieldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Text { get; set; }
    public DataTemplate? Textarea { get; set; }
    public DataTemplate? Select { get; set; }
    public DataTemplate? Radio { get; set; }
    public DataTemplate? Checkbox { get; set; }
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => (item as FieldViewModel)?.Type switch
    {
        FieldType.Textarea => Textarea,
        FieldType.Select => Select,
        FieldType.Radio => Radio,
        FieldType.Checkbox => Checkbox,
        _ => Text
    };
}
