from pathlib import Path

path = Path(__file__).parents[1] / "Assets/HuntzSlice/Scripts/HuntzSliceHud.cs"
lines = path.read_text(encoding="utf-8").splitlines()
for i, line in enumerate(lines):
    if line.lstrip().startswith("GUI.Label(new Rect(28, 97,") or line.lstrip().startswith("GUI.Label(new Rect(28, 115,"):
        lines[i] = line.replace("GUI.Label(", "LabelWithIcon(", 1).replace(", small);", ", coreIcon, small);")
    elif line.lstrip().startswith("GUI.Label(new Rect(28, 120,") or line.lstrip().startswith("GUI.Label(new Rect(28, 136,"):
        lines[i] = line.replace("GUI.Label(", "LabelWithIcon(", 1).replace(", small);", ", swordIcon, small);")
text = "\n".join(lines) + "\n"
marker = "    void DrawIcon(Texture2D icon, Rect rect)"
helper = '''    void LabelWithIcon(Rect rect, string value, Texture2D icon, GUIStyle style)
    {
        DrawIcon(icon, new Rect(rect.x, rect.y + 1f, 17f, 17f));
        GUI.Label(new Rect(rect.x + 22f, rect.y, rect.width - 22f, rect.height), value, style);
    }

'''
text = text.replace(marker, helper + marker, 1)
path.write_text(text, encoding="utf-8", newline="\n")
