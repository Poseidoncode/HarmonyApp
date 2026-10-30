using System.Text.Json;
using System.Text.Json.Serialization;

namespace HarmonyPrompts.Core.Models;

[JsonConverter(typeof(FieldTypeConverter))]
public enum FieldType { Text, Textarea, Select, Radio, Checkbox }

public sealed class FieldTypeConverter : JsonConverter<FieldType>
{
    public override FieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? reader.GetString() switch
        {
            "text" => FieldType.Text,
            "textarea" => FieldType.Textarea,
            "select" => FieldType.Select,
            "radio" => FieldType.Radio,
            "checkbox" => FieldType.Checkbox,
            _ => throw new JsonException("Unknown field type.")
        } : throw new JsonException("Field type must be a lowercase string.");

    public override void Write(Utf8JsonWriter writer, FieldType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            FieldType.Text => "text",
            FieldType.Textarea => "textarea",
            FieldType.Select => "select",
            FieldType.Radio => "radio",
            FieldType.Checkbox => "checkbox",
            _ => throw new JsonException("Unknown field type.")
        });
}
