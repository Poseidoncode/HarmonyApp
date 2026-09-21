import AppKit
import Combine
import Foundation

@MainActor
final class TemplateStore: ObservableObject {
    @Published private(set) var templates: [PromptTemplate] = []
    @Published var lastError: String?
    @Published var lastActionMessage: String?

    private let fileManager = FileManager.default
    private var isLoading = false

    var templatesURL: URL {
        cachedAppSupportDirectory.appendingPathComponent("templates.json")
    }

    /// Resolved once: every access previously re-queried FileManager and hit the filesystem.
    private lazy var cachedAppSupportDirectory: URL = {
        let base = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? fileManager.temporaryDirectory
        let dir = base.appendingPathComponent("HarmonyPrompts", isDirectory: true)
        if !fileManager.fileExists(atPath: dir.path) {
            try? fileManager.createDirectory(at: dir, withIntermediateDirectories: true)
        }
        return dir
    }()

    /// Loads templates without blocking the main thread. Both the window and the
    /// menu bar scenes call this on appear; the second call is a no-op while the
    /// first read is in flight.
    func load() {
        guard !isLoading else { return }
        if !fileManager.fileExists(atPath: templatesURL.path) {
            seedDefaultsIfNeeded()
        }

        isLoading = true
        let url = templatesURL
        Task.detached(priority: .userInitiated) {
            let decoded: [PromptTemplate]
            do {
                let data = try Data(contentsOf: url)
                decoded = try JSONDecoder().decode([PromptTemplate].self, from: data)
            } catch {
                let message = "Failed to load templates: \(error.localizedDescription)"
                await MainActor.run {
                    self.isLoading = false
                    self.lastError = message
                    self.templates = []
                }
                return
            }
            await MainActor.run {
                self.isLoading = false
                self.templates = decoded
                self.lastError = nil
            }
        }
    }

    func save() {
        let snapshot = templates
        let url = templatesURL
        Task.detached(priority: .utility) {
            do {
                let encoder = JSONEncoder()
                encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
                let data = try encoder.encode(snapshot)
                try data.write(to: url, options: .atomic)
            } catch {
                let message = "Failed to save templates: \(error.localizedDescription)"
                await MainActor.run {
                    self.lastError = message
                }
                return
            }
            await MainActor.run {
                self.lastError = nil
            }
        }
    }

    func importFromURL(_ url: URL) {
        Task.detached(priority: .userInitiated) {
            let imported: [PromptTemplate]
            do {
                let data = try Data(contentsOf: url)
                imported = try JSONDecoder().decode([PromptTemplate].self, from: data)
            } catch {
                let message = "Import failed: \(error.localizedDescription)"
                await MainActor.run {
                    self.lastError = message
                }
                return
            }
            await MainActor.run {
                guard !imported.isEmpty else {
                    self.lastError = "Imported file contains no templates."
                    return
                }
                self.templates = imported
                self.lastError = nil
                self.lastActionMessage = "Imported \(imported.count) template(s)."
                self.save()
            }
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
