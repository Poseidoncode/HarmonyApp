import SwiftUI

struct FieldEditorView: View {
    let field: PromptField
    @Binding var value: String

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(field.label)
                .font(.subheadline.weight(.medium))

            switch field.type {
            case .text:
                TextField(field.placeholder ?? "", text: $value)
                    .textFieldStyle(.roundedBorder)

            case .textarea:
                TextEditor(text: $value)
                    .font(.system(.body, design: .monospaced))
                    .frame(minHeight: 72, maxHeight: 120)
                    .overlay(
                        RoundedRectangle(cornerRadius: 6)
                            .stroke(Color.secondary.opacity(0.25))
                    )

            case .select:
                Picker(field.label, selection: $value) {
                    ForEach(field.options ?? [], id: \.self) { option in
                        Text(option).tag(option)
                    }
                }
                .labelsHidden()
                .pickerStyle(.menu)

            case .radio:
                Picker(field.label, selection: $value) {
                    ForEach(field.options ?? [], id: \.self) { option in
                        Text(option).tag(option)
                    }
                }
                .labelsHidden()
                .pickerStyle(.radioGroup)

            case .checkbox:
                Toggle(isOn: Binding(
                    get: { value == (field.checkedValue ?? "true") },
                    set: { enabled in
                        value = enabled ? (field.checkedValue ?? "true") : (field.uncheckedValue ?? "")
                    }
                )) {
                    Text(field.placeholder ?? "Enable")
                }
            }
        }
    }
}
