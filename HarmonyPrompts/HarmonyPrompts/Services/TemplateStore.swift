import AppKit
import Combine
import Foundation

@MainActor
final class TemplateStore: ObservableObject {
    @Published private(set) var templates: [PromptTemplate] = []
    @Published var lastError: String?
    @Published var lastActionMessage: String?

    private let fileManager = FileManager.default

    var templatesURL: URL {
        appSupportDirectory.appendingPathComponent("templates.json")
    }

    private var appSupportDirectory: URL {
        let base = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let dir = base.appendingPathComponent("HarmonyPrompts", isDirectory: true)
        if !fileManager.fileExists(atPath: dir.path) {
            try? fileManager.createDirectory(at: dir, withIntermediateDirectories: true)
        }
        return dir
    }

    func load() {
        if !fileManager.fileExists(atPath: templatesURL.path) {
            seedDefaultsIfNeeded()
        }

        do {
            let data = try Data(contentsOf: templatesURL)
            templates = try JSONDecoder().decode([PromptTemplate].self, from: data)
            lastError = nil
        } catch {
            lastError = "Failed to load templates: \(error.localizedDescription)"
            templates = []
        }
    }

    func save() {
        do {
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
            let data = try encoder.encode(templates)
            try data.write(to: templatesURL, options: .atomic)
            lastError = nil
        } catch {
            lastError = "Failed to save templates: \(error.localizedDescription)"
        }
    }

    func importFromURL(_ url: URL) {
        do {
            let data = try Data(contentsOf: url)
            let imported = try JSONDecoder().decode([PromptTemplate].self, from: data)
            guard !imported.isEmpty else {
                lastError = "Imported file contains no templates."
                return
            }
            templates = imported
            save()
            lastActionMessage = "Imported \(imported.count) template(s)."
            lastError = nil
        } catch {
            lastError = "Import failed: \(error.localizedDescription)"
        }
    }

    func revealTemplatesInFinder() {
        NSWorkspace.shared.activateFileViewerSelecting([templatesURL])
    }

    private func seedDefaultsIfNeeded() {
        guard let bundled = Bundle.main.url(forResource: "templates", withExtension: "json") else {
            return
        }
        try? fileManager.copyItem(at: bundled, to: templatesURL)
    }
}
