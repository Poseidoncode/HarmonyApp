import SwiftUI

struct TemplateFormView: View {
    let template: PromptTemplate
    @Binding var values: [String: String]
    let generatedPrompt: String
    let onCopy: () -> Void
    let onCopyAndPaste: () -> Void

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 14) {
                // MARK: - Template Header (Warm Brand Foundation Card)
                VStack(alignment: .leading, spacing: 8) {
                    HStack(alignment: .top) {
                        VStack(alignment: .leading, spacing: 4) {
                            HStack(spacing: 6) {
                                BadgePill(
                                    "@Template",
                                    icon: "doc.text.fill",
                                    foreground: HarmonyTheme.brandAccent,
                                    background: HarmonyTheme.brandAccent.opacity(0.12)
                                )
                                BadgePill(
                                    "\(template.fields.count) Variables",
                                    foreground: HarmonyTheme.chartLinkedin,
                                    background: HarmonyTheme.chartLinkedin.opacity(0.12)
                                )
                            }

                            Text(template.name)
                                .font(.system(size: 20, weight: .bold, design: .serif))
                                .foregroundStyle(HarmonyTheme.textPrimary)

                            Text(template.description)
                                .font(.system(size: 12, design: .rounded))
                                .foregroundStyle(HarmonyTheme.textSecondary)
                        }

                        Spacer()
                    }
                }
                .padding(14)
                .background(HarmonyTheme.surfaceBrand)
                .clipShape(RoundedRectangle(cornerRadius: 12, style: .continuous))
                .overlay(
                    RoundedRectangle(cornerRadius: 12, style: .continuous)
                        .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                )

                // MARK: - Variables / Form Fields Section
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        Image(systemName: "slider.horizontal.3")
                            .font(.system(size: 11, weight: .semibold))
                            .foregroundStyle(HarmonyTheme.brandAccent)
                        Text("Configure Parameters")
                            .font(.system(size: 12, weight: .bold, design: .serif))
                            .foregroundStyle(HarmonyTheme.textPrimary)
                    }
                    .padding(.horizontal, 2)

                    VStack(spacing: 8) {
                        ForEach(template.fields) { field in
                            FieldEditorView(
                                field: field,
                                value: binding(for: field.name)
                            )
                        }
                    }
                }

                // MARK: - 3D Agentic Live Prompt Preview Stage (Passionfroot Hero Dark Metaphor)
                VStack(alignment: .leading, spacing: 0) {
                    // Preview Header Bar
                    HStack {
                        HStack(spacing: 6) {
                            Circle()
                                .fill(HarmonyTheme.brandAccent)
                                .frame(width: 7, height: 7)
                                .shadow(color: HarmonyTheme.brandAccent.opacity(0.8), radius: 4)

                            Text("Rendered Prompt Output")
                                .font(.system(size: 11, weight: .semibold, design: .monospaced))
                                .foregroundStyle(HarmonyTheme.textInverse)
                        }

                        Spacer()

                        HStack(spacing: 8) {
                            Text("\(generatedPrompt.count) chars")
                                .font(.system(size: 10, design: .monospaced))
                                .foregroundStyle(HarmonyTheme.textInverseMuted)

                            Text("\(generatedPrompt.split { $0.isWhitespace }.count) words")
                                .font(.system(size: 10, design: .monospaced))
                                .foregroundStyle(HarmonyTheme.textInverseMuted)
                        }
                    }
                    .padding(.horizontal, 12)
                    .padding(.vertical, 8)
                    .background(HarmonyTheme.surfaceDarkCard)

                    Divider()
                        .background(Color.white.opacity(0.1))

                    // Code / Prompt Output Body
                    ScrollView(.vertical) {
                        Text(generatedPrompt.isEmpty ? "// Fill parameters above to generate prompt..." : generatedPrompt)
                            .font(.system(size: 11.5, weight: .regular, design: .monospaced))
                            .foregroundStyle(generatedPrompt.isEmpty ? HarmonyTheme.textInverseMuted.opacity(0.6) : HarmonyTheme.textInverse)
                            .lineSpacing(4)
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .padding(12)
                            .textSelection(.enabled)
                    }
                    .frame(minHeight: 90, maxHeight: 160)
                    .background(HarmonyTheme.surfaceDark)

                    Divider()
                        .background(Color.white.opacity(0.1))

                    // MARK: - Action CTA Bar
                    HStack(spacing: 10) {
                        // Secondary CTA: Copy
                        Button {
                            onCopy()
                        } label: {
                            HStack(spacing: 5) {
                                Image(systemName: "doc.on.doc")
                                    .font(.system(size: 11, weight: .medium))
                                Text("Copy")
                                    .font(.system(size: 12, weight: .semibold, design: .rounded))
                                ShortcutBadge("⌘⇧C", isInverse: true)
                            }
                            .foregroundStyle(HarmonyTheme.textInverse)
                            .padding(.horizontal, 12)
                            .padding(.vertical, 7)
                            .background(Color.white.opacity(0.08))
                            .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
                            .overlay(
                                RoundedRectangle(cornerRadius: 8, style: .continuous)
                                    .stroke(Color.white.opacity(0.15), lineWidth: 1)
                            )
                        }
                        .buttonStyle(.plain)
                        .keyboardShortcut("c", modifiers: [.command, .shift])

                        Spacer()

                        // Primary CTA: Copy & Paste (Passionfroot Accent Gradient)
                        Button {
                            onCopyAndPaste()
                        } label: {
                            HStack(spacing: 6) {
                                Image(systemName: "sparkles")
                                    .font(.system(size: 12, weight: .bold))
                                Text("Copy & Paste")
                                    .font(.system(size: 12, weight: .bold, design: .rounded))
                                ShortcutBadge("⌘⇧V", isInverse: true)
                            }
                            .foregroundStyle(Color.white)
                            .padding(.horizontal, 14)
                            .padding(.vertical, 7)
                            .background(
                                LinearGradient(
                                    colors: [HarmonyTheme.brandAccentLight, HarmonyTheme.brandAccent],
                                    startPoint: .topLeading,
                                    endPoint: .bottomTrailing
                                )
                            )
                            .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
                            .overlay(
                                RoundedRectangle(cornerRadius: 8, style: .continuous)
                                    .stroke(Color.white.opacity(0.25), lineWidth: 1)
                            )
                            .shadow(color: HarmonyTheme.brandAccent.opacity(0.35), radius: 6, x: 0, y: 2)
                        }
                        .buttonStyle(.plain)
                        .keyboardShortcut("v", modifiers: [.command, .shift])
                    }
                    .padding(10)
                    .background(HarmonyTheme.surfaceDarkCard)
                }
                .clipShape(RoundedRectangle(cornerRadius: 10, style: .continuous))
                .overlay(
                    RoundedRectangle(cornerRadius: 10, style: .continuous)
                        .stroke(Color.white.opacity(0.12), lineWidth: 1)
                )
                .shadow(color: Color.black.opacity(0.15), radius: 8, x: 0, y: 4)
            }
            .padding(14)
        }
        .background(HarmonyTheme.surfacePage)
    }

    private func binding(for name: String) -> Binding<String> {
        Binding(
            get: { values[name, default: ""] },
            set: { values[name] = $0 }
        )
    }
}
