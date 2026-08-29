import Foundation

struct PromptTemplate: Identifiable, Codable, Hashable {
    let id: String
    var name: String
    var description: String
    var template: String
    var fields: [PromptField]

    func makeInitialValues() -> [String: String] {
        Dictionary(uniqueKeysWithValues: fields.map { ($0.name, $0.initialValue()) })
    }
}
