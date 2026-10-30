using System.Text.Json;
using System.Text.Json.Serialization;

namespace HarmonyPrompts.Core.Models;

public sealed record PromptField
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public required FieldType Type { get; init; }
    public string? Placeholder { get; init; }
    public IReadOnlyList<string>? Options { get; init; }
    public string? DefaultValue { get; init; }
    public string? CheckedValue { get; init; }
    public string? UncheckedValue { get; init; }
    [JsonIgnore] public string Id => Name;
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    public string InitialValue() => Type switch
    {
        FieldType.Text or FieldType.Textarea => DefaultValue ?? Placeholder ?? "",
        FieldType.Select or FieldType.Radio => !string.IsNullOrEmpty(DefaultValue)
            ? DefaultValue : Options?.FirstOrDefault() ?? "",
        FieldType.Checkbox => string.Equals(DefaultValue, "true", StringComparison.OrdinalIgnoreCase)
            ? CheckedValue ?? "true" : UncheckedValue ?? "",
        _ => throw new InvalidOperationException("Unknown field type.")
    };
}
