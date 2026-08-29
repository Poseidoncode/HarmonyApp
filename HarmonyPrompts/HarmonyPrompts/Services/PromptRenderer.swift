import Foundation

enum PromptRenderer {
    private static let regex = try? NSRegularExpression(pattern: #"\{\{([a-zA-Z0-9_-]+)\}\}"#)

    static func render(template: String, values: [String: String]) -> String {
        guard let regex else { return template }
        let nsString = template as NSString
        let matches = regex.matches(in: template, range: NSRange(location: 0, length: nsString.length))

        var result = template
        for match in matches.reversed() {
            guard match.numberOfRanges > 1 else { continue }
            let fullRange = match.range(at: 0)
            let keyRange = match.range(at: 1)
            let key = nsString.substring(with: keyRange)

            if let val = values[key] {
                if let swiftRange = Range(fullRange, in: result) {
                    result.replaceSubrange(swiftRange, with: val)
                }
            }
        }
        return result
    }
}
