import SwiftUI

struct FieldEditorView: View {
    let field: PromptField
    @Binding var value: String

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            // Field Header
            HStack(spacing: 6) {
                Text(field.label)
                    .font(.system(size: 12, weight: .semibold, design: .rounded))
                    .foregroundStyle(HarmonyTheme.textPrimary)

                if let defaultValue = field.defaultValue, !defaultValue.isEmpty {
                    Text("default: \(defaultValue)")
                        .font(.system(size: 10, design: .monospaced))
                        .foregroundStyle(HarmonyTheme.textMuted)
                }

                Spacer()

                BadgePill(
                    field.type.rawValue.uppercased(),
                    foreground: HarmonyTheme.textMuted,
                    background: HarmonyTheme.borderDefault.opacity(0.6)
                )
            }

            // Input Control
            switch field.type {
            case .text:
                TextField(field.placeholder ?? "Enter \(field.label)...", text: $value)
                    .font(.system(size: 12, design: .rounded))
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background(HarmonyTheme.surfaceClickable)
                    .clipShape(RoundedRectangle(cornerRadius: 7, style: .continuous))
                    .overlay(
                        RoundedRectangle(cornerRadius: 7, style: .continuous)
                            .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                    )
                    .textFieldStyle(.plain)

            case .textarea:
                VStack(spacing: 0) {
                    TextEditor(text: $value)
                        .font(.system(size: 11, design: .monospaced))
                        .scrollContentBackground(.hidden)
                        .padding(6)
                        .frame(minHeight: 70, maxHeight: 110)
                        .background(HarmonyTheme.surfaceClickable)
                }
                .clipShape(RoundedRectangle(cornerRadius: 7, style: .continuous))
                .overlay(
                    RoundedRectangle(cornerRadius: 7, style: .continuous)
                        .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                )

            case .select:
                Menu {
                    ForEach(field.options ?? [], id: \.self) { option in
                        Button {
                            value = option
                        } label: {
                            HStack {
                                Text(option)
                                if value == option {
                                    Image(systemName: "checkmark")
                                }
                            }
                        }
                    }
                } label: {
                    HStack {
                        Text(value.isEmpty ? (field.placeholder ?? "Select an option") : value)
                            .font(.system(size: 12, design: .rounded))
                            .foregroundStyle(value.isEmpty ? HarmonyTheme.textMuted : HarmonyTheme.textPrimary)
                        Spacer()
                        Image(systemName: "chevron.up.chevron.down")
                            .font(.system(size: 10, weight: .semibold))
                            .foregroundStyle(HarmonyTheme.textMuted)
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background(HarmonyTheme.surfaceClickable)
                    .clipShape(RoundedRectangle(cornerRadius: 7, style: .continuous))
                    .overlay(
                        RoundedRectangle(cornerRadius: 7, style: .continuous)
                            .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                    )
                }
                .buttonStyle(.plain)

            case .radio:
                FlowLayout(spacing: 6) {
                    ForEach(field.options ?? [], id: \.self) { option in
                        let isSelected = value == option
                        Button {
                            value = option
                        } label: {
                            HStack(spacing: 6) {
                                ZStack {
                                    Circle()
                                        .stroke(isSelected ? HarmonyTheme.brandAccent : HarmonyTheme.borderStrong, lineWidth: 1.5)
                                        .frame(width: 13, height: 13)
                                    if isSelected {
                                        Circle()
                                            .fill(HarmonyTheme.brandAccent)
                                            .frame(width: 7, height: 7)
                                    }
                                }
                                Text(option)
                                    .font(.system(size: 11, weight: isSelected ? .semibold : .regular, design: .rounded))
                                    .foregroundStyle(isSelected ? HarmonyTheme.textPrimary : HarmonyTheme.textSecondary)
                            }
                            .padding(.horizontal, 9)
                            .padding(.vertical, 5)
                            .background(
                                isSelected
                                    ? HarmonyTheme.brandAccent.opacity(0.08)
                                    : HarmonyTheme.surfaceClickable
                            )
                            .clipShape(RoundedRectangle(cornerRadius: 6, style: .continuous))
                            .overlay(
                                RoundedRectangle(cornerRadius: 6, style: .continuous)
                                    .stroke(isSelected ? HarmonyTheme.brandAccent.opacity(0.4) : HarmonyTheme.borderDefault, lineWidth: 1)
                            )
                        }
                        .buttonStyle(.plain)
                    }
                }

            case .checkbox:
                let isChecked = value == (field.checkedValue ?? "true")
                Button {
                    value = isChecked ? (field.uncheckedValue ?? "") : (field.checkedValue ?? "true")
                } label: {
                    HStack(spacing: 8) {
                        ZStack {
                            RoundedRectangle(cornerRadius: 4, style: .continuous)
                                .fill(isChecked ? HarmonyTheme.growthEmerald : HarmonyTheme.surfaceClickable)
                                .frame(width: 16, height: 16)
                                .overlay(
                                    RoundedRectangle(cornerRadius: 4, style: .continuous)
                                        .stroke(isChecked ? HarmonyTheme.growthEmerald : HarmonyTheme.borderStrong, lineWidth: 1)
                                )
                            if isChecked {
                                Image(systemName: "checkmark")
                                    .font(.system(size: 10, weight: .bold))
                                    .foregroundStyle(.white)
                            }
                        }

                        Text(field.placeholder ?? "Enable this setting")
                            .font(.system(size: 12, weight: isChecked ? .medium : .regular, design: .rounded))
                            .foregroundStyle(HarmonyTheme.textPrimary)

                        Spacer()
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background(isChecked ? HarmonyTheme.growthEmerald.opacity(0.06) : HarmonyTheme.surfaceClickable)
                    .clipShape(RoundedRectangle(cornerRadius: 7, style: .continuous))
                    .overlay(
                        RoundedRectangle(cornerRadius: 7, style: .continuous)
                            .stroke(isChecked ? HarmonyTheme.growthEmerald.opacity(0.3) : HarmonyTheme.borderDefault, lineWidth: 1)
                    )
                }
                .buttonStyle(.plain)
            }
        }
        .padding(10)
        .background(HarmonyTheme.surfaceFoundation.opacity(0.6))
        .clipShape(RoundedRectangle(cornerRadius: 9, style: .continuous))
        .overlay(
            RoundedRectangle(cornerRadius: 9, style: .continuous)
                .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
        )
    }
}
