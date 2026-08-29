import SwiftUI
import UniformTypeIdentifiers

struct ContentView: View {
    @EnvironmentObject private var store: TemplateStore

    @State private var searchText = ""
    @State private var selectedID: String?
    @State private var fieldValues: [String: String] = [:]
    @State private var showImportPanel = false
    @State private var toastMessage: String?
    @State private var showAccessibilityAlert = false

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
                selectedID: $selectedID
            )
            .frame(minWidth: 220, idealWidth: 240, maxWidth: 280)

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
                    ContentUnavailableView(
                        "Select a template",
                        systemImage: "text.badge.plus",
                        description: Text("Pick a prompt from the list, fill fields, then Copy or Copy & Paste.")
                    )
                }
            }
            .frame(minWidth: 360)
        }
        .frame(width: 640, height: 520)
        .toolbar {
            ToolbarItemGroup {
                Button("Import JSON…") {
                    showImportPanel = true
                }
                Button("Open templates folder") {
                    store.revealTemplatesInFinder()
                }
            }
        }
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
                toastMessage = message
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
                }
            case .failure(let error):
                store.lastError = error.localizedDescription
            }
        }
        .overlay(alignment: .bottom) {
            if let toastMessage {
                Text(toastMessage)
                    .font(.caption)
                    .padding(.horizontal, 12)
                    .padding(.vertical, 6)
                    .background(.ultraThinMaterial)
                    .clipShape(Capsule())
                    .padding(.bottom, 8)
                    .onAppear {
                        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                            self.toastMessage = nil
                            store.lastActionMessage = nil
                        }
                    }
            }
        }
        .alert("Error", isPresented: .constant(store.lastError != nil)) {
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

    private func syncFieldValues() {
        guard let template = selectedTemplate else {
            fieldValues = [:]
            return
        }
        fieldValues = template.makeInitialValues()
    }

    private func copyPrompt() {
        ClipboardService.copy(generatedPrompt)
        toastMessage = "Copied to clipboard"
    }

    private func copyAndPastePrompt() {
        ClipboardService.copyAndPaste(generatedPrompt) { result in
            switch result {
            case .pasted(let appName):
                toastMessage = "Pasted into \(appName)"
            case .needsAccessibility:
                toastMessage = "Copied — enable Accessibility, then retry"
                showAccessibilityAlert = true
            case .noTargetApp:
                toastMessage = "Copied — click Terminal (or target app) first, then retry"
            case .pasteFailed(let appName):
                toastMessage = "Copied — paste into \(appName) manually with ⌘V"
            }
        }
    }
}
