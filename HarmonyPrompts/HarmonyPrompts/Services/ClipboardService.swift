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
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(text, forType: .string)
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

        // Terminal and other apps need time to become key after Harmony's menu window closes.
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.28) {
            let pasted = postPaste(into: target)
            if pasted {
                completion(.pasted(into: targetName))
            } else {
                completion(.pasteFailed(into: targetName))
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

    /// Try CGEvent first, then AppleScript (more reliable in Terminal.app).
    private static func postPaste(into target: NSRunningApplication) -> Bool {
        if postCommandV() {
            return true
        }
        return postPasteViaAppleScript(target: target)
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

    private static func postPasteViaAppleScript(target: NSRunningApplication) -> Bool {
        let activateLine: String
        if let bundleID = target.bundleIdentifier {
            let sanitizedBundleID = bundleID.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
            activateLine = "tell application id \"\(sanitizedBundleID)\" to activate"
        } else if let name = target.localizedName {
            let sanitizedName = name.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
            activateLine = "tell application \"\(sanitizedName)\" to activate"
        } else {
            return false
        }

        let script = """
        \(activateLine)
        delay 0.15
        tell application "System Events" to keystroke "v" using command down
        """

        var error: NSDictionary?
        NSAppleScript(source: script)?.executeAndReturnError(&error)
        return error == nil
    }
}
