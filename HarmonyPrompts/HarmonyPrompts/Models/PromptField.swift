import Foundation

enum FieldType: String, Codable, CaseIterable {
    case text
    case textarea
    case select
    case radio
    case checkbox
}

struct PromptField: Identifiable, Codable, Hashable {
    var id: String { name }
    let name: String
    let label: String
    let type: FieldType
    var placeholder: String?
    var options: [String]?
    var defaultValue: String?
    var checkedValue: String?
    var uncheckedValue: String?

    func initialValue() -> String {
        switch type {
        case .checkbox:
            if defaultValue?.lowercased() == "true" {
                return checkedValue ?? ""
            }
            return uncheckedValue ?? ""
        case .select, .radio:
            if let defaultValue, !defaultValue.isEmpty {
                return defaultValue
            }
            return options?.first ?? ""
        case .text, .textarea:
            return defaultValue ?? placeholder ?? ""
        }
    }
}
