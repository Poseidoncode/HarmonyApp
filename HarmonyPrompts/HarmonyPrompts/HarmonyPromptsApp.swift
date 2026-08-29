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
        VStack(alignment: .leading, spacing: 16) {
            // Header
            HStack(spacing: 8) {
                ZStack {
                    RoundedRectangle(cornerRadius: 8, style: .continuous)
                        .fill(HarmonyTheme.brandAccent.opacity(0.12))
                        .frame(width: 32, height: 32)
                    Image(systemName: "gearshape.fill")
                        .font(.system(size: 15, weight: .semibold))
                        .foregroundStyle(HarmonyTheme.brandAccent)
                }

                VStack(alignment: .leading, spacing: 2) {
                    Text("Harmony Prompts Settings")
                        .font(.system(size: 15, weight: .bold, design: .serif))
                        .foregroundStyle(HarmonyTheme.textPrimary)
                    Text("Preferences and Permissions")
                        .font(.system(size: 11, design: .rounded))
                        .foregroundStyle(HarmonyTheme.textSecondary)
                }
            }

            Divider()
                .background(HarmonyTheme.borderDefault)

            // Templates Section Card
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    Text("TEMPLATES STORAGE")
                        .font(.system(size: 10, weight: .bold, design: .monospaced))
                        .foregroundStyle(HarmonyTheme.textMuted)
                    Spacer()
                }

                HStack {
                    Text(store.templatesURL.path)
                        .font(.system(size: 10.5, design: .monospaced))
                        .foregroundStyle(HarmonyTheme.textSecondary)
                        .lineLimit(1)
                        .truncationMode(.middle)
                        .textSelection(.enabled)

                    Spacer()

                    Button {
                        store.revealTemplatesInFinder()
                    } label: {
                        HStack(spacing: 4) {
                            Image(systemName: "folder")
                                .font(.system(size: 10, weight: .semibold))
                            Text("Reveal in Finder")
                                .font(.system(size: 11, weight: .medium, design: .rounded))
                        }
                        .foregroundStyle(HarmonyTheme.textPrimary)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(HarmonyTheme.surfaceClickable)
                        .clipShape(RoundedRectangle(cornerRadius: 6, style: .continuous))
                        .overlay(
                            RoundedRectangle(cornerRadius: 6, style: .continuous)
                                .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                        )
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(12)
            .background(HarmonyTheme.surfaceFoundation)
            .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
            .overlay(
                RoundedRectangle(cornerRadius: 8, style: .continuous)
                    .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
            )

            // Accessibility Section Card
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    Text("PASTE ANYWHERE")
                        .font(.system(size: 10, weight: .bold, design: .monospaced))
                        .foregroundStyle(HarmonyTheme.textMuted)

                    Spacer()

                    BadgePill(
                        ClipboardService.isAccessibilityTrusted ? "Allowed" : "Action Needed",
                        icon: ClipboardService.isAccessibilityTrusted ? "checkmark.circle.fill" : "exclamationmark.triangle.fill",
                        foreground: ClipboardService.isAccessibilityTrusted ? HarmonyTheme.growthEmerald : HarmonyTheme.brandAccent,
                        background: ClipboardService.isAccessibilityTrusted ? HarmonyTheme.growthEmerald.opacity(0.12) : HarmonyTheme.brandAccent.opacity(0.12)
                    )
                }

                Text("Copy & Paste returns focus to your frontmost application (Cursor, Xcode, Terminal, Browser) and simulates ⌘V. Click the target input first, then open Harmony.")
                    .font(.system(size: 11, design: .rounded))
                    .foregroundStyle(HarmonyTheme.textSecondary)
                    .lineSpacing(2)

                HStack(spacing: 8) {
                    Button {
                        ClipboardService.openAccessibilitySettings()
                    } label: {
                        Text("Open Settings")
                            .font(.system(size: 11, weight: .semibold, design: .rounded))
                            .foregroundStyle(HarmonyTheme.textInverse)
                            .padding(.horizontal, 10)
                            .padding(.vertical, 5)
                            .background(HarmonyTheme.brandAccent)
                            .clipShape(RoundedRectangle(cornerRadius: 6, style: .continuous))
                    }
                    .buttonStyle(.plain)

                    Button {
                        ClipboardService.promptForAccessibility()
                    } label: {
                        Text("Request Permission")
                            .font(.system(size: 11, weight: .medium, design: .rounded))
                            .foregroundStyle(HarmonyTheme.textPrimary)
                            .padding(.horizontal, 10)
                            .padding(.vertical, 5)
                            .background(HarmonyTheme.surfaceClickable)
                            .clipShape(RoundedRectangle(cornerRadius: 6, style: .continuous))
                            .overlay(
                                RoundedRectangle(cornerRadius: 6, style: .continuous)
                                    .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                            )
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(12)
            .background(HarmonyTheme.surfaceFoundation)
            .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
            .overlay(
                RoundedRectangle(cornerRadius: 8, style: .continuous)
                    .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
            )

            Spacer()
        }
        .padding(16)
        .frame(width: 440, height: 320)
        .background(HarmonyTheme.surfacePage)
    }
}
