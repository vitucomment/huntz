"""Generate deterministic, point-filtered pixel-art sheets and props for Huntz."""

from __future__ import annotations

import argparse
import math
import random
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageOps


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/Huntz/Art/Final"
SHARED = ART / "Shared"
TERRAIN = SHARED / "Terrain"
PROPS = SHARED / "Props"
VFX = SHARED / "VFX"
UI = SHARED / "UI"
HERO = ART / "Characters/Hero/Sheets"
NPC = ART / "Characters/NPC/Sheets"
CREATURES = ART / "Characters/Creatures/Sheets"
SLIME_SOURCE = ROOT / "Assets/Huntz/Art/Source/Characters/Creatures/AshSlime_Source.png"
WARDEN_SOURCE = ROOT / "Assets/Huntz/Art/Source/Characters/Creatures/RiftWarden_Source.png"
HERO_SOURCE = ROOT / "Assets/Huntz/Art/Source/Characters/Hero/Hero_Idle_8D_Source.png"
HERO_WALK_SOURCE = ROOT / "Assets/Huntz/Art/Source/Characters/Hero/WalkCycle/Hero_Walk_8D_Source.png"
HERO_IDLE_REVIEWED = ROOT / "Assets/Huntz/Art/Source/Characters/Hero/Hero_Idle_8D_Reviewed.png"
HERO_FINAL = HERO / "Hero_Idle_8D.png"

INK = "#211d2b"
INK_SOFT = "#342c3a"
HERO_SOURCE_FINAL = ROOT / "Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Idle_8D.png"


def rgba(hex_color: str, alpha: int = 255) -> tuple[int, int, int, int]:
    h = hex_color.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), alpha


def save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def tile_diamond(name: str, colors: tuple[str, ...], seed: int, pattern: str) -> Image.Image:
    rng = random.Random(seed)
    image = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    diamond = [(31, 0), (63, 15), (32, 31), (0, 16)]
    draw.polygon(diamond, fill=rgba(colors[0]))
    draw.line(diamond + [diamond[0]], fill=rgba(INK), width=1, joint="curve")

    if pattern in ("grass", "dark_grass", "arena"):
        for _ in range(54):
            x, y = rng.randrange(3, 61), rng.randrange(3, 29)
            if abs(x - 31) / 31 + abs(y - 15) / 15 > 0.88:
                continue
            color = colors[1] if rng.random() < 0.68 else colors[2]
            if rng.random() < 0.26:
                draw.rectangle((x, y, x + rng.choice((1, 1, 2)), y), fill=rgba(color))
            if pattern != "arena" and rng.random() < 0.16:
                draw.point((x, y - 1), fill=rgba(colors[3]))
        for _ in range(4 if pattern == "grass" else 2):
            x, y = rng.randrange(8, 56), rng.randrange(7, 25)
            if abs(x - 31) / 31 + abs(y - 15) / 15 < 0.78:
                draw.point((x, y), fill=rgba(colors[3]))
                draw.point((x + 1, y - 1), fill=rgba(colors[3]))
    elif pattern in ("path", "sand", "ash"):
        for _ in range(66):
            x, y = rng.randrange(3, 61), rng.randrange(3, 29)
            if abs(x - 31) / 31 + abs(y - 15) / 15 > 0.9:
                continue
            col = rng.choice(colors[1:])
            draw.rectangle((x, y, x + rng.choice((0, 1, 2)), y + rng.choice((0, 0, 1))), fill=rgba(col))
        if pattern == "path":
            for x, y in ((15, 13), (38, 19), (47, 10)):
                draw.rectangle((x, y, x + 2, y + 1), fill=rgba(colors[-1]))
    elif pattern == "plaza":
        draw.line([(13, 8), (30, 16), (21, 21)], fill=rgba(colors[1]), width=1)
        draw.line([(32, 16), (48, 8)], fill=rgba(colors[1]), width=1)
        draw.line([(32, 17), (42, 23)], fill=rgba(colors[1]), width=1)
        for x, y in ((20, 12), (41, 16), (28, 23)):
            draw.point((x, y), fill=rgba(colors[2]))
    elif pattern == "water":
        for band in range(4):
            y = 6 + band * 6
            start_x = 13 + (band % 2) * 7
            draw.line([(start_x, y), (start_x + 7, y)], fill=rgba(colors[1]), width=1)
            draw.point((start_x + 8, y + 1), fill=rgba(colors[2]))
        for _ in range(9):
            x, y = rng.randrange(10, 53), rng.randrange(5, 27)
            if abs(x - 31) / 31 + abs(y - 15) / 15 < 0.7:
                draw.point((x, y), fill=rgba(colors[2]))
    return image


def make_tiles() -> None:
    specs = [
        ("Grass", ("#456d45", "#58824e", "#38583e", "#9ab76a"), "grass"),
        ("Grass_Dark", ("#2d4d3b", "#3d6346", "#254138", "#65865a"), "dark_grass"),
        ("Trail_Dirt", ("#8a6845", "#a77e4f", "#705238", "#c09a61"), "path"),
        ("Village_Stone", ("#7b8380", "#929b95", "#666e70", "#b4b8a8"), "plaza"),
        ("Pond_Water", ("#255b70", "#34798a", "#68aeb0", "#152f4a"), "water"),
        ("Shore_Sand", ("#b69d65", "#cbb679", "#947d52", "#e0c98b"), "sand"),
        ("Mainland_Grass", ("#3e665b", "#517f68", "#31534d", "#93ad78"), "grass"),
        ("Hunt_Ash", ("#353b42", "#4a5054", "#292d38", "#76706b"), "ash"),
        ("Hunt_Sanctuary", ("#52605d", "#6c7b72", "#3c494c", "#a1aa8a"), "plaza"),
        ("Hunt_Arena", ("#454b45", "#535b50", "#373d40", "#957f62"), "arena"),
    ]
    for index, (name, colors, pattern) in enumerate(specs):
        save(tile_diamond(name, colors, 721 + index, pattern), TERRAIN / f"{name}.png")


def outlined_polygon(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill: str, outline: str = INK, width: int = 2) -> None:
    draw.polygon(points, fill=rgba(fill))
    draw.line(points + [points[0]], fill=rgba(outline), width=width, joint="curve")


def make_tree() -> Image.Image:
    im = Image.new("RGBA", (64, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((9, 80, 56, 91), fill=rgba("#10191e", 100))
    d.rectangle((28, 53, 37, 83), fill=rgba("#694332"))
    d.rectangle((30, 56, 34, 81), fill=rgba("#946044"))
    for box, fill in [((8, 25, 39, 67), "#254a3b"), ((24, 15, 55, 62), "#315b43"), ((12, 7, 48, 48), "#46704b")]:
        d.ellipse(box, fill=rgba(INK))
        d.ellipse((box[0] + 2, box[1] + 2, box[2] - 2, box[3] - 2), fill=rgba(fill))
    for x, y, col in ((18, 22, "#6a8c55"), (31, 15, "#7b9a59"), (40, 28, "#527d50"), (23, 39, "#527d50"), (36, 48, "#244738")):
        d.rectangle((x, y, x + 4, y + 2), fill=rgba(col))
    return im


def make_rock() -> Image.Image:
    im = Image.new("RGBA", (64, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((7, 37, 58, 46), fill=rgba("#10191e", 90))
    outlined_polygon(d, [(8, 35), (12, 19), (24, 8), (40, 7), (55, 18), (60, 34), (50, 41), (18, 42)], "#4b5456")
    d.polygon([(13, 31), (17, 20), (27, 11), (38, 12), (34, 26), (23, 35)], fill=rgba("#727e7b"))
    d.polygon([(29, 12), (40, 11), (53, 20), (55, 31), (42, 30), (34, 24)], fill=rgba("#626d6f"))
    d.line([(16, 32), (26, 29), (29, 22)], fill=rgba("#a4aaa0"), width=2)
    d.point((43, 20), fill=rgba("#b1b5a8"))
    return im


def make_house(roof: str, plaster: str, trim: str) -> Image.Image:
    im = Image.new("RGBA", (96, 88), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((9, 72, 88, 85), fill=rgba("#10191e", 90))
    outlined_polygon(d, [(18, 38), (48, 22), (78, 38), (78, 73), (48, 84), (18, 69)], plaster)
    d.polygon([(18, 38), (48, 22), (48, 58), (18, 69)], fill=rgba("#d5c49a"))
    d.polygon([(48, 22), (78, 38), (78, 73), (48, 58)], fill=rgba("#b49c76"))
    outlined_polygon(d, [(10, 39), (47, 7), (87, 37), (77, 44), (47, 20), (19, 47)], roof)
    d.line([(16, 40), (47, 14), (82, 39)], fill=rgba(trim), width=2)
    d.rectangle((31, 48, 41, 67), fill=rgba("#50372d"), outline=rgba(INK))
    d.rectangle((24, 42, 33, 51), fill=rgba("#d39b4e"), outline=rgba(INK))
    d.rectangle((59, 42, 69, 50), fill=rgba("#86b7b0"), outline=rgba(INK))
    d.point((38, 58), fill=rgba("#e2bb64"))
    return im


def make_well() -> Image.Image:
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((6, 49, 59, 60), fill=rgba("#10191e", 95))
    d.ellipse((8, 30, 56, 51), fill=rgba(INK))
    d.polygon([(11, 37), (30, 46), (30, 55), (10, 47)], fill=rgba("#777f7e"))
    d.polygon([(30, 46), (54, 36), (54, 46), (30, 55)], fill=rgba("#555f61"))
    d.ellipse((11, 27, 53, 46), fill=rgba("#aaa99a"), outline=rgba(INK), width=2)
    d.ellipse((16, 31, 48, 42), fill=rgba("#255a73"), outline=rgba("#383847"), width=1)
    d.line([(17, 16), (17, 31)], fill=rgba("#694936"), width=3)
    d.line([(47, 16), (47, 31)], fill=rgba("#694936"), width=3)
    d.line([(14, 15), (50, 15)], fill=rgba("#594033"), width=3)
    return im


def make_chest() -> Image.Image:
    im = Image.new("RGBA", (56, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((5, 39, 52, 47), fill=rgba("#11191d", 100))
    outlined_polygon(d, [(8, 23), (27, 14), (49, 24), (49, 39), (28, 46), (8, 37)], "#6c4430")
    d.polygon([(8, 23), (27, 14), (49, 24), (29, 33)], fill=rgba("#ae7043"))
    d.polygon([(8, 23), (29, 33), (29, 45), (8, 37)], fill=rgba("#835334"))
    d.polygon([(29, 33), (49, 24), (49, 39), (29, 45)], fill=rgba("#5a3a32"))
    d.line([(8, 23), (8, 16), (27, 8), (49, 17), (49, 24)], fill=rgba(INK), width=3)
    d.polygon([(8, 16), (27, 8), (49, 17), (29, 26)], fill=rgba("#c18a51"))
    d.rectangle((27, 28, 32, 37), fill=rgba("#e8bd56"), outline=rgba(INK))
    return im


def make_portal_frame(frame: int) -> Image.Image:
    im = Image.new("RGBA", (96, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # Stone plinth and broken astral arch.
    outlined_polygon(d, [(13, 106), (47, 94), (83, 107), (48, 120)], "#51445c")
    d.polygon([(13, 106), (48, 120), (48, 127), (13, 113)], fill=rgba("#332d40"))
    d.polygon([(48, 120), (83, 107), (83, 114), (48, 127)], fill=rgba("#44394f"))
    d.ellipse((18, 15, 79, 104), outline=rgba("#352b4d"), width=10)
    d.arc((19, 13, 78, 104), 175, 358, fill=rgba("#9a77ca"), width=5)
    d.arc((19, 13, 78, 104), 0, 175, fill=rgba("#54b4b4"), width=4)
    d.arc((25, 21, 72, 98), 170 + frame * 9, 520 + frame * 9, fill=rgba("#d2b777"), width=2)
    d.ellipse((27, 24, 70, 96), fill=rgba("#392455"))
    d.ellipse((31 + (frame % 2) * 2, 29, 66, 89), fill=rgba("#69459b"))
    d.ellipse((37, 33 + (frame % 3), 59, 82), fill=rgba("#345b79"))
    d.arc((33, 30, 68, 88), 210 + frame * 12, 445 + frame * 12, fill=rgba("#7ed1c4"), width=3)
    # Feet, brass caps and two shifting sparks.
    d.rectangle((12, 103, 24, 113), fill=rgba("#c49757"), outline=rgba(INK))
    d.rectangle((72, 104, 84, 114), fill=rgba("#c49757"), outline=rgba(INK))
    for x, y in (((30, 44), (66, 67)), ((65, 48), (35, 74)), ((39, 56), (61, 38)), ((55, 76), (42, 35)))[frame]:
        d.point((x, y), fill=rgba("#e6d38f"))
        d.point((x + 1, y), fill=rgba("#e6d38f"))
    return im


def make_forge() -> Image.Image:
    im = Image.new("RGBA", (80, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((4, 52, 76, 62), fill=rgba("#10191e", 105))
    outlined_polygon(d, [(8, 34), (39, 24), (72, 36), (42, 47)], "#5d4638")
    d.polygon([(8, 34), (42, 47), (42, 54), (8, 41)], fill=rgba("#49392f"))
    d.polygon([(42, 47), (72, 36), (72, 43), (42, 54)], fill=rgba("#392f32"))
    d.polygon([(18, 27), (48, 22), (61, 27), (54, 31), (45, 30), (40, 35), (28, 35), (23, 32), (15, 32)], fill=rgba("#9aa39d"), outline=rgba(INK))
    d.rectangle((33, 34, 46, 43), fill=rgba("#687780"), outline=rgba(INK))
    d.line([(11, 31), (18, 28)], fill=rgba("#d1b673"), width=2)
    d.point((57, 24), fill=rgba("#ecd17e"))
    return im


def make_core_frame(frame: int) -> Image.Image:
    im = Image.new("RGBA", (32, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shift = (frame % 2)
    pts = [(16, 2), (27, 12), (24, 37), (16, 46), (7, 37), (4, 13)]
    outlined_polygon(d, pts, "#387c88", outline=INK, width=2)
    d.polygon([(16, 3), (24, 13), (19, 36), (16, 42)], fill=rgba("#79d2c7"))
    d.polygon([(16, 3), (11, 14), (14, 39), (16, 42)], fill=rgba("#427f91"))
    d.line([(11, 14), (16, 18 + shift), (21, 13)], fill=rgba("#d8dcad"), width=2)
    d.point((16, 24 - shift), fill=rgba("#fff0ab"))
    return im


def make_sword_arc_frame(frame: int) -> Image.Image:
    im = Image.new("RGBA", (72, 72), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    start = 208 + frame * 15
    end = min(start + 74, 355)
    box = (5, 4, 67, 66)
    d.arc(box, start, end, fill=rgba("#dba659", 245), width=6)
    d.arc((10, 9, 62, 61), start + 5, end - 4, fill=rgba("#fff1b5", 230), width=3)
    angle = math.radians(start)
    x, y = 36 + int(math.cos(angle) * 30), 36 + int(math.sin(angle) * 30)
    d.point((x, y), fill=rgba("#fff7d8"))
    return im


def make_ui_panel() -> Image.Image:
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((1, 1, 62, 62), fill=rgba("#302b3b"), outline=rgba("#171924"), width=2)
    d.rectangle((4, 4, 59, 59), outline=rgba("#806750"), width=2)
    d.rectangle((8, 8, 55, 55), outline=rgba("#4b5260"), width=1)
    for x, y in ((4, 4), (56, 4), (4, 56), (56, 56)):
        d.rectangle((x, y, x + 3, y + 3), fill=rgba("#d1aa63"))
    d.line((13, 10, 50, 10), fill=rgba("#a27c53"), width=1)
    return im


def make_circle_sprite(ring: bool = False) -> Image.Image:
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if ring:
        d.ellipse((3, 3, 60, 60), outline=rgba("#ffffff"), width=4)
        d.ellipse((9, 9, 54, 54), outline=rgba("#ffffff", 120), width=1)
    else:
        d.ellipse((3, 3, 60, 60), fill=rgba("#ffffff"))
    return im


def make_shadow_sprite() -> Image.Image:
    im = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for inset, alpha in ((0, 38), (3, 46), (7, 34), (11, 20)):
        d.ellipse((inset, 4 + inset // 3, 63 - inset, 27 - inset // 3), fill=(8, 13, 19, alpha))
    return im


def make_boundary() -> Image.Image:
    im = Image.new("RGBA", (64, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 4, 63, 18), fill=rgba("#303941"), outline=rgba(INK))
    for x in (4, 19, 36, 52):
        d.rectangle((x, 6, x + 8, 12), fill=rgba("#59656a"), outline=rgba("#242832"))
        d.point((x + 2, 7), fill=rgba("#86918b"))
    d.line((0, 4, 63, 4), fill=rgba("#9b987e"), width=2)
    d.line((0, 17, 63, 17), fill=rgba("#1a232d"), width=2)
    return im


def make_icons() -> None:
    heart = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(heart)
    d.polygon([(2, 5), (2, 3), (6, 1), (8, 4), (10, 1), (14, 3), (14, 6), (8, 14)], fill=rgba("#c34853"), outline=rgba(INK))
    d.point((5, 4), fill=rgba("#f39a88"))
    core = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    c = ImageDraw.Draw(core)
    outlined_polygon(c, [(8, 1), (14, 5), (12, 13), (8, 15), (3, 12), (2, 5)], "#6fc8c0", INK, 1)
    c.line((5, 5, 9, 9), fill=rgba("#eff1bd"), width=2)
    blade = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    b = ImageDraw.Draw(blade)
    b.line((4, 13, 12, 3), fill=rgba("#e6e1cb"), width=3)
    b.line((3, 12, 7, 15), fill=rgba("#bf9555"), width=2)
    b.point((12, 3), fill=rgba("#fff7cf"))
    save(heart, UI / "Icon_Heart.png")
    save(core, UI / "Icon_RiftCore.png")
    save(blade, UI / "Icon_Sword.png")

    flower = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    fd = ImageDraw.Draw(flower)
    for box in ((6, 1, 9, 5), (2, 5, 6, 8), (9, 5, 13, 8), (4, 9, 7, 12), (8, 9, 11, 12)):
        fd.rectangle(box, fill=rgba("#f4e5b4"), outline=rgba(INK))
    fd.rectangle((6, 5, 9, 8), fill=rgba("#dba94e"), outline=rgba(INK))
    fd.rectangle((7, 8, 8, 12), fill=rgba("#537c4c"))
    fd.rectangle((4, 11, 11, 13), fill=rgba("#537c4c"))
    save(flower, PROPS / "Meadow_Flower.png")

    marker = Image.new("RGBA", (16, 24), (0, 0, 0, 0))
    md = ImageDraw.Draw(marker)
    md.polygon([(8, 1), (14, 7), (8, 13), (2, 7)], fill=rgba("#f2c85c"), outline=rgba(INK))
    md.rectangle((7, 4, 8, 9), fill=rgba("#fff3c3"))
    md.point((8, 11), fill=rgba("#fff3c3"))
    md.rectangle((7, 15, 8, 20), fill=rgba("#f2c85c"), outline=rgba(INK))
    md.ellipse((6, 21, 9, 23), fill=rgba("#d99f3f"), outline=rgba(INK))
    save(marker, PROPS / "Quest_Marker.png")

    potion = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    pd = ImageDraw.Draw(potion)
    pd.rectangle((12, 2, 19, 7), fill=rgba("#c7b187"), outline=rgba(INK))
    pd.rectangle((10, 7, 21, 10), fill=rgba("#75604f"), outline=rgba(INK))
    outlined_polygon(pd, [(10, 10), (21, 10), (25, 24), (21, 29), (9, 29), (5, 24)], "#405872")
    pd.polygon([(9, 20), (22, 17), (24, 24), (21, 28), (9, 28), (6, 24)], fill=rgba("#67b5db"))
    pd.line((9, 12, 12, 11), fill=rgba("#d6eff4"), width=2)
    save(potion, UI / "Icon_Potion.png")


def make_static_art() -> None:
    save(make_tree(), PROPS / "Tree.png")
    save(make_rock(), PROPS / "Rock.png")
    save(make_house("#934844", "#d6bd90", "#dbbc72"), PROPS / "House_RedRoof.png")
    save(make_house("#48647e", "#beb8a8", "#d0b66e"), PROPS / "House_BlueRoof.png")
    save(make_well(), PROPS / "Village_Well.png")
    save(make_chest(), PROPS / "Treasure_Chest.png")
    save(make_forge(), PROPS / "Forge_Table.png")
    save(make_ui_panel(), UI / "Panel_Frame.png")
    save(make_circle_sprite(), VFX / "Telegraph_Fill.png")
    save(make_circle_sprite(ring=True), VFX / "Telegraph_Ring.png")
    save(make_shadow_sprite(), VFX / "PixelShadow.png")
    save(Image.new("RGBA", (8, 8), (255, 255, 255, 255)), VFX / "SolidPixel.png")
    save(make_boundary(), PROPS / "HuntBoundary.png")
    for frame in range(4):
        save(make_portal_frame(frame), PROPS / f"PortalHZ_F{frame:02}.png")
        save(make_core_frame(frame), PROPS / f"RiftCore_F{frame:02}.png")
        save(make_sword_arc_frame(frame), VFX / f"SwordArc_F{frame:02}.png")
    save(tile_diamond("", ("#4c5a59", "#728b7c", "#384647", "#b0b391"), 615, "plaza"), VFX / "TelegraphDiamond.png")
    save(tile_diamond("", ("#ac4439", "#e76e47", "#622e3b", "#ffd16e"), 616, "ash"), VFX / "ImpactBurstBase.png")


def direction_vectors() -> list[tuple[float, float]]:
    # Matches rows in HuntzAnimationAssetBuilder: S, SW, W, NW, N, NE, E, SE.
    return [(0, 1), (-1, 0.5), (-1, 0), (-1, -0.5), (0, -1), (1, -0.5), (1, 0), (1, 0.5)]


def extract_hero_cells(source: Image.Image) -> list[list[Image.Image]]:
    _, height = source.size
    rows = []
    for direction in range(8):
        y0, y1 = round(direction * height / 8), round((direction + 1) * height / 8)
        frames = []
        for frame in range(4):
            # The authored sheet has four 201x181 frames centered from x=240 onward,
            # with 140px of transparent lead-in and 142px of trailing padding.
            # Split those actual cells instead of dividing the full atlas width evenly.
            x0 = 140 + frame * 201
            crop = source.crop((x0, y0, x0 + 201, y1))
            cell = Image.new("RGBA", (208, 192), (0, 0, 0, 0))
            cell.alpha_composite(crop, (4, 11))
            frames.append(cell)
        rows.append(frames)
    return rows


def add_pixel_sword(frame: Image.Image, direction: int, swing_degrees: float = 0) -> None:
    d = ImageDraw.Draw(frame)
    dx, dy = direction_vectors()[direction]
    norm = math.hypot(dx, dy) or 1
    dx, dy = dx / norm, dy / norm
    side_x, side_y = dy, -dx
    center_x, hand_y = 104, 108
    hx = int(center_x + side_x * 28 + dx * 4)
    hy = int(hand_y - dy * 6)
    base_angle = math.atan2(-dy, dx) + math.radians(swing_degrees)
    tip_x = hx + int(math.cos(base_angle) * 24)
    tip_y = hy - int(math.sin(base_angle) * 24)
    # Dark silhouette, steel blade, and a brass guard/hilt.
    d.line((hx, hy, tip_x, tip_y), fill=rgba(INK), width=6)
    inner_x = hx + int((tip_x - hx) * 0.82)
    inner_y = hy + int((tip_y - hy) * 0.82)
    d.line((hx + 1, hy - 1, inner_x, inner_y), fill=rgba("#d7e3db"), width=3)
    d.line((inner_x, inner_y, tip_x, tip_y), fill=rgba("#fff1bd"), width=2)
    guard_x, guard_y = hx - int(math.sin(base_angle) * 4), hy - int(math.cos(base_angle) * 4)
    d.line((guard_x - 4, guard_y + 4, guard_x + 4, guard_y - 4), fill=rgba("#d2a858"), width=4)
    d.line((hx, hy, hx - int(math.cos(base_angle) * 6), hy + int(math.sin(base_angle) * 6)), fill=rgba("#70422f"), width=4)
    d.point((hx, hy), fill=rgba("#f1d578"))


def armed_idle(rows: list[list[Image.Image]]) -> Image.Image:
    w, h = rows[0][0].size
    sheet = Image.new("RGBA", (w * 4, h * 8), (0, 0, 0, 0))
    for direction, frames in enumerate(rows):
        for frame_index, frame in enumerate(frames):
            frame = frame.copy()
            add_pixel_sword(frame, direction, swing_degrees=-8 if frame_index in (1, 2) else 0)
            sheet.alpha_composite(frame, (frame_index * w, direction * h))
    return sheet


def make_walk() -> Image.Image:
    # Keep the reviewed limb poses. Translating pieces of Idle produces sliding,
    # and must never silently replace the authored animation on a full rebuild.
    if not HERO_WALK_SOURCE.exists():
        raise FileNotFoundError("Import the Hero WalkCycle manifest before rebuilding hero art.")
    with Image.open(HERO_WALK_SOURCE) as source:
        sheet = source.convert("RGBA")
    if sheet.size != (208 * 8, 192 * 8):
        raise ValueError(f"Hero Walk requires eight 208 x 192 frames per direction: {sheet.size}")
    return sheet


def make_attack(rows: list[list[Image.Image]]) -> Image.Image:
    cell_w, cell_h = rows[0][0].size
    sheet = Image.new("RGBA", (cell_w * 6, cell_h * 8), (0, 0, 0, 0))
    lunge = (0, 1, 3, 5, 2, 0)
    swing = (-45, -20, 15, 55, 30, -15)
    for direction, idle_frames in enumerate(rows):
        dx, dy = direction_vectors()[direction]
        for frame_no in range(6):
            frame = idle_frames[frame_no % 4].copy()
            shifted = Image.new("RGBA", frame.size, (0, 0, 0, 0))
            shifted.alpha_composite(frame, (int(dx * lunge[frame_no]), -int(dy * lunge[frame_no])))
            add_pixel_sword(shifted, direction, swing[frame_no])
            sheet.alpha_composite(shifted, (frame_no * cell_w, direction * cell_h))
    return sheet


def make_dodge(rows: list[list[Image.Image]]) -> Image.Image:
    cell_w, cell_h = rows[0][0].size
    sheet = Image.new("RGBA", (cell_w * 4, cell_h * 8), (0, 0, 0, 0))
    progress = (0, 3, 7, 11)
    for direction, idle_frames in enumerate(rows):
        dx, dy = direction_vectors()[direction]
        for frame_no in range(4):
            frame = idle_frames[frame_no % 4].copy()
            translated = Image.new("RGBA", frame.size, (0, 0, 0, 0))
            translated.alpha_composite(frame, (int(dx * progress[frame_no]), -int(dy * progress[frame_no])))
            if frame_no < 3:
                ghost = Image.new("RGBA", frame.size, (0, 0, 0, 0))
                ghost.alpha_composite(frame, (-int(dx * (4 + frame_no * 2)), int(dy * (4 + frame_no * 2))))
                ghost.putalpha(ghost.getchannel("A").point(lambda value: int(value * (0.19 - frame_no * 0.04))))
                translated.alpha_composite(ghost)
            sheet.alpha_composite(translated, (frame_no * cell_w, direction * cell_h))
    return sheet


def make_hurt(rows: list[list[Image.Image]]) -> Image.Image:
    cell_w, cell_h = rows[0][0].size
    sheet = Image.new("RGBA", (cell_w * 2, cell_h * 8), (0, 0, 0, 0))
    for direction, idle_frames in enumerate(rows):
        dx, _ = direction_vectors()[direction]
        for frame_no in range(2):
            frame = idle_frames[(frame_no + 1) % 4].copy()
            frame = ImageOps.colorize(frame.convert("L"), black="#632d42", white="#ffd0a4").convert("RGBA")
            source = idle_frames[(frame_no + 1) % 4]
            frame.putalpha(source.getchannel("A"))
            shaken = Image.new("RGBA", frame.size, (0, 0, 0, 0))
            shaken.alpha_composite(frame, (-int(dx * (4 if frame_no == 0 else 1)), 0))
            sheet.alpha_composite(shaken, (frame_no * cell_w, direction * cell_h))
    return sheet


def make_death(rows: list[list[Image.Image]]) -> Image.Image:
    cell_w, cell_h = rows[0][0].size
    sheet = Image.new("RGBA", (cell_w * 8, cell_h * 8), (0, 0, 0, 0))
    angles = (0, 4, 12, 27, 48, 68, 83, 90)
    for direction, idle_frames in enumerate(rows):
        for frame_no, angle in enumerate(angles):
            frame = idle_frames[frame_no % 4]
            rotated = frame.rotate(angle, resample=Image.Resampling.NEAREST, expand=True)
            bounds = rotated.getchannel("A").getbbox()
            if bounds is None:
                continue
            rotated = rotated.crop(bounds)
            if frame_no > 4:
                scale = 1 - (frame_no - 4) * 0.045
                rotated = rotated.resize((max(1, round(rotated.width * scale)),
                                          max(1, round(rotated.height * scale))), Image.Resampling.NEAREST)
            fit = min(1.0, (cell_w - 12) / rotated.width, (cell_h - 12) / rotated.height)
            if fit < 1.0:
                rotated = rotated.resize((max(1, round(rotated.width * fit)),
                                          max(1, round(rotated.height * fit))), Image.Resampling.NEAREST)
            canvas = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
            canvas.alpha_composite(rotated, ((cell_w - rotated.width) // 2, cell_h - 8 - rotated.height))
            if frame_no == 7:
                canvas.putalpha(canvas.getchannel("A").point(lambda value: int(value * 0.64)))
            sheet.alpha_composite(canvas, (frame_no * cell_w, direction * cell_h))
    return sheet


def palette_variant(source: Image.Image, tunic: tuple[str, str, str], hair: tuple[str, str], skin: tuple[str, str]) -> Image.Image:
    image = source.copy()
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if not a:
                continue
            # The teal tunic, violet-black hair and warm skin are deliberately narrow ramps.
            if g > r * 1.1 and b > r * 0.9 and g > 65 and b > 65:
                intensity = (r + g + b) / 3
                pixels[x, y] = rgba(tunic[0] if intensity < 60 else tunic[1] if intensity < 120 else tunic[2], a)
            elif r < 95 and b > r * 1.12 and b > g * 0.88:
                intensity = (r + g + b) / 3
                pixels[x, y] = rgba(hair[0] if intensity < 42 else hair[1], a)
            elif r > 160 and g > 75 and r > b * 1.22:
                pixels[x, y] = rgba(skin[0] if g < 145 else skin[1], a)
    return image


def make_npc_variants(rows: list[list[Image.Image]]) -> None:
    NPC.mkdir(parents=True, exist_ok=True)
    for name, tunic, hair, skin in [
        ("Ana", ("#583a6a", "#805a8c", "#b58bb2"), ("#574855", "#b9a7a5"), ("#a46552", "#dbad82")),
        ("Bruno", ("#593d2d", "#806043", "#a97d4c"), ("#231d24", "#594637"), ("#98553f", "#d0946b")),
    ]:
        cell_w, cell_h = rows[0][0].size
        sheet = Image.new("RGBA", (cell_w * 4, cell_h * 8), (0, 0, 0, 0))
        for direction, frames in enumerate(rows):
            for frame_no, frame in enumerate(frames):
                variant = palette_variant(frame, tunic, hair, skin)
                sheet.alpha_composite(variant, (frame_no * cell_w, direction * cell_h))
        save(sheet, NPC / f"{name}_Idle_8D.png")


def load_hero_rows() -> list[list[Image.Image]]:
    if not HERO_SOURCE.exists() and HERO_FINAL.exists():
        HERO_SOURCE.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(HERO_FINAL, HERO_SOURCE)
    if not HERO_SOURCE.exists():
        raise FileNotFoundError(f"Missing the approved warrior sheet: {HERO_SOURCE}")
    original = Image.open(HERO_SOURCE).convert("RGBA")
    return extract_hero_cells(original)


def make_hero_sheets() -> None:
    rows = load_hero_rows()
    HERO.mkdir(parents=True, exist_ok=True)
    if HERO_IDLE_REVIEWED.exists():
        with Image.open(HERO_IDLE_REVIEWED) as reviewed_idle:
            save(reviewed_idle.convert("RGBA"), HERO_FINAL)
    else:
        save(armed_idle(rows), HERO_FINAL)
    save(make_walk(), HERO / "Hero_Walk_8D.png")
    save(make_attack(rows), HERO / "Hero_Attack_8D.png")
    save(make_dodge(rows), HERO / "Hero_Dodge_8D.png")
    save(make_hurt(rows), HERO / "Hero_Hurt_8D.png")
    save(make_death(rows), HERO / "Hero_Death_8D.png")
    make_npc_variants(rows)


def make_hero_death_sheet() -> None:
    HERO.mkdir(parents=True, exist_ok=True)
    save(make_death(load_hero_rows()), HERO / "Hero_Death_8D.png")
    print(f"Generated {HERO / 'Hero_Death_8D.png'}")


def make_slime_fallback() -> None:
    """Create a local fallback while the final PixelLab slime image is downloaded."""
    frames = []
    for phase in range(4):
        im = Image.new("RGBA", (64, 56), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        squash = (0, 2, 0, -1)[phase]
        d.ellipse((9, 39, 56, 51), fill=rgba("#111a1b", 80))
        outlined_polygon(d, [(9, 41+squash), (8, 31+squash), (14, 18+squash), (23, 11+squash), (40, 10+squash), (51, 20+squash), (57, 33+squash), (54, 43+squash), (43, 49+squash), (21, 49+squash)], "#4b6958")
        d.polygon([(12, 35+squash), (17, 21+squash), (25, 14+squash), (36, 16+squash), (34, 30+squash)], fill=rgba("#779079"))
        d.polygon([(33, 31+squash), (52, 26+squash), (54, 40+squash), (44, 47+squash), (29, 47+squash)], fill=rgba("#385746"))
        d.rectangle((19, 31+squash, 23, 37+squash), fill=rgba("#efb85b"), outline=rgba(INK))
        d.rectangle((39, 31+squash, 43, 37+squash), fill=rgba("#efb85b"), outline=rgba(INK))
        d.point((20, 32+squash), fill=rgba("#241e29"))
        d.point((40, 32+squash), fill=rgba("#241e29"))
        frames.append(im)
    sheet = Image.new("RGBA", (64*4, 56), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index*64, 0))
    save(sheet, CREATURES / "AshSlime_Idle_4F_Fallback.png")


def anchored_variant(source: Image.Image, width: int, height: int, sx: float = 1.0, sy: float = 1.0,
                     offset_x: int = 0, offset_y: int = 0, opacity: float = 1.0) -> Image.Image:
    scaled = source.resize((max(1, round(source.width * sx)), max(1, round(source.height * sy))), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    x = (width - scaled.width) // 2 + offset_x
    y = height - scaled.height + offset_y
    canvas.alpha_composite(scaled, (x, y))
    if opacity != 1.0:
        canvas.putalpha(canvas.getchannel("A").point(lambda value: int(value * opacity)))
    return canvas


def tinted_copy(source: Image.Image, color: str, mix: float = 0.3) -> Image.Image:
    overlay = Image.new("RGBA", source.size, rgba(color))
    alpha = source.getchannel("A")
    overlay.putalpha(alpha.point(lambda value: int(value * mix)))
    out = source.copy()
    out.alpha_composite(overlay)
    return out


def save_sequence(frames: list[Image.Image], path: Path) -> None:
    width, height = frames[0].size
    sheet = Image.new("RGBA", (width * len(frames), height), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index * width, 0))
    save(sheet, path)


def make_creature_sheets() -> None:
    slime_path = SLIME_SOURCE
    if slime_path.exists():
        slime = Image.open(slime_path).convert("RGBA")
        save_sequence([anchored_variant(slime, 64, 56, sy=s) for s in (0.92, 1.0, 1.08, 1.0)],
                      CREATURES / "AshSlime_Idle_4F.png")
        walk = []
        for i, (sy, dx) in enumerate(((0.86, -2), (0.96, -1), (1.08, 0), (1.0, 1), (0.9, 2), (1.0, 0))):
            frame = anchored_variant(slime, 64, 56, sx=1.0 + (0.025 if i % 2 else 0), sy=sy, offset_x=dx)
            if i in (2, 3):
                frame = tinted_copy(frame, "#9ccf79", 0.09)
            walk.append(frame)
        save_sequence(walk, CREATURES / "AshSlime_Move_6F.png")
        save_sequence([tinted_copy(slime, "#fff0ba", 0.58), tinted_copy(slime, "#e99b69", 0.22)],
                      CREATURES / "AshSlime_Hurt_2F.png")
        dead = [anchored_variant(slime, 64, 56, sx=1.0 - i * 0.1, sy=1.0 - i * 0.12,
                                  offset_y=0, opacity=max(0.0, 1.0 - i * 0.17)) for i in range(6)]
        save_sequence(dead, CREATURES / "AshSlime_Death_6F.png")
        king = tinted_copy(slime, "#8c54bd", 0.65)
        king = king.resize((80, 70), Image.Resampling.NEAREST)
        kd = ImageDraw.Draw(king)
        kd.polygon([(29, 12), (33, 1), (39, 10), (46, 0), (50, 11), (58, 3), (59, 18)], fill=rgba("#e3b955"), outline=rgba(INK))
        king_sheet = Image.new("RGBA", (80 * 4, 70), (0, 0, 0, 0))
        for i, frame in enumerate((king, king.transpose(Image.Transpose.FLIP_LEFT_RIGHT), king, king.transpose(Image.Transpose.FLIP_LEFT_RIGHT))):
            king_sheet.alpha_composite(frame, (i * 80, 0))
        save(king_sheet, CREATURES / "SlimeKing_Idle_4F.png")
        king_walk = [anchored_variant(king, 80, 70, sx=sx, sy=sy, offset_x=dx)
                     for sx, sy, dx in ((0.96, 0.92, -2), (1.0, 0.98, -1), (1.04, 1.08, 0),
                                        (1.02, 1.05, 1), (0.96, 0.94, 2), (1.0, 1.0, 0))]
        save_sequence(king_walk, CREATURES / "SlimeKing_Move_6F.png")
        save_sequence([tinted_copy(king, "#fff0ba", 0.58), tinted_copy(king, "#eea4ca", 0.34)],
                      CREATURES / "SlimeKing_Hurt_2F.png")
        king_death = [anchored_variant(king, 80, 70, sx=1.0 - i * 0.1, sy=1.0 - i * 0.12,
                                       opacity=max(0.0, 1.0 - i * 0.17)) for i in range(6)]
        save_sequence(king_death, CREATURES / "SlimeKing_Death_6F.png")
    else:
        fallback = Image.open(CREATURES / "AshSlime_Idle_4F_Fallback.png").convert("RGBA")
        save(fallback, CREATURES / "AshSlime_Idle_4F.png")

    warden_path = WARDEN_SOURCE
    if warden_path.exists():
        warden = Image.open(warden_path).convert("RGBA")
        idle = [anchored_variant(warden, 112, 128, sx=sx, sy=sy, offset_y=0)
                for sx, sy in ((1.0, 1.0), (1.02, 0.99), (1.0, 1.02), (0.98, 1.0))]
        save_sequence(idle, CREATURES / "RiftWarden_Idle_4F.png")
        charge = []
        for i in range(6):
            frame = anchored_variant(warden, 112, 128, sx=1.0 + i * 0.015, sy=1.0 - i * 0.008,
                                     offset_x=round(math.sin(i / 5 * math.pi) * 5), offset_y=-i)
            charge.append(tinted_copy(frame, "#bd7fbd", 0.12 + i * 0.025))
        save_sequence(charge, CREATURES / "RiftWarden_Charge_6F.png")
        pulse = [tinted_copy(anchored_variant(warden, 112, 128, sx=s, sy=s), "#a855d2", 0.12 + i * 0.11)
                 for i, s in enumerate((0.96, 1.0, 1.06, 1.12, 1.06, 1.0))]
        save_sequence(pulse, CREATURES / "RiftWarden_Pulse_6F.png")
        recover = [anchored_variant(warden, 112, 128, sx=s, sy=s, opacity=op)
                   for s, op in ((1.06, 1.0), (1.0, 1.0), (0.96, 0.9), (1.0, 1.0))]
        save_sequence(recover, CREATURES / "RiftWarden_Recover_4F.png")
        save_sequence([tinted_copy(anchored_variant(warden, 112, 128, offset_x=-3), "#fff0dc", 0.76),
                       tinted_copy(anchored_variant(warden, 112, 128, offset_x=2), "#e17272", 0.42)],
                      CREATURES / "RiftWarden_Hurt_2F.png")
        death = [anchored_variant(warden, 112, 128, sy=1.0 - i * 0.06,
                                  offset_x=(i % 2) * 2, opacity=max(0.0, 1.0 - i * 0.13)) for i in range(8)]
        save_sequence(death, CREATURES / "RiftWarden_Death_8F.png")


def make_combined_effect_sheets() -> None:
    for base_name, folder, file_prefix, count in (
        ("PortalHZ_Loop_4F.png", PROPS, "PortalHZ_F", 4),
        ("RiftCore_Idle_4F.png", PROPS, "RiftCore_F", 4),
        ("SwordArc_4F.png", VFX, "SwordArc_F", 4),
    ):
        frames = [Image.open(folder / f"{file_prefix}{index:02}.png").convert("RGBA") for index in range(count)]
        save_sequence(frames, folder / base_name)


def build() -> None:
    for folder in (TERRAIN, PROPS, VFX, UI, HERO, NPC, CREATURES):
        folder.mkdir(parents=True, exist_ok=True)
    make_tiles()
    make_static_art()
    make_icons()
    make_hero_sheets()
    make_slime_fallback()
    make_creature_sheets()
    make_combined_effect_sheets()
    print(f"Generated Huntz pixel art under {ART}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Build the Huntz pixel art asset pack.")
    parser.add_argument("--only", choices=("hero-death", "hero-walk"), help="Regenerate only one targeted asset.")
    args = parser.parse_args()
    if args.only == "hero-death":
        make_hero_death_sheet()
    elif args.only == "hero-walk":
        save(make_walk(), HERO / "Hero_Walk_8D.png")
    else:
        build()
