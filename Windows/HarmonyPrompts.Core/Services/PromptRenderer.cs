using System.Text.RegularExpressions;

namespace HarmonyPrompts.Core.Services;

public static partial class PromptRenderer
{
    [GeneratedRegex(@"\{\{([a-zA-Z0-9_-]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        // Force ordinal lookup even if a caller supplies a case-insensitive dictionary.
        var ordinalValues = new Dictionary<string, string>(values, StringComparer.Ordinal);
        return PlaceholderRegex().Replace(template, match =>
            ordinalValues.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    }
}
