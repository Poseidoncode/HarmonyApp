import AppKit

enum CopyPasteResult {
    case pasted(into: String)
    case needsAccessibility
    case noTargetApp
    case pasteFailed(into: String)
}

enum ClipboardService {
    /// Whether this process has Accessibility (trusted) permission.
    static var isAccessibilityTrusted: Bool {
        AXIsProcessTrusted()
    }

    static func copy(_ text: String) {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        pasteboard.setString(text, forType: .string)
        // Prompts can contain sensitive material: ask clipboard managers not
        // to record it (nspasteboard.org convention, matches the Windows build).
        pasteboard.setString("", forType: NSPasteboard.PasteboardType("org.nspasteboard.ConcealedType"))
    }

    /// Copies text, returns focus to the target app, then pastes.
    static func copyAndPaste(_ text: String, completion: @escaping (CopyPasteResult) -> Void) {
        copy(text)

        guard isAccessibilityTrusted else {
            completion(.needsAccessibility)
            return
        }

        guard let target = FocusTracker.shared.pasteTarget() else {
            completion(.noTargetApp)
            return
        }

        let targetName = target.localizedName ?? "app"
        dismissHarmonyWindows()
        target.activate(options: [.activateAllWindows, .activateIgnoringOtherApps])

        // Terminal and other apps need time to become key after Harmony's menu
        // window closes. Only paste once the target is verifiably frontmost;
        // otherwise ⌘V could land in whatever app happens to be active.
        awaitFrontmost(target, attemptsLeft: 20) { isFrontmost in
            if isFrontmost, postCommandV() {
                completion(.pasted(into: targetName))
            } else {
                completion(.pasteFailed(into: targetName))
            }
        }
    }

    private static func awaitFrontmost(
        _ target: NSRunningApplication,
        attemptsLeft: Int,
        completion: @escaping (Bool) -> Void
    ) {
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) {
            if target.isTerminated {
                completion(false)
            } else if NSWorkspace.shared.frontmostApplication?.processIdentifier == target.processIdentifier {
                completion(true)
            } else if attemptsLeft > 1 {
                awaitFrontmost(target, attemptsLeft: attemptsLeft - 1, completion: completion)
            } else {
                completion(false)
            }
        }
    }

    /// Opens System Settings → Privacy & Security → Accessibility.
    static func openAccessibilitySettings() {
        if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility") {
            NSWorkspace.shared.open(url)
        }
    }

    /// Shows the system Accessibility prompt once. Call only from explicit user action (e.g. Settings button).
    static func promptForAccessibility() {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(options)
    }

    private static func dismissHarmonyWindows() {
        for window in NSApp.windows {
            window.orderOut(nil)
        }
        NSApp.hide(nil)
    }

    private static func postCommandV() -> Bool {
        let source = CGEventSource(stateID: .hidSystemState)
        let keyDown = CGEvent(keyboardEventSource: source, virtualKey: 0x09, keyDown: true)
        let keyUp = CGEvent(keyboardEventSource: source, virtualKey: 0x09, keyDown: false)
        keyDown?.flags = .maskCommand
        keyUp?.flags = .maskCommand

        guard let keyDown, let keyUp else { return false }

        keyDown.post(tap: .cgSessionEventTap)
        keyUp.post(tap: .cgSessionEventTap)
        return true
    }
}
