"""Build exact, reusable UI glyph atlases from the supplied Cute Dino font.

The original TTF remains the preferred Unity text source. Atlases are provided
for a sprite-only UI path; cell locations are recorded in glyph_manifest.json.
"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/UI/WorkshopMatchProduction/Group01_Font/CuteDino"
FONT = ROOT / "Assets/cute_dino_2/Cute Dino.ttf"
CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789/+:=-!?.;"
CELL = 112
COLUMNS = 12


def font(size: int) -> ImageFont.FreeTypeFont:
    result = ImageFont.truetype(str(FONT), size)
    return result


def draw_centered(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int],
                  value: str, face: ImageFont.FreeTypeFont, fill: str,
                  stroke: str, stroke_width: int) -> None:
    left, top, right, bottom = box
    draw.text(((left + right) / 2, (top + bottom) / 2), value,
              font=face, anchor="mm", fill=fill,
              stroke_width=stroke_width, stroke_fill=stroke)


def make_atlas(name: str, fill: str, stroke: str) -> None:
    rows = (len(CHARS) + COLUMNS - 1) // COLUMNS
    image = Image.new("RGBA", (COLUMNS * CELL, rows * CELL), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    face = font(76)
    for index, character in enumerate(CHARS):
        x, y = index % COLUMNS, index // COLUMNS
        box = (x * CELL, y * CELL, (x + 1) * CELL, (y + 1) * CELL)
        draw_centered(draw, box, character, face, fill, stroke, 4)
    image.save(OUT / f"glyphs_{name}.png")


def make_preview() -> None:
    image = Image.new("RGB", (1120, 410), "#f6f0e2")
    draw = ImageDraw.Draw(image)
    face = font(67)
    small = font(50)
    for y, fill, stroke in [
        (36, "#603613", "#ffe4a6"),
        (215, "#fff5d6", "#69421f"),
    ]:
        draw.rounded_rectangle((25, y, 1095, y + 165), radius=28,
                               fill="#e8ca97", outline="#b87531", width=5)
        draw.text((60, y + 20), "MOVES 21", font=face, fill=fill,
                  stroke_width=3, stroke_fill=stroke)
        draw.text((585, y + 20), "CHAIN 3x", font=face, fill=fill,
                  stroke_width=3, stroke_fill=stroke)
        draw.text((66, y + 103), "0/5  0123456789  + x", font=small,
                  fill=fill, stroke_width=2, stroke_fill=stroke)
    image.save(OUT / "font_style_preview.png")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    make_atlas("dark", "#603613", "#ffe4a6")
    make_atlas("cream", "#fff5d6", "#69421f")
    (OUT / "glyph_manifest.json").write_text(
        json.dumps({
            "source_font": "Assets/cute_dino_2/Cute Dino.ttf",
            "cell_size": CELL,
            "columns": COLUMNS,
            "characters": CHARS,
            "indexing": "left-to-right, top-to-bottom; transparent atlas",
            "styles": ["dark", "cream"],
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    make_preview()
    metrics = {"advances": [font(76).getlength(c) for c in CHARS]}
    (ROOT / "Assets/Resources/WorkshopMatch/glyph_metrics.json").write_text(
        json.dumps(metrics) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
