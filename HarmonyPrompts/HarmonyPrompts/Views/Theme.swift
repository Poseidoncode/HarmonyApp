import SwiftUI

// MARK: - Design System Tokens (from design.md)
// Adopts the OKLCH / Hex tokens from Passionfroot Design Specification

public enum HarmonyTheme {
    // MARK: - Surfaces
    public static let surfacePage = Color(hex: "FCFCF7")        // Full-page background
    public static let surfaceFoundation = Color(hex: "F5F3EA")  // Card & section background
    public static let surfaceBrand = Color(hex: "F0EDE0")       // Hero outer frame & brand surface
    public static let surfaceClickable = Color(hex: "FFFFFF")   // White interactive card surfaces
    public static let surfaceDark = Color(hex: "190922")        // Deep hero dark stage / 3D agentic canvas
    public static let surfaceDarkCard = Color(hex: "231230")    // Dark card surface
    public static let surfaceDarkBorder = Color(hex: "38214A")  // Dark border

    // MARK: - Text / Ink Scale
    public static let textPrimary = Color(hex: "1C1D1E")        // Headings and main text
    public static let textSecondary = Color(hex: "3D3A36")      // Subtitles & descriptions
    public static let textMuted = Color(hex: "8A8580")          // Metadata labels & placeholders
    public static let textInverse = Color(hex: "F5F3EA")        // Text on dark surfaces (not pure white)
    public static let textInverseMuted = Color(hex: "D9D5CE")   // Secondary text on dark surfaces

    // MARK: - Brand & Accents
    public static let brandAccent = Color(hex: "EB6928")        // Primary warm Passionfroot orange
    public static let brandAccentLight = Color(hex: "FF9147")   // Gradient highlight & pulse
    public static let growthEmerald = Color(hex: "3EBB85")      // Success / verified / positive growth

    // MARK: - Borders
    public static let borderSubtle = Color(hex: "F5F3EA")       // Subtle divider
    public static let borderDefault = Color(hex: "EDEAE4")      // Standard card and input border
    public static let borderStrong = Color(hex: "D9D5CE")       // Emphasized border

    // MARK: - Chart & Accents
    public static let chartViolet = Color(hex: "B26BF5")        // Radial pulse / AI glow
    public static let chartLinkedin = Color(hex: "2C91AF")      // Info / tag badge
    public static let chartYoutube = Color(hex: "EE5968")       // Danger / alert badge
}

// MARK: - Hex Color Extension
extension Color {
    init(hex: String) {
        let hex = hex.trimmingCharacters(in: CharacterSet.alphanumerics.inverted)
        var int: UInt64 = 0
        Scanner(string: hex).scanHexInt64(&int)
        let a, r, g, b: UInt64
        switch hex.count {
        case 3: // RGB (12-bit)
            (a, r, g, b) = (255, (int >> 8) * 17, (int >> 4 & 0xF) * 17, (int & 0xF) * 17)
        case 6: // RGB (24-bit)
            (a, r, g, b) = (255, int >> 16, int >> 8 & 0xFF, int & 0xFF)
        case 8: // ARGB (32-bit)
            (a, r, g, b) = (int >> 24, int >> 16 & 0xFF, int >> 8 & 0xFF, int & 0xFF)
        default:
            (a, r, g, b) = (255, 0, 0, 0)
        }

        self.init(
            .sRGB,
            red: Double(r) / 255,
            green: Double(g) / 255,
            blue:  Double(b) / 255,
            opacity: Double(a) / 255
        )
    }
}

// MARK: - Reusable UI Components & Modifiers

public struct BadgePill: View {
    let text: String
    var icon: String? = nil
    var foreground: Color = HarmonyTheme.brandAccent
    var background: Color = HarmonyTheme.brandAccent.opacity(0.12)

    public init(_ text: String, icon: String? = nil, foreground: Color = HarmonyTheme.brandAccent, background: Color = HarmonyTheme.brandAccent.opacity(0.12)) {
        self.text = text
        self.icon = icon
        self.foreground = foreground
        self.background = background
    }

    public var body: some View {
        HStack(spacing: 4) {
            if let icon {
                Image(systemName: icon)
                    .font(.system(size: 9, weight: .bold))
            }
            Text(text)
                .font(.system(size: 11, weight: .semibold, design: .rounded))
        }
        .foregroundStyle(foreground)
        .padding(.horizontal, 7)
        .padding(.vertical, 3)
        .background(background)
        .clipShape(Capsule())
    }
}

public struct ShortcutBadge: View {
    let text: String
    var isInverse: Bool = false

    public init(_ text: String, isInverse: Bool = false) {
        self.text = text
        self.isInverse = isInverse
    }

    public var body: some View {
        Text(text)
            .font(.system(size: 10, weight: .bold, design: .monospaced))
            .foregroundStyle(isInverse ? HarmonyTheme.textInverse : HarmonyTheme.textSecondary)
            .padding(.horizontal, 5)
            .padding(.vertical, 2)
            .background(
                RoundedRectangle(cornerRadius: 4)
                    .fill(isInverse ? Color.white.opacity(0.15) : Color.black.opacity(0.06))
            )
            .overlay(
                RoundedRectangle(cornerRadius: 4)
                    .stroke(isInverse ? Color.white.opacity(0.2) : Color.black.opacity(0.1), lineWidth: 0.5)
            )
    }
}
