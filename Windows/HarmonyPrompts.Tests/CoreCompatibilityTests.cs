using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarmonyPrompts.Core.Models;
using HarmonyPrompts.Core.Services;

namespace HarmonyPrompts.Tests;

public sealed class CoreCompatibilityTests
{
    private static JsonElement[] Cases(string file) => JsonSerializer.Deserialize<JsonElement[]>(
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)))!;

    public static IEnumerable<object[]> RendererCases() => Cases("renderer-cases.json").Select(item => new object[]
        { item.GetProperty("name").GetString()!, item.GetProperty("template").GetString()!,
          item.GetProperty("values").Deserialize<Dictionary<string, string>>()!, item.GetProperty("expected").GetString()! });
    [Theory, MemberData(nameof(RendererCases))]
    public void RendererMatchesSwiftRules(string name, string template, Dictionary<string, string> values, string expected)
    {
        Assert.NotEmpty(name);
        Assert.Equal(expected, PromptRenderer.Render(template, values));
    }

    public static IEnumerable<object[]> FieldCases() => Cases("field-default-cases.json").Select(item => new object[]
        { item.GetProperty("name").GetString()!, item.GetProperty("field").GetRawText(), item.GetProperty("expected").GetString()! });
    [Theory, MemberData(nameof(FieldCases))]
    public void InitialValueMatchesSwiftRules(string name, string json, string expected)
    {
        Assert.NotEmpty(name);
        var field = JsonSerializer.Deserialize<PromptField>(json, TemplateJson.Options)!;
        Assert.Equal(expected, field.InitialValue());
        Assert.Equal(field.Name, field.Id);
    }

    public static IEnumerable<object[]> StatsCases() => Cases("stats-cases.json").Select(item => new object[]
        { item.GetProperty("name").GetString()!, item.GetProperty("text").GetString()!,
          item.GetProperty("chars").GetInt32(), item.GetProperty("words").GetInt32() });
    [Theory, MemberData(nameof(StatsCases))]
    public void GraphemeAndWhitespaceCountsMatchSourceDerivedFixtures(string name, string text, int chars, int words)
    {
        Assert.NotEmpty(name);
        Assert.Equal(new PromptMeasurement(chars, words), PromptStats.Measure(text));
    }

    [Fact]
    public void SharedDefaultsAreByteIdenticalToUntouchedMacResource()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        Assert.Equal(File.ReadAllBytes(Path.Combine(folder, "macos-templates.json")), File.ReadAllBytes(Path.Combine(folder, "templates.json")));
    }

    [Fact]
    public void BuiltInJsonRoundtripAndInitialOutputsRetainAllData()
    {
        var data = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "templates.json"));
        var templates = TemplateJson.Deserialize(data);
        Assert.Equal(new[] { "explain-code", "find-bugs", "generate-tests", "refactor-code", "add-docs" }, templates.Select(template => template.Id));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(data), JsonNode.Parse(TemplateJson.Serialize(templates))));
        var expected = new[] { new PromptMeasurement(114, 17), new(131, 21), new(132, 20), new(94, 13), new(57, 9) };
        for (var i = 0; i < templates.Count; i++)
            Assert.Equal(expected[i], PromptStats.Measure(PromptRenderer.Render(templates[i].Template, templates[i].MakeInitialValues())));
    }

    [Fact]
    public void OptionalNullsEmptyStringsAndUnknownPropertiesAreRetainedSemantically()
    {
        const string json = """[{"id":"","name":"","description":"","template":"你好 {{x}}","fields":[{"name":"x","label":"X","type":"text","defaultValue":"","placeholder":null,"options":[],"extension":{"b":2,"a":1}}],"future":[1,"✅"]}]""";
        var templates = TemplateJson.Deserialize(Encoding.UTF8.GetBytes(json));
        var after = TemplateJson.Deserialize(TemplateJson.Serialize(templates));
        Assert.True(TemplateJson.Equivalent(templates[0], after[0]));
        Assert.Equal("", after[0].Fields[0].InitialValue());
        Assert.Empty(after[0].Fields[0].Options!);
        Assert.Equal("✅", after[0].AdditionalProperties!["future"][1].GetString());
        Assert.Equal(2, after[0].Fields[0].AdditionalProperties!["extension"].GetProperty("b").GetInt32());
    }

    [Fact]
    public void Utf8BomAndEmptyLoadAreSupportedButEmptyImportIsRejected()
    {
        Assert.Empty(TemplateJson.Deserialize(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'[', (byte)']' }));
        Assert.Throws<TemplateValidationException>(() => TemplateJson.Deserialize("[]"u8, requireNonEmpty: true));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{\"id\":\"a\"}]")]
    [InlineData("[{\"id\":\"a\",\"name\":null,\"description\":\"\",\"template\":\"\",\"fields\":[]}]")]
    [InlineData("[{\"id\":\"a\",\"name\":\"\",\"description\":\"\",\"template\":\"\",\"fields\":[{\"name\":\"x\",\"label\":\"X\",\"type\":\"Text\"}]}]")]
    [InlineData("[{\"id\":\"a\",\"name\":\"\",\"description\":\"\",\"template\":\"\",\"fields\":[{\"name\":\"x\",\"label\":\"X\",\"type\":0}]}]")]
    [InlineData("[{\"id\":\"a\",\"name\":\"\",\"description\":\"\",\"template\":\"\",\"fields\":[{\"name\":\"x\",\"label\":\"X\",\"type\":\"select\",\"options\":[null]}]}]")]
    [InlineData("[invalid]")]
    public void MalformedOrIncompleteCollectionsAreRejected(string json) =>
        Assert.ThrowsAny<Exception>(() => TemplateJson.Deserialize(Encoding.UTF8.GetBytes(json), requireNonEmpty: true));

    [Fact]
    public void DuplicateNamesAndIdsAreExplicitlyRejectedRatherThanOverwritten()
    {
        var template = TestData.Template("a");
        Assert.Throws<TemplateValidationException>(() => TemplateJson.Serialize(new[] { template, template }));
        var duplicate = template with { Fields = new[] { template.Fields[0], template.Fields[0] } };
        Assert.Throws<TemplateValidationException>(() => TemplateJson.Serialize(new[] { duplicate }));
        Assert.Throws<TemplateValidationException>(() => duplicate.MakeInitialValues());
    }

    [Fact]
    public void EqualityIsByContentAndIgnoresJsonObjectPropertyOrder()
    {
        var a = TestData.Template("a") with { AdditionalProperties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("{\"z\":{\"a\":1,\"b\":2},\"y\":3}") };
        var b = a with { AdditionalProperties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("{\"y\":3,\"z\":{\"b\":2,\"a\":1}}") };
        Assert.True(TemplateJson.Equivalent(a, b));
        Assert.False(TemplateJson.Equivalent(a, b with { Template = "changed" }));
    }
}
