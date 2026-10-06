from pathlib import Path

path = Path(__file__).parents[1] / "Assets/IsoRPG/Scripts/IsoGame.cs"
lines = path.read_text(encoding="utf-8").splitlines()
for i, line in enumerate(lines):
    if line.lstrip().startswith("GUI.Label(new Rect(24, 82,"):
        lines[i] = line.replace("new Rect(24, 82, 220, 22)", "new Rect(45, 82, 205, 22)")
        lines.insert(i, "            DrawIcon(potionIcon, new Rect(24, 82, 17, 17));")
        break
path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
