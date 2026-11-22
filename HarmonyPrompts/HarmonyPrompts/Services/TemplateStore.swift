import AppKit
import Combine
import Foundation

enum TemplateStoreError: LocalizedError {
    case invalid(String)

    var errorDescription: String? {
        switch self {
        case .invalid(let message): return message
        }
    }
}

@MainActor
final class TemplateStore: ObservableObject {
    @Published private(set) var templates: [PromptTemplate] = []
    @Published private(set) var lastError: String?
    @Published var lastActionMessage: String?

    /// Upper bound for an imported/loaded templates file.
    nonisolated static let maxImportBytes = 10 * 1_048_576

    private let fileManager = FileManager.default
    private var isLoading = false

    /// Bumped on every `templates` assignment; lets stale async completions yield.
    private var templatesTicket = 0
    /// Bumped on every `importFromData` call; only the latest import applies.
    private var importTicket = 0
    /// Bumped on every `lastError` assignment; a success clears only when no
    /// newer error was posted while it was in flight.
    private var errorTicket = 0
    /// Tail of the save chain; saves run strictly FIFO so a stale snapshot can
    /// never land on disk after a newer one.
    private var pendingSave: Task<Void, Never>?

    var templatesURL: URL {
        cachedAppSupportDirectory.appendingPathComponent("templates.json")
    }

    /// Resolved once: every access previously re-queried FileManager and hit the filesystem.
    private lazy var cachedAppSupportDirectory: URL = {
        let base = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? fileManager.temporaryDirectory
        let dir = base.appendingPathComponent("HarmonyPrompts", isDirectory: true)
        if !fileManager.fileExists(atPath: dir.path) {
            do {
                try fileManager.createDirectory(at: dir, withIntermediateDirectories: true)
            } catch {
                postError("Could not create the templates folder: \(error.localizedDescription)")
            }
        }
        return dir
    }()

    /// Loads templates without blocking the main thread. Both the window and the
    /// menu bar scenes call this on appear; the second call is a no-op while the
    /// first read is in flight. A completion that lost a race (e.g. an import
    /// landed meanwhile) is discarded instead of clobbering newer state.
    func load() {
        guard !isLoading else { return }
        if !fileManager.fileExists(atPath: templatesURL.path) {
            seedDefaultsIfNeeded()
        }

        isLoading = true
        let ticket = templatesTicket
        let url = templatesURL
        let priorSave = pendingSave
        Task.detached(priority: .userInitiated) {
            // Read-after-write: wait for an in-flight save so we never load a
            // pre-import file over an already-imported list.
            await priorSave?.value
            let decoded: [PromptTemplate]
            do {
                let size = (try? url.resourceValues(forKeys: [.fileSizeKey]).fileSize) ?? 0
                guard size <= TemplateStore.maxImportBytes else {
                    throw TemplateStoreError.invalid("File is larger than \(TemplateStore.maxImportBytes / 1_048_576) MB.")
                }
                let data = try Data(contentsOf: url)
                decoded = try JSONDecoder().decode([PromptTemplate].self, from: data)
                if let problem = TemplateValidation.problem(in: decoded) {
                    throw TemplateStoreError.invalid(problem)
                }
            } catch {
                let message = "Failed to load templates: \(error.localizedDescription)"
                await MainActor.run {
                    self.isLoading = false
                    guard ticket == self.templatesTicket else { return }
                    self.assignTemplates([])
                    self.postError(message)
                }
                return
            }
            await MainActor.run {
                self.isLoading = false
                guard ticket == self.templatesTicket else { return }
                self.assignTemplates(decoded)
                self.postError(nil)
            }
        }
    }

    func save() {
        let snapshot = templates
        let url = templatesURL
        let errorTicketAtStart = errorTicket
        let previous = pendingSave
        pendingSave = Task.detached(priority: .utility) {
            await previous?.value
            do {
                let encoder = JSONEncoder()
                encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
                let data = try encoder.encode(snapshot)
                // Keep the previous file (even a corrupt one) so an import or
                // save can never destroy the only copy of hand-edited data.
                let backup = url.appendingPathExtension("bak")
                let fm = FileManager.default
                if fm.fileExists(atPath: url.path) {
                    try? fm.removeItem(at: backup)
                    try? fm.copyItem(at: url, to: backup)
                }
                try data.write(to: url, options: .atomic)
            } catch {
                let message = "Failed to save templates: \(error.localizedDescription)"
                await MainActor.run {
                    self.postError(message)
                }
                return
            }
            await MainActor.run {
                if errorTicketAtStart == self.errorTicket {
                    self.postError(nil)
                }
            }
        }
    }

    /// Decodes `data` off the main thread and adopts it. Takes bytes rather than
    /// a URL so the caller can read inside its security-scoped access: the
    /// background decode must never touch a revoked scope.
    func importFromData(_ data: Data) {
        importTicket += 1
        let ticket = importTicket
        let errorTicketAtStart = errorTicket
        Task.detached(priority: .userInitiated) {
            let imported: [PromptTemplate]
            do {
                guard data.count <= TemplateStore.maxImportBytes else {
                    throw TemplateStoreError.invalid("File is larger than \(TemplateStore.maxImportBytes / 1_048_576) MB.")
                }
                imported = try JSONDecoder().decode([PromptTemplate].self, from: data)
                if let problem = TemplateValidation.problem(in: imported) {
                    throw TemplateStoreError.invalid(problem)
                }
            } catch {
                let message = "Import failed: \(error.localizedDescription)"
                await MainActor.run {
                    guard ticket == self.importTicket else { return }
                    self.postError(message)
                }
                return
            }
            await MainActor.run {
                // A newer import supersedes this one; its outcome reports instead.
                guard ticket == self.importTicket else { return }
                guard !imported.isEmpty else {
                    self.postError("Imported file contains no templates.")
                    return
                }
                self.assignTemplates(imported)
                if errorTicketAtStart == self.errorTicket {
                    self.postError(nil)
                }
                self.lastActionMessage = "Imported \(imported.count) template(s)."
                self.save()
            }
        }
    }

    /// Reports a caller-side failure (e.g. reading the picked file).
    func reportError(_ message: String) {
        postError(message)
    }

    /// Dismisses the current error alert.
    func clearError() {
        postError(nil)
    }

    func revealTemplatesInFinder() {
        NSWorkspace.shared.activateFileViewerSelecting([templatesURL])
    }

    private func assignTemplates(_ value: [PromptTemplate]) {
        templatesTicket += 1
        templates = value
    }

    private func postError(_ message: String?) {
        errorTicket += 1
        lastError = message
    }

    private func seedDefaultsIfNeeded() {
        guard let bundled = Bundle.main.url(forResource: "templates", withExtension: "json") else {
            postError("Bundled default templates are missing from the app.")
            return
        }
        do {
            try fileManager.copyItem(at: bundled, to: templatesURL)
        } catch {
            postError("Could not install default templates: \(error.localizedDescription)")
        }
    }
}
