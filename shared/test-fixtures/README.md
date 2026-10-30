# Compatibility fixtures

These cases are derived from the checked-in Swift implementations (`PromptField.initialValue`, `PromptRenderer.render`, `PromptStats.measure`) at commit `f024a0d7c8608f20b14689ccf7bc93da19a29632`. They are reviewable expected results, **not captured output from a Swift execution** on this Windows host. Every case includes inputs and expected output. The xUnit suite consumes them without WPF.

- Renderer: a single regex scan, ASCII/case-sensitive names, unknown literals, nonrecursive replacements, CRLF/Unicode, literal `$`/backslash.
- Defaults: all five field types, missing/null/empty distinctions, no sorting or trimming, custom checkbox values.
- Stats: extended grapheme clusters, CRLF, combining accents, Hangul, emoji ZWJ/skin tones/flags/keycaps, CJK, Unicode spaces, non-whitespace ZWSP/BOM, whitespace + combining-mark clusters.

Swift `Character.isWhitespace` uses the first scalar (`_firstScalar.properties.isWhitespace`): https://github.com/swiftlang/swift/blob/main/stdlib/public/core/CharacterProperties.swift . .NET uses `StringInfo` grapheme segmentation + `Rune.IsWhiteSpace` on that first scalar. Unicode data versions differ across OS/runtime versions, especially for newly introduced emoji and scripts. No claim of exhaustive equality for all future Unicode sequences; run these fixtures against Swift on macOS before certifying cross-runtime parity. Character count is not UTF-16 length; words are whitespace-separated runs, not linguistic token counts.

`shared/templates.json` is a byte copy of the existing Mac resource. Do not edit just one of the two copies: the parity test must remain green. The Mac source/resource/project and installer are intentionally unchanged.
