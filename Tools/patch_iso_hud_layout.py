from pathlib import Path

path = Path(__file__).parents[1] / "Assets/IsoRPG/Scripts/IsoGame.cs"
text = path.read_text(encoding="utf-8")
text = text.replace("Screen.width / 2f", "viewWidth / 2f")
text = text.replace("Screen.height - 120", "viewHeight - 120")
text = text.replace("Screen.height - 30", "viewHeight - 30")
text = text.replace("Screen.height - 190", "viewHeight - 190")
path.write_text(text, encoding="utf-8", newline="\n")
