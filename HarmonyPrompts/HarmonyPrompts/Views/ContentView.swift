import SwiftUI
import UniformTypeIdentifiers

struct ContentView: View {
    @EnvironmentObject private var store: TemplateStore

    @State private var searchText = ""
    @State private var selectedID: String?
    @State private var fieldValues: [String: String] = [:]
    @State private var showImportPanel = false
    @State private var toastMessage: String?
    @State private var toastType: ToastType = .info
    @State private var toastTask: Task<Void, Never>?
    @State private var showAccessibilityAlert = false

    enum ToastType {
        case info
        case success
        case warning
    }

    private var selectedTemplate: PromptTemplate? {
        guard let selectedID else { return nil }
        return store.templates.first { $0.id == selectedID }
    }

    private var generatedPrompt: String {
        guard let template = selectedTemplate else { return "" }
        return PromptRenderer.render(template: template.template, values: fieldValues)
    }

    var body: some View {
        HSplitView {
            TemplateListView(
                templates: store.templates,
                searchText: $searchText,
                selectedID: $selectedID,
                onImport: { showImportPanel = true },
                onOpenFolder: { store.revealTemplatesInFinder() }
            )
            .frame(minWidth: 230, idealWidth: 250, maxWidth: 290)

            Group {
                if let template = selectedTemplate {
                    TemplateFormView(
                        template: template,
                        values: $fieldValues,
                        generatedPrompt: generatedPrompt,
                        onCopy: copyPrompt,
                        onCopyAndPaste: copyAndPastePrompt
                    )
                } else {
                    EmptyStateAgenticView(
                        templates: store.templates,
                        onSelect: { templateId in
                            selectedID = templateId
                        }
                    )
                }
            }
            .frame(minWidth: 380)
        }
        .frame(width: 660, height: 530)
        .background(HarmonyTheme.surfacePage)
        .onAppear {
            // Menu bar window opens after click — capture target app before we took focus.
            FocusTracker.shared.captureCurrentFrontmost()
            store.load()
            if selectedID == nil {
                selectedID = store.templates.first?.id
            }
            syncFieldValues()
        }
        .onChange(of: selectedID) { _, _ in
            syncFieldValues()
        }
        .onChange(of: store.lastActionMessage) { _, message in
            if let message {
                showToast(message, type: .info)
            }
        }
        .fileImporter(
            isPresented: $showImportPanel,
            allowedContentTypes: [.json],
            allowsMultipleSelection: false
        ) { result in
            switch result {
            case .success(let urls):
                guard let url = urls.first else { return }
                if url.startAccessingSecurityScopedResource() {
                    defer { url.stopAccessingSecurityScopedResource() }
                    store.importFromURL(url)
                    selectedID = store.templates.first?.id
                    syncFieldValues()
                    showToast("Templates successfully imported", type: .success)
                }
            case .failure(let error):
                store.lastError = error.localizedDescription
            }
        }
        .overlay(alignment: .bottom) {
            if let toastMessage {
                HStack(spacing: 6) {
                    Image(systemName: toastIcon)
                        .font(.system(size: 11, weight: .bold))
                        .foregroundStyle(toastColor)

                    Text(toastMessage)
                        .font(.system(size: 11.5, weight: .medium, design: .rounded))
                        .foregroundStyle(HarmonyTheme.textInverse)
                }
                .padding(.horizontal, 14)
                .padding(.vertical, 8)
                .background(
                    Capsule()
                        .fill(HarmonyTheme.surfaceDark.opacity(0.95))
                )
                .overlay(
                    Capsule()
                        .stroke(Color.white.opacity(0.15), lineWidth: 1)
                )
                .shadow(color: Color.black.opacity(0.25), radius: 8, x: 0, y: 3)
                .padding(.bottom, 16)
                .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .animation(.spring(response: 0.35, dampingFraction: 0.75), value: toastMessage)
        .alert("Error", isPresented: Binding(
            get: { store.lastError != nil },
            set: { if !$0 { store.lastError = nil } }
        )) {
            Button("OK") { store.lastError = nil }
        } message: {
            Text(store.lastError ?? "")
        }
        .alert("Accessibility Required", isPresented: $showAccessibilityAlert) {
            Button("Open Settings") {
                ClipboardService.openAccessibilitySettings()
            }
            Button("Show Permission Prompt") {
                ClipboardService.promptForAccessibility()
            }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("Copy & Paste needs Accessibility permission for Harmony Prompts.\n\n1. Open Settings\n2. Enable **Harmony Prompts** (path: /Applications/Harmony Prompts.app)\n3. Remove any old entries from build/ folder\n4. Retry Copy & Paste")
        }
    }

    private var toastIcon: String {
        switch toastType {
        case .info: return "sparkles"
        case .success: return "checkmark.circle.fill"
        case .warning: return "exclamationmark.triangle.fill"
        }
    }

    private var toastColor: Color {
        switch toastType {
        case .info: return HarmonyTheme.brandAccentLight
        case .success: return HarmonyTheme.growthEmerald
        case .warning: return HarmonyTheme.brandAccent
        }
    }

    private func showToast(_ msg: String, type: ToastType = .info) {
        toastTask?.cancel()
        toastMessage = msg
        toastType = type
        toastTask = Task { @MainActor in
            try? await Task.sleep(nanoseconds: 2_200_000_000)
            if !Task.isCancelled {
                self.toastMessage = nil
                self.store.lastActionMessage = nil
            }
        }
    }

    private func syncFieldValues() {
        guard let template = selectedTemplate else {
            fieldValues = [:]
            return
        }
        fieldValues = template.makeInitialValues()
    }

    private func copyPrompt() {
        ClipboardService.copy(generatedPrompt)
        showToast("Copied to clipboard", type: .success)
    }

    private func copyAndPastePrompt() {
        ClipboardService.copyAndPaste(generatedPrompt) { result in
            switch result {
            case .pasted(let appName):
                showToast("Pasted into \(appName)", type: .success)
            case .needsAccessibility:
                showToast("Copied — enable Accessibility, then retry", type: .warning)
                showAccessibilityAlert = true
            case .noTargetApp:
                showToast("Copied — click your target app first, then retry", type: .warning)
            case .pasteFailed(let appName):
                showToast("Copied — paste into \(appName) manually with ⌘V", type: .info)
            }
        }
    }
}

// MARK: - Agentic Empty State View (Passionfroot Hero Metaphor)
struct EmptyStateAgenticView: View {
    let templates: [PromptTemplate]
    let onSelect: (String) -> Void

    var body: some View {
        VStack(spacing: 16) {
            Spacer()

            // 3D Agentic Hero Badge
            ZStack {
                Circle()
                    .fill(
                        RadialGradient(
                            colors: [HarmonyTheme.brandAccent.opacity(0.2), Color.clear],
                            center: .center,
                            startRadius: 10,
                            endRadius: 50
                        )
                    )
                    .frame(width: 90, height: 90)

                RoundedRectangle(cornerRadius: 16, style: .continuous)
                    .fill(
                        LinearGradient(
                            colors: [HarmonyTheme.surfaceDarkCard, HarmonyTheme.surfaceDark],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
                    .frame(width: 56, height: 56)
                    .overlay(
                        RoundedRectangle(cornerRadius: 16, style: .continuous)
                            .stroke(Color.white.opacity(0.15), lineWidth: 1)
                    )
                    .shadow(color: HarmonyTheme.brandAccent.opacity(0.2), radius: 10, x: 0, y: 4)

                Image(systemName: "wand.and.stars")
                    .font(.system(size: 24, weight: .bold))
                    .foregroundStyle(
                        LinearGradient(
                            colors: [HarmonyTheme.brandAccentLight, HarmonyTheme.chartViolet],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
            }

            VStack(spacing: 6) {
                Text("Select a Prompt Template")
                    .font(.system(size: 18, weight: .bold, design: .serif))
                    .foregroundStyle(HarmonyTheme.textPrimary)

                Text("Pick a template from the library to configure parameters and generate ready-to-use prompts for AI coding agents.")
                    .font(.system(size: 12, design: .rounded))
                    .foregroundStyle(HarmonyTheme.textSecondary)
                    .multilineTextAlignment(.center)
                    .frame(maxWidth: 320)
            }

            // Quick Prompt Suggestion Pills
            if !templates.isEmpty {
                VStack(spacing: 8) {
                    Text("QUICK SUGGESTIONS")
                        .font(.system(size: 10, weight: .bold, design: .monospaced))
                        .foregroundStyle(HarmonyTheme.textMuted)

                    FlowLayout(spacing: 8) {
                        ForEach(templates.prefix(4)) { template in
                            Button {
                                onSelect(template.id)
                            } label: {
                                HStack(spacing: 5) {
                                    Image(systemName: "sparkle")
                                        .font(.system(size: 9, weight: .bold))
                                        .foregroundStyle(HarmonyTheme.brandAccent)
                                    Text(template.name)
                                        .font(.system(size: 11, weight: .medium, design: .rounded))
                                        .foregroundStyle(HarmonyTheme.textPrimary)
                                }
                                .padding(.horizontal, 10)
                                .padding(.vertical, 6)
                                .background(HarmonyTheme.surfaceClickable)
                                .clipShape(Capsule())
                                .overlay(
                                    Capsule()
                                        .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                                )
                                .shadow(color: Color.black.opacity(0.03), radius: 3, x: 0, y: 1)
                            }
                            .buttonStyle(.plain)
                        }
                    }
                    .frame(maxWidth: 340)
                }
                .padding(.top, 8)
            }

            Spacer()
        }
        .padding(20)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .background(HarmonyTheme.surfacePage)
    }
}
