using System.Text.Json;
using System.Text.Json.Serialization;

namespace HarmonyPrompts.Core.Models;

public sealed record PromptTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Template { get; init; }
    public required IReadOnlyList<PromptField> Fields { get; init; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    public Dictionary<string, string> MakeInitialValues()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in Fields)
            if (!values.TryAdd(field.Name, field.InitialValue()))
                throw new TemplateValidationException("A template contains duplicate field names. Each field name must be unique.");
        return values;
    }
}

public sealed class TemplateValidationException(string message) : Exception(message);
