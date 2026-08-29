import AppKit

/// Remembers the last app the user was in before Harmony Prompts took focus.
final class FocusTracker {
    static let shared = FocusTracker()

    private(set) var lastNonSelfApp: NSRunningApplication?
    private var workspaceObserver: NSObjectProtocol?
    private var globalEventMonitor: Any?

    private var ourBundleID: String? {
        Bundle.main.bundleIdentifier
    }

    private init() {}

    func start() {
        stop()
        refreshFromFrontmost()

        workspaceObserver = NSWorkspace.shared.notificationCenter.addObserver(
            forName: NSWorkspace.didActivateApplicationNotification,
            object: nil,
            queue: .main
        ) { [weak self] notification in
            guard let self,
                  let app = notification.userInfo?[NSWorkspace.applicationUserInfoKey] as? NSRunningApplication,
                  app.bundleIdentifier != self.ourBundleID else { return }
            self.lastNonSelfApp = app
        }

        // Capture target app on any click (before menu bar extra steals focus).
        globalEventMonitor = NSEvent.addGlobalMonitorForEvents(matching: .leftMouseDown) { [weak self] _ in
            self?.refreshFromFrontmost()
        }
    }

    func stop() {
        if let observer = workspaceObserver {
            NSWorkspace.shared.notificationCenter.removeObserver(observer)
            workspaceObserver = nil
        }
        if let monitor = globalEventMonitor {
            NSEvent.removeMonitor(monitor)
            globalEventMonitor = nil
        }
    }

    deinit {
        stop()
    }

    /// Snapshot the current frontmost app (call before Harmony takes focus).
    func captureCurrentFrontmost() {
        refreshFromFrontmost()
    }

    /// App that should receive the simulated ⌘V.
    func pasteTarget() -> NSRunningApplication? {
        // Menu bar windows often keep the previous app as "frontmost" — prefer that.
        if let front = NSWorkspace.shared.frontmostApplication,
           front.bundleIdentifier != ourBundleID,
           !front.isTerminated {
            return front
        }

        if let app = lastNonSelfApp, !app.isTerminated {
            return app
        }

        return nil
    }

    private func refreshFromFrontmost() {
        if let front = NSWorkspace.shared.frontmostApplication,
           front.bundleIdentifier != ourBundleID {
            lastNonSelfApp = front
        }
    }
}
