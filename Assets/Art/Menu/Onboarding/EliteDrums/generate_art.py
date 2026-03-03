"""
Generate Elite Drums binding dialog art assets.

Creates EliteDrumKit.png (base kit diagram) and 13 highlight overlay PNGs.
All images are 1064x1126 RGBA, matching existing drum onboarding art dimensions.

Style: Clean line art with slightly isometric perspective to match existing
DrumKit.png / 5Lkit.png aesthetic. Uses ellipses for cymbals (tilted perspective),
circles for pads, and rounded shapes for pedals.
"""

from PIL import Image, ImageDraw
import os

W, H = 1064, 1126
LINE_COLOR = (40, 40, 40, 255)  # Near-black lines
LINE_WIDTH = 3
THIN_LINE = 2
HIGHLIGHT_COLOR = (255, 255, 255, 255)  # White — Unity tints at runtime
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# ──────────────────────────────────────────────
# Kit layout — positions and sizes for each piece
# Coordinates are (center_x, center_y) and dimensions
# Layout: drummer's perspective, slightly isometric
# ──────────────────────────────────────────────

PIECES = {
    # Cymbals — ellipses (wider than tall for perspective tilt)
    "LeftCrash":    {"type": "cymbal", "cx": 180, "cy": 170, "rx": 120, "ry": 55},
    "HiHat":        {"type": "cymbal", "cx": 370, "cy": 200, "rx": 110, "ry": 50},
    "Ride":         {"type": "cymbal", "cx": 750, "cy": 190, "rx": 115, "ry": 52},
    "RightCrash":   {"type": "cymbal", "cx": 920, "cy": 160, "rx": 115, "ry": 52},

    # Pads — circles (slightly elliptical for perspective)
    "Snare":  {"type": "pad", "cx": 310, "cy": 480, "rx": 95, "ry": 80},
    "Tom1":   {"type": "pad", "cx": 430, "cy": 340, "rx": 85, "ry": 72},
    "Tom2":   {"type": "pad", "cx": 600, "cy": 330, "rx": 85, "ry": 72},
    "Tom3":   {"type": "pad", "cx": 770, "cy": 470, "rx": 100, "ry": 85},

    # Pedals — rounded rectangles at the bottom
    "HiHatPedal": {"type": "pedal", "cx": 250, "cy": 920, "w": 100, "h": 180},
    "Kick":       {"type": "pedal", "cx": 530, "cy": 900, "w": 130, "h": 220},
}

# Cymbal stand positions (connect cymbals to ground)
STANDS = [
    # (top_x, top_y, bottom_x, bottom_y) — stand poles
    (180, 225, 200, 750),    # Left Crash stand
    (370, 250, 310, 700),    # Hi-Hat stand (goes to pedal)
    (750, 242, 720, 740),    # Ride stand
    (920, 212, 880, 740),    # Right Crash stand
    # Tom mount bar
    (430, 412, 430, 620),    # Tom1 leg
    (600, 402, 600, 620),    # Tom2 leg
    # Center stand (connects toms)
    (515, 620, 515, 770),    # Main vertical
]

# Tom mount crossbar
TOM_MOUNT = [(400, 420), (430, 420), (600, 410), (630, 410)]

# Stand feet / base
STAND_BASES = [
    # (cx, cy, rx, ry) — tripod feet indicators
    (200, 760, 60, 15),
    (515, 775, 70, 18),
    (720, 750, 55, 14),
    (880, 750, 55, 14),
]

# Snare stand
SNARE_STAND = [(310, 560, 310, 740), (310, 740, 60, 15)]

# Tom3 (floor tom) legs
TOM3_LEGS = [
    (730, 540, 700, 740),
    (810, 540, 840, 740),
]


def draw_cymbal(draw, cx, cy, rx, ry, line_color=LINE_COLOR, line_width=LINE_WIDTH):
    """Draw a cymbal with bell and edge lines."""
    # Main cymbal ellipse
    draw.ellipse(
        [cx - rx, cy - ry, cx + rx, cy + ry],
        outline=line_color, width=line_width
    )
    # Bell (small circle in center)
    bell_r = rx // 5
    bell_ry = ry // 5
    draw.ellipse(
        [cx - bell_r, cy - bell_ry, cx + bell_r, cy + bell_ry],
        outline=line_color, width=THIN_LINE
    )
    # Wing nut on top of bell
    draw.line([cx, cy - bell_ry - 6, cx, cy - bell_ry - 16], fill=line_color, width=THIN_LINE)
    draw.ellipse([cx - 4, cy - bell_ry - 20, cx + 4, cy - bell_ry - 14],
                 outline=line_color, width=1)


def fill_cymbal(draw, cx, cy, rx, ry):
    """Fill cymbal area for highlight."""
    draw.ellipse(
        [cx - rx, cy - ry, cx + rx, cy + ry],
        fill=HIGHLIGHT_COLOR
    )


def draw_pad(draw, cx, cy, rx, ry, line_color=LINE_COLOR, line_width=LINE_WIDTH):
    """Draw a drum pad with rim."""
    # Outer rim
    draw.ellipse(
        [cx - rx, cy - ry, cx + rx, cy + ry],
        outline=line_color, width=line_width
    )
    # Inner rim (head edge)
    inner_rx = int(rx * 0.82)
    inner_ry = int(ry * 0.82)
    draw.ellipse(
        [cx - inner_rx, cy - inner_ry, cx + inner_rx, cy + inner_ry],
        outline=line_color, width=THIN_LINE
    )


def fill_pad(draw, cx, cy, rx, ry):
    """Fill pad area for highlight."""
    draw.ellipse(
        [cx - rx, cy - ry, cx + rx, cy + ry],
        fill=HIGHLIGHT_COLOR
    )


def draw_pedal(draw, cx, cy, w, h, line_color=LINE_COLOR, line_width=LINE_WIDTH):
    """Draw a kick/hi-hat pedal with footboard and hinge."""
    x1, y1 = cx - w // 2, cy - h // 2
    x2, y2 = cx + w // 2, cy + h // 2

    # Base plate (wider, at bottom)
    base_y = y2 - h // 5
    draw.rounded_rectangle(
        [x1 - 10, base_y, x2 + 10, y2],
        radius=8, outline=line_color, width=line_width
    )

    # Footboard (narrower, angled)
    fb_left = x1 + 8
    fb_right = x2 - 8
    fb_top = y1
    fb_bottom = base_y - 5
    # Draw as slightly tapered shape
    points = [
        (fb_left + 10, fb_top),
        (fb_right - 10, fb_top),
        (fb_right, fb_bottom),
        (fb_left, fb_bottom),
    ]
    draw.polygon(points, outline=line_color, width=line_width)

    # Hinge at top
    draw.arc(
        [cx - 15, fb_top - 8, cx + 15, fb_top + 8],
        start=180, end=360, fill=line_color, width=THIN_LINE
    )

    # Chain/rod from footboard to beater
    draw.line([cx, fb_top - 4, cx, fb_top - 30], fill=line_color, width=THIN_LINE)


def fill_pedal(draw, cx, cy, w, h):
    """Fill pedal area for highlight."""
    x1, y1 = cx - w // 2 - 10, cy - h // 2
    x2, y2 = cx + w // 2 + 10, cy + h // 2
    draw.rounded_rectangle(
        [x1, y1, x2, y2],
        radius=10, fill=HIGHLIGHT_COLOR
    )


def draw_stand_line(draw, x1, y1, x2, y2, line_color=LINE_COLOR):
    """Draw a stand pole."""
    draw.line([x1, y1, x2, y2], fill=line_color, width=THIN_LINE)


def draw_stand_base(draw, cx, cy, rx, ry, line_color=LINE_COLOR):
    """Draw a tripod base indicator."""
    # Simple curved base
    draw.arc([cx - rx, cy - ry, cx + rx, cy + ry], start=0, end=180,
             fill=line_color, width=THIN_LINE)
    draw.line([cx - rx, cy, cx + rx, cy], fill=line_color, width=THIN_LINE)


def create_kit_image():
    """Create the base EliteDrumKit.png line art."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Draw stands first (behind everything)
    for x1, y1, x2, y2 in STANDS:
        draw_stand_line(draw, x1, y1, x2, y2)

    for cx, cy, rx, ry in STAND_BASES:
        draw_stand_base(draw, cx, cy, rx, ry)

    # Tom mount crossbar
    draw.line([430, 415, 600, 405], fill=LINE_COLOR, width=THIN_LINE)

    # Snare stand
    sx1, sy1, sx2, sy2 = SNARE_STAND[0]
    draw_stand_line(draw, sx1, sy1, sx2, sy2)
    scx, scy, srx, sry = SNARE_STAND[1]
    draw_stand_base(draw, scx, scy, srx, sry)

    # Tom3 legs
    for x1, y1, x2, y2 in TOM3_LEGS:
        draw_stand_line(draw, x1, y1, x2, y2)

    # Hi-hat stand connection to pedal
    draw_stand_line(draw, 310, 700, 250, 830)

    # Draw pedals
    for name in ["HiHatPedal", "Kick"]:
        p = PIECES[name]
        draw_pedal(draw, p["cx"], p["cy"], p["w"], p["h"])

    # Draw pads
    for name in ["Snare", "Tom1", "Tom2", "Tom3"]:
        p = PIECES[name]
        draw_pad(draw, p["cx"], p["cy"], p["rx"], p["ry"])

    # Draw cymbals (on top of everything)
    for name in ["LeftCrash", "HiHat", "Ride", "RightCrash"]:
        p = PIECES[name]
        draw_cymbal(draw, p["cx"], p["cy"], p["rx"], p["ry"])

    # Add labels (small, below each piece)
    try:
        from PIL import ImageFont
        font = ImageFont.truetype("arial.ttf", 18)
    except (OSError, ImportError):
        font = ImageFont.load_default()

    labels = {
        "LeftCrash": (180, 240), "HiHat": (370, 265), "Ride": (750, 258),
        "RightCrash": (920, 228), "Snare": (310, 575), "Tom1": (430, 425),
        "Tom2": (600, 415), "Tom3": (770, 570), "HiHatPedal": (250, 1025),
        "Kick": (530, 1025),
    }
    label_names = {
        "LeftCrash": "L. Crash", "HiHat": "Hi-Hat", "Ride": "Ride",
        "RightCrash": "R. Crash", "Snare": "Snare", "Tom1": "Tom 1",
        "Tom2": "Tom 2", "Tom3": "Tom 3", "HiHatPedal": "HH Pedal",
        "Kick": "Kick",
    }
    for name, (lx, ly) in labels.items():
        text = label_names[name]
        bbox = draw.textbbox((0, 0), text, font=font)
        tw = bbox[2] - bbox[0]
        draw.text((lx - tw // 2, ly), text, fill=LINE_COLOR, font=font)

    return img


def create_highlight(piece_name):
    """Create a highlight overlay PNG for a specific piece."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Map highlight names to physical pieces
    physical_map = {
        "Kick": "Kick",
        "Stomp": "HiHatPedal",
        "Splash": "HiHatPedal",
        "LeftCrash": "LeftCrash",
        "ClosedHiHat": "HiHat",
        "SizzleHiHat": "HiHat",
        "OpenHiHat": "HiHat",
        "Snare": "Snare",
        "Tom1": "Tom1",
        "Tom2": "Tom2",
        "Tom3": "Tom3",
        "Ride": "Ride",
        "RightCrash": "RightCrash",
    }

    phys = physical_map[piece_name]
    p = PIECES[phys]

    if p["type"] == "cymbal":
        fill_cymbal(draw, p["cx"], p["cy"], p["rx"], p["ry"])
    elif p["type"] == "pad":
        fill_pad(draw, p["cx"], p["cy"], p["rx"], p["ry"])
    elif p["type"] == "pedal":
        fill_pedal(draw, p["cx"], p["cy"], p["w"], p["h"])

    return img


def main():
    # Generate base kit image
    kit = create_kit_image()
    kit_path = os.path.join(OUTPUT_DIR, "EliteDrumKit.png")
    kit.save(kit_path)
    print(f"Created: {kit_path}")

    # Generate 13 highlight overlays
    highlights = [
        "Kick", "Stomp", "Splash", "LeftCrash",
        "ClosedHiHat", "SizzleHiHat", "OpenHiHat",
        "Snare", "Tom1", "Tom2", "Tom3",
        "Ride", "RightCrash",
    ]

    for name in highlights:
        hl = create_highlight(name)
        hl_path = os.path.join(OUTPUT_DIR, f"{name}.png")
        hl.save(hl_path)
        print(f"Created: {hl_path}")

    print(f"\nGenerated {1 + len(highlights)} images in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
