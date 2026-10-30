using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HarmonyPrompts.Core.Models;

namespace HarmonyPrompts.Core.Services;

public static class TemplateJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    public static IReadOnlyList<PromptTemplate> Deserialize(ReadOnlySpan<byte> data, bool requireNonEmpty = false)
    {
        // UTF-8 BOM is commonly emitted by Windows text editors.
        if (data.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) data = data[3..];
        var templates = JsonSerializer.Deserialize<List<PromptTemplate>>(data, Options)
            ?? throw new TemplateValidationException("Expected a JSON array of templates, not null.");
        Validate(templates, requireNonEmpty);
        return templates.AsReadOnly();
    }

    public static byte[] Serialize(IReadOnlyList<PromptTemplate> templates)
    {
        Validate(templates);
        return JsonSerializer.SerializeToUtf8Bytes(templates, Options);
    }

    public static void Validate(IReadOnlyList<PromptTemplate> templates, bool requireNonEmpty = false)
    {
        if (requireNonEmpty && templates.Count == 0)
            throw new TemplateValidationException("Imported file contains no templates.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var template in templates)
        {
            if (template is null || template.Id is null || template.Name is null ||
                template.Description is null || template.Template is null || template.Fields is null)
                throw new TemplateValidationException("Every template needs non-null id, name, description, template and fields.");
            if (!ids.Add(template.Id))
                throw new TemplateValidationException("Duplicate template IDs are ambiguous. Give each template a unique ID.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in template.Fields)
            {
                if (field is null || field.Name is null || field.Label is null || !Enum.IsDefined(field.Type) ||
                    (field.Options is not null && field.Options.Any(option => option is null)))
                    throw new TemplateValidationException("Every field needs non-null name, label, a supported type and string options.");
                if (!names.Add(field.Name))
                    throw new TemplateValidationException("A template contains duplicate field names. Each field name must be unique.");
            }
        }
    }

    // Content equality (records alone compare list references, not field contents).
    public static bool Equivalent(PromptTemplate? left, PromptTemplate? right) =>
        ReferenceEquals(left, right) || (left is not null && right is not null &&
        JsonNode.DeepEquals(JsonSerializer.SerializeToNode(left, Options), JsonSerializer.SerializeToNode(right, Options)));
}
