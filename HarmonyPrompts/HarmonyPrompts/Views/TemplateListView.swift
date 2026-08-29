import SwiftUI

struct TemplateListView: View {
    let templates: [PromptTemplate]
    @Binding var searchText: String
    @Binding var selectedID: String?

    private var filtered: [PromptTemplate] {
        guard !searchText.isEmpty else { return templates }
        let query = searchText.lowercased()
        return templates.filter {
            $0.name.lowercased().contains(query)
                || $0.description.lowercased().contains(query)
                || $0.id.lowercased().contains(query)
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            TextField("Search templates…", text: $searchText)
                .textFieldStyle(.roundedBorder)

            if filtered.isEmpty {
                ContentUnavailableView("No templates", systemImage: "doc.text.magnifyingglass")
            } else {
                List(selection: $selectedID) {
                    ForEach(filtered) { template in
                        VStack(alignment: .leading, spacing: 2) {
                            Text(template.name)
                                .font(.headline)
                            Text(template.description)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                                .lineLimit(2)
                        }
                        .tag(template.id as String?)
                    }
                }
                .listStyle(.inset)
            }
        }
    }
}
