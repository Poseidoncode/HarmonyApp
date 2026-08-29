import SwiftUI

struct TemplateFormView: View {
    let template: PromptTemplate
    @Binding var values: [String: String]
    let generatedPrompt: String
    let onCopy: () -> Void
    let onCopyAndPaste: () -> Void

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 12) {
                VStack(alignment: .leading, spacing: 4) {
                    Text(template.name)
                        .font(.title2.weight(.semibold))
                    Text(template.description)
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                }

                Divider()

                ForEach(template.fields) { field in
                    FieldEditorView(
                        field: field,
                        value: binding(for: field.name)
                    )
                }

                Divider()

                Text("Preview")
                    .font(.subheadline.weight(.medium))

                Text(generatedPrompt)
                    .font(.system(.body, design: .monospaced))
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(8)
                    .background(Color.secondary.opacity(0.08))
                    .clipShape(RoundedRectangle(cornerRadius: 8))

                HStack {
                    Button("Copy") {
                        onCopy()
                    }
                    .keyboardShortcut("c", modifiers: [.command, .shift])

                    Button("Copy & Paste") {
                        onCopyAndPaste()
                    }
                    .keyboardShortcut("v", modifiers: [.command, .shift])
                }
            }
            .padding()
        }
    }

    private func binding(for name: String) -> Binding<String> {
        Binding(
            get: { values[name, default: ""] },
            set: { values[name] = $0 }
        )
    }
}
