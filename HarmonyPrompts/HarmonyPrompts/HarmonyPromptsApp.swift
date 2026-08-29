import AppKit
import SwiftUI

@main
struct HarmonyPromptsApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @StateObject private var store = TemplateStore()

    var body: some Scene {
        WindowGroup("Harmony Prompts") {
            ContentView()
                .environmentObject(store)
        }
        .defaultSize(width: 640, height: 520)

        MenuBarExtra {
            ContentView()
                .environmentObject(store)
        } label: {
            MenuBarLabel()
        }
        .menuBarExtraStyle(.window)

        Settings {
            SettingsView()
                .environmentObject(store)
        }
    }
}

/// Capture the frontmost app before the menu bar window opens.
private struct MenuBarLabel: View {
    var body: some View {
        Image("MenuBarIcon")
            .renderingMode(.template)
            .resizable()
            .frame(width: 18, height: 18)
            .help("Harmony Prompts")
            .onHover { hovering in
                if hovering {
                    FocusTracker.shared.captureCurrentFrontmost()
                }
            }
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationDidFinishLaunching(_ notification: Notification) {
        FocusTracker.shared.start()
        NSApp.setActivationPolicy(.regular)
        if let icon = NSImage(named: "AppIcon") ?? NSImage(named: NSImage.applicationIconName) {
            NSApp.applicationIconImage = icon
        }
        NSApp.activate(ignoringOtherApps: true)

        DispatchQueue.main.async {
            for window in NSApp.windows where window.canBecomeKey {
                window.makeKeyAndOrderFront(nil)
            }
        }
    }
}

struct SettingsView: View {
    @EnvironmentObject private var store: TemplateStore

    var body: some View {
        Form {
            Section("Templates") {
                LabeledContent("Location") {
                    Text(store.templatesURL.path)
                        .font(.caption.monospaced())
                        .textSelection(.enabled)
                }
                Button("Reveal in Finder") {
                    store.revealTemplatesInFinder()
                }
            }

            Section("Paste anywhere") {
                LabeledContent("Accessibility") {
                    Text(ClipboardService.isAccessibilityTrusted ? "Allowed" : "Not allowed")
                        .foregroundStyle(ClipboardService.isAccessibilityTrusted ? .green : .orange)
                }
                Text("**Copy & Paste** returns focus to your last app (Cursor, browser, etc.) and simulates ⌘V there. Click the target input first, then open Harmony. Run from **Applications** (`just install`) for stable Accessibility permission.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                HStack {
                    Button("Open Accessibility Settings") {
                        ClipboardService.openAccessibilitySettings()
                    }
                    Button("Show Permission Prompt") {
                        ClipboardService.promptForAccessibility()
                    }
                }
            }
        }
        .formStyle(.grouped)
        .frame(width: 420, height: 280)
        .padding()
    }
}
