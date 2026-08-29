#!/usr/bin/env python3
"""Generate a clean macOS app icon from Harmony brand assets."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
HARMONY_LOGO = ROOT.parent.parent / "Harmony" / "media" / "Harmony.png"
ICONSET = ROOT / "HarmonyPrompts" / "Assets.xcassets" / "AppIcon.appiconset"
MENUBAR = ROOT / "HarmonyPrompts" / "Assets.xcassets" / "MenuBarIcon.imageset"
SIZE = 1024

# Harmony brand palette.
BG_TOP = (255, 252, 247)
BG_BOTTOM = (255, 243, 230)
ORANGE = (255, 138, 0)
GOLD = (255, 196, 0)


def rounded_mask(size: int, radius: float) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def remove_paper_background(logo: Image.Image) -> Image.Image:
    """Drop notebook grid / white paper, keep colored logo marks."""
    rgba = logo.convert("RGBA")
    pixels = rgba.load()
    w, h = rgba.size

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue

            # Light paper + faint grid lines.
            if r > 210 and g > 210 and b > 210:
                pixels[x, y] = (r, g, b, 0)
                continue

            # Desaturated gray grid strokes.
            if abs(r - g) < 12 and abs(g - b) < 12 and max(r, g, b) < 210:
                pixels[x, y] = (r, g, b, 0)

    return rgba


def draw_fallback_mark(canvas: Image.Image) -> None:
    draw = ImageDraw.Draw(canvas)
    cx, cy = SIZE // 2, SIZE // 2
    loop_w, loop_h = int(SIZE * 0.22), int(SIZE * 0.30)
    stroke = int(SIZE * 0.045)

    draw.ellipse(
        (cx - loop_w - loop_w // 3, cy - loop_h // 2, cx - loop_w // 3, cy + loop_h // 2),
        outline=ORANGE + (255,),
        width=stroke,
    )
    draw.ellipse(
        (cx + loop_w // 3, cy - loop_h // 2, cx + loop_w + loop_w // 3, cy + loop_h // 2),
        outline=GOLD + (255,),
        width=stroke,
    )

    # Simple handshake hint.
    hand_y = cy + int(SIZE * 0.02)
    draw.rounded_rectangle(
        (cx - int(SIZE * 0.08), hand_y - int(SIZE * 0.05), cx, hand_y + int(SIZE * 0.05)),
        radius=int(SIZE * 0.02),
        fill=GOLD + (255,),
    )
    draw.rounded_rectangle(
        (cx, hand_y - int(SIZE * 0.05), cx + int(SIZE * 0.08), hand_y + int(SIZE * 0.05)),
        radius=int(SIZE * 0.02),
        fill=ORANGE + (255,),
    )


def build_icon() -> Image.Image:
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)

    # Vertical warm gradient.
    for y in range(SIZE):
        t = y / (SIZE - 1)
        color = tuple(int(BG_TOP[i] * (1 - t) + BG_BOTTOM[i] * t) for i in range(3))
        draw.line([(0, y), (SIZE, y)], fill=color + (255,))

    # Soft center glow.
    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse(
        (SIZE * 0.18, SIZE * 0.18, SIZE * 0.82, SIZE * 0.82),
        fill=ORANGE + (30,),
    )
    glow = glow.filter(ImageFilter.GaussianBlur(radius=SIZE * 0.10))
    canvas = Image.alpha_composite(canvas, glow)

    if HARMONY_LOGO.is_file():
        logo = remove_paper_background(Image.open(HARMONY_LOGO))
        bbox = logo.getbbox()
        if bbox:
            logo = logo.crop(bbox)

        target = int(SIZE * 0.56)
        ratio = min(target / logo.width, target / logo.height)
        new_size = (max(1, int(logo.width * ratio)), max(1, int(logo.height * ratio)))
        logo = logo.resize(new_size, Image.Resampling.LANCZOS)

        x = (SIZE - logo.width) // 2
        y = (SIZE - logo.height) // 2 + int(SIZE * 0.01)
        canvas.alpha_composite(logo, (x, y))
    else:
        draw_fallback_mark(canvas)

    mask = rounded_mask(SIZE, SIZE * 0.225)
    output = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    output.paste(canvas, (0, 0), mask)
    return output


def build_menubar_icon(source: Image.Image) -> tuple[Image.Image, Image.Image]:
    """High-contrast monochrome for menu bar template rendering."""
    rgba = source.convert("RGBA")
    gray = Image.new("L", rgba.size, 0)
    src_px = rgba.load()
    dst_px = gray.load()
    w, h = rgba.size

    for y in range(h):
        for x in range(w):
            r, g, b, a = src_px[x, y]
            if a < 20:
                dst_px[x, y] = 0
                continue
            lum = int(0.299 * r + 0.587 * g + 0.114 * b)
            dst_px[x, y] = 255 if lum > 90 else 0

    one_x = gray.resize((16, 16), Image.Resampling.LANCZOS)
    two_x = gray.resize((32, 32), Image.Resampling.LANCZOS)

    def to_rgba(g: Image.Image) -> Image.Image:
        return Image.merge("RGBA", (g, g, g, g))

    return to_rgba(one_x), to_rgba(two_x)


def main() -> None:
    ICONSET.mkdir(parents=True, exist_ok=True)
    MENUBAR.mkdir(parents=True, exist_ok=True)

    icon = build_icon()
    source_path = ICONSET / "icon_512x512@2x.png"
    icon.save(source_path, "PNG")
    print(f"Wrote {source_path}")

    one_x, two_x = build_menubar_icon(icon)
    one_x.save(MENUBAR / "MenuBarIcon.png", "PNG")
    two_x.save(MENUBAR / "MenuBarIcon@2x.png", "PNG")
    print(f"Wrote menu bar icons in {MENUBAR}")


if __name__ == "__main__":
    main()
