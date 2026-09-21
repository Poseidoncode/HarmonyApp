import SwiftUI

/// A flexible flow layout that wraps items across lines when horizontal space is exceeded.
public struct FlowLayout: Layout {
    public var spacing: CGFloat

    public init(spacing: CGFloat = 6) {
        self.spacing = spacing
    }

    /// Subview sizes measured once per layout invalidation, shared by
    /// `sizeThatFits` and `placeSubviews` (each previously re-queried every subview).
    public struct Cache {
        var sizes: [CGSize] = []
    }

    public func makeCache(subviews: Subviews) -> Cache {
        Cache()
    }

    public func updateCache(_ cache: inout Cache, subviews: Subviews) {
        cache.sizes = subviews.map { $0.sizeThatFits(.unspecified) }
    }

    public func sizeThatFits(proposal: ProposedViewSize, subviews: Subviews, cache: inout Cache) -> CGSize {
        let width = proposal.replacingUnspecifiedDimensions().width
        let sizes = cachedSizes(subviews: subviews, cache: cache)
        var currentX: CGFloat = 0
        var currentY: CGFloat = 0
        var lineHeight: CGFloat = 0

        for size in sizes {
            if currentX + size.width > width, currentX > 0 {
                currentX = 0
                currentY += lineHeight + spacing
                lineHeight = 0
            }
            currentX += size.width + spacing
            lineHeight = max(lineHeight, size.height)
        }
        return CGSize(width: width, height: currentY + lineHeight)
    }

    public func placeSubviews(in bounds: CGRect, proposal: ProposedViewSize, subviews: Subviews, cache: inout Cache) {
        let sizes = cachedSizes(subviews: subviews, cache: cache)
        var currentX = bounds.minX
        var currentY = bounds.minY
        var lineHeight: CGFloat = 0

        for (index, view) in subviews.enumerated() {
            let size = sizes[index]
            if currentX + size.width > bounds.maxX, currentX > bounds.minX {
                currentX = bounds.minX
                currentY += lineHeight + spacing
                lineHeight = 0
            }
            view.place(at: CGPoint(x: currentX, y: currentY), proposal: .unspecified)
            currentX += size.width + spacing
            lineHeight = max(lineHeight, size.height)
        }
    }

    private func cachedSizes(subviews: Subviews, cache: Cache) -> [CGSize] {
        cache.sizes.count == subviews.count
            ? cache.sizes
            : subviews.map { $0.sizeThatFits(.unspecified) }
    }
}
