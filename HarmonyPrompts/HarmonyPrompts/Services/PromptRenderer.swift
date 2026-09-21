import Foundation

enum PromptRenderer {
    private static let regex = try! NSRegularExpression(pattern: #"\{\{([a-zA-Z0-9_-]+)\}\}"#)

    /// Single-pass substitution: one regex scan, one output allocation.
    /// Unknown keys are left in place, matching previous behavior.
    static func render(template: String, values: [String: String]) -> String {
        let nsTemplate = template as NSString
        let matches = regex.matches(
            in: template,
            range: NSRange(location: 0, length: nsTemplate.length)
        )
        guard !matches.isEmpty else { return template }

        var result = ""
        result.reserveCapacity(template.utf16.count)
        var cursor = 0
        for match in matches {
            guard match.numberOfRanges > 1 else { continue }
            let fullRange = match.range(at: 0)
            if fullRange.location > cursor {
                result += nsTemplate.substring(with: NSRange(location: cursor, length: fullRange.location - cursor))
            }
            let key = nsTemplate.substring(with: match.range(at: 1))
            if let value = values[key] {
                result += value
            } else {
                result += nsTemplate.substring(with: fullRange)
            }
            cursor = fullRange.location + fullRange.length
        }
        if cursor < nsTemplate.length {
            result += nsTemplate.substring(with: NSRange(location: cursor, length: nsTemplate.length - cursor))
        }
        return result
    }
}

/// Character/word counts in a single grapheme pass.
/// `chars` equals `string.count` and `words` equals
/// `string.split { $0.isWhitespace }.count`, without the temporary array.
enum PromptStats {
    static func measure(_ string: String) -> (chars: Int, words: Int) {
        var chars = 0
        var words = 0
        var inWord = false
        for character in string {
            chars += 1
            if character.isWhitespace {
                inWord = false
            } else if !inWord {
                words += 1
                inWord = true
            }
        }
        return (chars, words)
    }
}
