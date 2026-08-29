import SwiftUI

struct TemplateListView: View {
    let templates: [PromptTemplate]
    @Binding var searchText: String
    @Binding var selectedID: String?
    var onImport: () -> Void = {}
    var onOpenFolder: () -> Void = {}

    @State private var selectedCategory: String = "All"

    private var categories: [String] {
        var set = Set<String>()
        set.insert("All")
        for t in templates {
            if let cat = categoryFor(template: t) {
                set.insert(cat)
            }
        }
        return Array(set).sorted { $0 == "All" ? true : ($1 == "All" ? false : $0 < $1) }
    }

    private func categoryFor(template: PromptTemplate) -> String? {
        let name = template.name.lowercased()
        let id = template.id.lowercased()
        if name.contains("code") || id.contains("code") { return "Code" }
        if name.contains("bug") || id.contains("bug") { return "Bugs" }
        if name.contains("test") || id.contains("test") { return "Tests" }
        if name.contains("doc") || id.contains("doc") { return "Docs" }
        if name.contains("refactor") || id.contains("refactor") { return "Refactor" }
        return "Prompt"
    }

    private var filtered: [PromptTemplate] {
        templates.filter { template in
            let matchesCategory: Bool
            if selectedCategory == "All" {
                matchesCategory = true
            } else {
                matchesCategory = categoryFor(template: template) == selectedCategory
            }

            guard !searchText.isEmpty else { return matchesCategory }
            let query = searchText.lowercased()
            let matchesSearch = template.name.lowercased().contains(query)
                || template.description.lowercased().contains(query)
                || template.id.lowercased().contains(query)

            return matchesCategory && matchesSearch
        }
    }

    var body: some View {
        VStack(spacing: 0) {
            // MARK: - Header & Brand
            VStack(alignment: .leading, spacing: 10) {
                HStack(spacing: 8) {
                    ZStack {
                        RoundedRectangle(cornerRadius: 7, style: .continuous)
                            .fill(HarmonyTheme.brandAccent.opacity(0.15))
                            .frame(width: 26, height: 26)
                        Image(systemName: "sparkles")
                            .font(.system(size: 13, weight: .bold))
                            .foregroundStyle(HarmonyTheme.brandAccent)
                    }

                    Text("Prompt Library")
                        .font(.system(size: 15, weight: .bold, design: .serif))
                        .foregroundStyle(HarmonyTheme.textPrimary)

                    Spacer()

                    BadgePill(
                        "\(templates.count)",
                        foreground: HarmonyTheme.textSecondary,
                        background: HarmonyTheme.borderDefault
                    )
                }

                // MARK: - Search Box (Passionfroot Prompt input style)
                HStack(spacing: 6) {
                    Image(systemName: "magnifyingglass")
                        .font(.system(size: 12, weight: .medium))
                        .foregroundStyle(HarmonyTheme.textMuted)

                    TextField("Search prompt templates…", text: $searchText)
                        .font(.system(size: 12, design: .rounded))
                        .textFieldStyle(.plain)

                    if !searchText.isEmpty {
                        Button {
                            searchText = ""
                        } label: {
                            Image(systemName: "xmark.circle.fill")
                                .font(.system(size: 11))
                                .foregroundStyle(HarmonyTheme.textMuted)
                        }
                        .buttonStyle(.plain)
                    }
                }
                .padding(.horizontal, 9)
                .padding(.vertical, 6)
                .background(HarmonyTheme.surfaceClickable)
                .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
                .overlay(
                    RoundedRectangle(cornerRadius: 8, style: .continuous)
                        .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                )

                // MARK: - Category Filter Pills
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(spacing: 6) {
                        ForEach(categories, id: \.self) { cat in
                            Button {
                                selectedCategory = cat
                            } label: {
                                Text(cat)
                                    .font(.system(size: 11, weight: selectedCategory == cat ? .semibold : .regular, design: .rounded))
                                    .padding(.horizontal, 9)
                                    .padding(.vertical, 4)
                                    .background(
                                        selectedCategory == cat
                                            ? HarmonyTheme.brandAccent
                                            : HarmonyTheme.surfaceClickable
                                    )
                                    .foregroundStyle(
                                        selectedCategory == cat
                                            ? HarmonyTheme.textInverse
                                            : HarmonyTheme.textSecondary
                                    )
                                    .clipShape(Capsule())
                                    .overlay(
                                        Capsule()
                                            .stroke(selectedCategory == cat ? Color.clear : HarmonyTheme.borderDefault, lineWidth: 1)
                                    )
                            }
                            .buttonStyle(.plain)
                        }
                    }
                }
            }
            .padding(12)
            .background(HarmonyTheme.surfaceFoundation)

            Divider()
                .background(HarmonyTheme.borderDefault)

            // MARK: - Template List Content
            if filtered.isEmpty {
                VStack(spacing: 8) {
                    Spacer()
                    Image(systemName: "doc.text.magnifyingglass")
                        .font(.system(size: 28))
                        .foregroundStyle(HarmonyTheme.textMuted)
                    Text("No templates found")
                        .font(.system(size: 13, weight: .semibold, design: .rounded))
                        .foregroundStyle(HarmonyTheme.textSecondary)
                    Text("Try adjusting your search query or category filter.")
                        .font(.system(size: 11))
                        .foregroundStyle(HarmonyTheme.textMuted)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal, 20)
                    Spacer()
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .background(HarmonyTheme.surfacePage)
            } else {
                ScrollView {
                    LazyVStack(spacing: 6) {
                        ForEach(filtered) { template in
                            let isSelected = selectedID == template.id
                            let category = categoryFor(template: template) ?? "Prompt"

                            Button {
                                selectedID = template.id
                            } label: {
                                HStack(spacing: 0) {
                                    // Left active indicator strip
                                    RoundedRectangle(cornerRadius: 2)
                                        .fill(isSelected ? HarmonyTheme.brandAccent : Color.clear)
                                        .frame(width: 3)
                                        .padding(.vertical, 4)

                                    VStack(alignment: .leading, spacing: 3) {
                                        HStack(alignment: .center) {
                                            Text(template.name)
                                                .font(.system(size: 13, weight: isSelected ? .bold : .medium, design: .rounded))
                                                .foregroundStyle(isSelected ? HarmonyTheme.textPrimary : HarmonyTheme.textPrimary)
                                                .lineLimit(1)

                                            Spacer()

                                            BadgePill(
                                                category,
                                                foreground: isSelected ? HarmonyTheme.brandAccent : HarmonyTheme.textMuted,
                                                background: isSelected ? HarmonyTheme.brandAccent.opacity(0.12) : HarmonyTheme.borderDefault.opacity(0.6)
                                            )
                                        }

                                        Text(template.description)
                                            .font(.system(size: 11, design: .rounded))
                                            .foregroundStyle(isSelected ? HarmonyTheme.textSecondary : HarmonyTheme.textMuted)
                                            .lineLimit(2)
                                            .multilineTextAlignment(.leading)
                                    }
                                    .padding(.leading, 8)
                                    .padding(.trailing, 10)
                                    .padding(.vertical, 8)
                                }
                                .frame(maxWidth: .infinity, alignment: .leading)
                                .background(
                                    isSelected
                                        ? HarmonyTheme.surfaceClickable
                                        : HarmonyTheme.surfacePage
                                )
                                .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
                                .overlay(
                                    RoundedRectangle(cornerRadius: 8, style: .continuous)
                                        .stroke(isSelected ? HarmonyTheme.brandAccent.opacity(0.4) : HarmonyTheme.borderDefault, lineWidth: 1)
                                )
                                .shadow(
                                    color: isSelected ? Color.black.opacity(0.04) : Color.clear,
                                    radius: 4,
                                    x: 0,
                                    y: 2
                                )
                            }
                            .buttonStyle(.plain)
                        }
                    }
                    .padding(8)
                }
                .background(HarmonyTheme.surfaceFoundation.opacity(0.5))
            }

            Divider()
                .background(HarmonyTheme.borderDefault)

            // MARK: - Footer Actions
            HStack(spacing: 8) {
                Button {
                    onImport()
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: "square.and.arrow.down")
                            .font(.system(size: 11, weight: .semibold))
                        Text("Import JSON")
                            .font(.system(size: 11, weight: .semibold, design: .rounded))
                    }
                    .foregroundStyle(HarmonyTheme.textSecondary)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 5)
                    .background(HarmonyTheme.surfaceClickable)
                    .clipShape(RoundedRectangle(cornerRadius: 6, style: .continuous))
                    .overlay(
                        RoundedRectangle(cornerRadius: 6, style: .continuous)
                            .stroke(HarmonyTheme.borderDefault, lineWidth: 1)
                    )
                }
                .buttonStyle(.plain)

                Spacer()

                Button {
                    onOpenFolder()
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: "folder")
                            .font(.system(size: 11, weight: .semibold))
                        Text("Templates Folder")
                            .font(.system(size: 11, weight: .semibold, design: .rounded))
                    }
                    .foregroundStyle(HarmonyTheme.textSecondary)
                    .padding(.horizontal, 8)
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
            .padding(.horizontal, 10)
            .padding(.vertical, 8)
            .background(HarmonyTheme.surfaceFoundation)
        }
    }
}
