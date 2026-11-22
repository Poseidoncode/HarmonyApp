import Foundation

struct PromptTemplate: Identifiable, Codable, Hashable {
    let id: String
    var name: String
    var description: String
    var template: String
    var fields: [PromptField]

    func makeInitialValues() -> [String: String] {
        // Duplicate names must never trap (`uniqueKeysWithValues` would crash);
        // the first definition wins. Stores reject duplicates up front anyway.
        Dictionary(fields.map { ($0.name, $0.initialValue()) }, uniquingKeysWith: { first, _ in first })
    }
}

enum TemplateValidation {
    /// Returns a human-readable problem description, or nil when `templates`
    /// is safe to adopt (unique template IDs, unique field names per template,
    /// no blank identifiers).
    static func problem(in templates: [PromptTemplate]) -> String? {
        var seenIDs = Set<String>()
        for template in templates {
            let id = template.id.trimmingCharacters(in: .whitespacesAndNewlines)
            if id.isEmpty {
                return "A template has an empty id."
            }
            if !seenIDs.insert(template.id).inserted {
                return "Duplicate template id '\(template.id)'."
            }
            var seenFields = Set<String>()
            for field in template.fields {
                if field.name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                    return "Template '\(template.id)' has a field with an empty name."
                }
                if !seenFields.insert(field.name).inserted {
                    return "Template '\(template.id)' has duplicate field name '\(field.name)'."
                }
            }
        }
        return nil
    }
}
