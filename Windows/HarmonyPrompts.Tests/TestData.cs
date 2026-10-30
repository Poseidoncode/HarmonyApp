using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Core.Services;

namespace HarmonyPrompts.Tests;

internal static class TestData
{
    public static PromptTemplate Template(string id, string initial = "initial") => new()
    {
        Id = id, Name = id, Description = "Description " + id, Template = "Hello {{x}} {{unknown}}",
        Fields = new[] { new PromptField { Name = "x", Label = "X", Type = FieldType.Text, DefaultValue = initial } }
    };
    public static byte[] Bytes(string id) => TemplateJson.Serialize(new[] { Template(id) });
    public static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
