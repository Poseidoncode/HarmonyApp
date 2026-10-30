using System.Globalization;
using System.Text;

namespace HarmonyPrompts.Core.Services;

public readonly record struct PromptMeasurement(int Chars, int Words);

public static class PromptStats
{
    public static PromptMeasurement Measure(string text)
    {
        var elements = StringInfo.GetTextElementEnumerator(text);
        var chars = 0;
        var words = 0;
        var inWord = false;
        while (elements.MoveNext())
        {
            chars++;
            var element = elements.GetTextElement();
            // Swift Character.isWhitespace checks _firstScalar.properties.isWhitespace.
            var firstScalar = Rune.GetRuneAt(element, 0);
            if (Rune.IsWhiteSpace(firstScalar)) inWord = false;
            else if (!inWord) { words++; inWord = true; }
        }
        return new(chars, words);
    }
}
