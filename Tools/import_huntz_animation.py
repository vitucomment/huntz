"""Download, review and pack an authored animation for Huntz.

Reads a checked-in manifest and keeps original generated frames under Art/Source.
No generation API or credentials are used here. Final is written only with --apply.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import urllib.request
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DIRECTIONS = ("South", "SouthWest", "West", "NorthWest", "North", "NorthEast", "East", "SouthEast")


def project_path(value: str) -> Path:
    path = (ROOT / value).resolve()
    if not path.is_relative_to(ROOT):
        raise ValueError(f"Path escapes the project: {value}")
    return path


def resolve_reference_path(manifest_path: Path, value: str) -> Path:
    if value.replace("\\", "/").startswith("Assets/"):
        return project_path(value)
    path = (manifest_path.parent / value).resolve()
    if not path.is_relative_to(manifest_path.parent.resolve()):
        raise ValueError("Reference path escapes the manifest directory.")
    return path


def assemble(manifest_path: Path, download: bool, apply: bool) -> None:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    frame_count = manifest["frame_count"]
    width, height = manifest["source_frame_size"]
    scale = manifest["integer_scale"]
    entries = {entry["direction"]: entry for entry in manifest["directions"]}
    base_sheet = manifest.get("base_sheet")
    layout = manifest.get("layout", "directional")
    if layout not in {"directional", "single_direction"}:
        raise ValueError("Layout must be 'directional' or 'single_direction'.")
    if not entries or not set(entries).issubset(DIRECTIONS):
        raise ValueError("The manifest contains no directions or an unknown direction.")
    if layout == "directional" and base_sheet is None and set(entries) != set(DIRECTIONS):
        raise ValueError("The manifest must contain all eight directions exactly once.")
    if layout == "single_direction" and (base_sheet is not None or len(entries) != 1):
        raise ValueError("A single-direction manifest contains exactly one direction and no base sheet.")
    if len(manifest["directions"]) != len(entries):
        raise ValueError("Duplicate directions in the manifest.")
    if frame_count < 2 or scale < 1:
        raise ValueError("At least two frames and a positive integer scale are required.")

    review_dir = ROOT / "Library/HuntzArtReview" / manifest["name"]
    review_dir.mkdir(parents=True, exist_ok=True)
    output_directions = DIRECTIONS if layout == "directional" else list(entries)
    sheet_rows = 8 if layout == "directional" else 1
    sheet_size = (width * frame_count * scale, height * sheet_rows * scale)
    sheet = Image.open(project_path(base_sheet)).convert("RGBA") if base_sheet else Image.new("RGBA", sheet_size)
    if sheet.size != sheet_size:
        raise ValueError(f"Base sheet size differs from the manifest: {sheet.size}")
    all_frames = []
    hashes = {}
    for row, direction in enumerate(output_directions):
        if direction not in entries:
            all_frames.append([sheet.crop((index * width * scale, row * height * scale,
                                          (index + 1) * width * scale, (row + 1) * height * scale))
                               for index in range(frame_count)])
            continue
        entry = entries[direction]
        folder = manifest_path.parent / entry.get("folder", direction)
        if not folder.resolve().is_relative_to(manifest_path.parent.resolve()):
            raise ValueError("Frame folder escapes the manifest directory.")
        folder.mkdir(parents=True, exist_ok=True)
        reference_path = resolve_reference_path(
            manifest_path, entry.get("reference", f"Reference_{direction}.png"))
        reference = Image.open(reference_path).convert("RGBA")
        baseline = manifest.get("ground_line", reference.getchannel("A").getbbox()[3])
        if not 0 < baseline <= height:
            raise ValueError("The ground line must be inside the source canvas.")

        download_format = entry.get("download_format", "pixellab_frames")
        if download_format not in {"pixellab_frames", "workbench_grid"}:
            raise ValueError(f"Unknown download format: {download_format}")
        if download_format == "workbench_grid" and download:
            grid_columns = entry.get("grid_columns", 0)
            if not 1 <= grid_columns <= frame_count:
                raise ValueError(f"Invalid workbench grid column count for {direction}.")
            grid_path = folder / "WorkbenchSheet.png"
            refresh_workbench = manifest.get("refresh_downloads", False)
            if not grid_path.exists() or refresh_workbench:
                url = entry["download_url"]
                if not url.startswith("https://api.pixellab.ai/mcp/pixel-tools/"):
                    raise ValueError("Unexpected PixelLab workbench download host.")
                with urllib.request.urlopen(url, timeout=60) as response:
                    grid_path.write_bytes(response.read())
            with Image.open(grid_path) as source_grid:
                source_grid = source_grid.convert("RGBA")
                grid_rows = (frame_count + grid_columns - 1) // grid_columns
                expected_size = (width * scale * grid_columns, height * scale * grid_rows)
                if source_grid.size != expected_size:
                    raise ValueError(f"Unexpected workbench sheet size for {direction}: {source_grid.size}")
                for index in range(frame_count):
                    path = folder / f"F{index:02}.png"
                    if path.exists() and not refresh_workbench:
                        continue
                    column = index % grid_columns
                    grid_row = index // grid_columns
                    frame = source_grid.crop((column * width * scale, grid_row * height * scale,
                                              (column + 1) * width * scale,
                                              (grid_row + 1) * height * scale))
                    frame.save(path)

        frames = []
        # Generation sequences reserve frame zero for the input; workbench grids contain authored frames only.
        for index in range(1, frame_count + 1):
            path = folder / f"F{index - 1:02}.png"
            if download and not path.exists():
                if download_format == "workbench_grid":
                    raise FileNotFoundError(f"Workbench frame extraction failed: {path}")
                url = entry["download_url"].format(index=index)
                if not url.startswith("https://api.pixellab.ai/mcp/images/"):
                    raise ValueError("Unexpected generation download host.")
                with urllib.request.urlopen(url, timeout=60) as response:
                    payload = response.read()
                path.write_bytes(payload)
            with Image.open(path) as source:
                frame = source.convert("RGBA")
            if frame.size != (width, height):
                raise ValueError(f"Unexpected size in {path}: {frame.size}")
            bounds = frame.getchannel("A").getbbox()
            if bounds is None:
                raise ValueError(f"Empty frame: {path}")
            hashes[str(path.relative_to(ROOT)).replace("\\", "/")] = hashlib.sha256(path.read_bytes()).hexdigest()
            # Preserve the body pose and optionally align the ground contact.
            aligned = Image.new("RGBA", (width, height))
            offset_y = baseline - bounds[3] if manifest.get("align_ground", True) else 0
            if bounds[1] + offset_y < 0:
                raise ValueError(f"Foot alignment would clip the head in {path}")
            aligned.alpha_composite(frame, (0, offset_y))
            aligned = aligned.resize((width * scale, height * scale), Image.Resampling.NEAREST)
            sheet.paste(aligned, ((index - 1) * width * scale, row * height * scale))
            frames.append(aligned)
        minimum_unique_frames = manifest.get("minimum_unique_frames", 2)
        if minimum_unique_frames < 1:
            raise ValueError("The minimum unique frame count must be positive.")
        if len({hashlib.sha256(frame.tobytes()).hexdigest() for frame in frames}) < minimum_unique_frames:
            raise ValueError(f"All frames are identical for {direction}")
        all_frames.append(frames)

    sheet.save(review_dir / "contact_sheet.png")
    preview_columns = min(4, len(output_directions))
    preview_rows = (len(output_directions) + preview_columns - 1) // preview_columns
    preview_frames = []
    for index in range(frame_count):
        canvas = Image.new("RGB", (width * scale * preview_columns,
                                   (height * scale + 22) * preview_rows), "#242e3a")
        draw = ImageDraw.Draw(canvas)
        for row, direction in enumerate(output_directions):
            x, y = (row % preview_columns) * width * scale, (row // preview_columns) * (height * scale + 22)
            canvas.paste(all_frames[row][index], (x, y + 22), all_frames[row][index])
            draw.text((x + 4, y + 3), direction, fill="white")
        preview_frames.append(canvas)
    review_name = manifest.get("review_gif", "walk_review.gif")
    preview_frames[0].save(review_dir / review_name, save_all=True, append_images=preview_frames[1:],
                           duration=round(1000 / manifest["fps"]), loop=0, disposal=2)
    (review_dir / "frame_hashes.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")
    if apply:
        for destination in (manifest["source_sheet"], manifest["final_sheet"]):
            path = project_path(destination)
            path.parent.mkdir(parents=True, exist_ok=True)
            sheet.save(path)
        print(f"Applied {manifest['name']}: {len(entries)} directions x {frame_count} frames.")
    print(f"Review: {review_dir}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--download", action="store_true", help="Download missing original frames.")
    parser.add_argument("--apply", action="store_true", help="Write reviewed sheets to Source and Final, keeping .meta files.")
    arguments = parser.parse_args()
    assemble(arguments.manifest.resolve(), arguments.download, arguments.apply)
