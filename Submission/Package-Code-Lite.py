"""Build a compact, reproducible CatchLab source archive (under 10 MB)."""

from __future__ import annotations

import re
import subprocess
import tempfile
import zipfile
from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "CatchLabFriend"
ASSETS = PROJECT / "Assets"
OUTPUT = ROOT / "Submission" / "code.zip"
MAX_BYTES = 10_000_000
GUID = re.compile(rb"\bguid:\s*([0-9a-fA-F]{32})\b")
META_GUID = re.compile(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")


def add_tree(selected: set[Path], folder: Path) -> None:
    selected.update(p for p in folder.rglob("*") if p.is_file())


def collect_unity_assets() -> tuple[set[Path], set[str]]:
    guid_to_asset: dict[str, Path] = {}
    for meta in ASSETS.rglob("*.meta"):
        match = META_GUID.search(meta.read_bytes()[:256])
        if match:
            guid_to_asset[match.group(1).decode().lower()] = meta.with_suffix("")

    selected: set[Path] = set()
    queue: deque[Path] = deque()
    missing: set[str] = set()

    def add_asset(path: Path) -> None:
        if path in selected:
            return
        if not path.is_file():
            raise FileNotFoundError(path)
        selected.add(path)
        meta = Path(str(path) + ".meta")
        if meta.exists():
            selected.add(meta)
        queue.append(path)

    add_asset(ASSETS / "Scenes" / "SampleScene.unity")
    for script in ASSETS.rglob("*.cs"):
        if "Plugins" not in script.relative_to(ASSETS).parts:
            add_asset(script)
    for folder in (ASSETS / "Resources", ASSETS / "WebGLTemplates"):
        for path in folder.rglob("*"):
            if path.is_file() and not path.name.endswith(".meta"):
                add_asset(path)
    for relative in (
        "FriendCoins/S_ItemNoOutline_CoinGold_00.png",
        "FriendCoins/S_ItemNoOutline_CoinRed_00.png",
        "Player_Sprite/Fuel_icon.png",
        "GeneratedItems/ItemUnlit.mat",
        "Square.prefab",
        "Square (1).prefab",
        "Square (2).prefab",
    ):
        add_asset(ASSETS / relative)

    for project_setting in (PROJECT / "ProjectSettings").rglob("*"):
        if project_setting.is_file():
            for match in GUID.finditer(project_setting.read_bytes()):
                asset = guid_to_asset.get(match.group(1).decode().lower())
                if asset is not None and asset.is_file():
                    add_asset(asset)

    while queue:
        path = queue.popleft()
        if path.suffix.lower() in {".png", ".jpg", ".jpeg", ".mp3", ".wav", ".ttf", ".otf", ".dll"}:
            continue
        for match in GUID.finditer(path.read_bytes()):
            key = match.group(1).decode().lower()
            asset = guid_to_asset.get(key)
            if asset is not None and asset.is_file():
                add_asset(asset)
            elif asset is None:
                missing.add(key)

    # Folder .meta files preserve asset folder identity when Unity imports them.
    for path in list(selected):
        parent = path.parent
        while parent != ASSETS and ASSETS in parent.parents:
            meta = Path(str(parent) + ".meta")
            if meta.exists():
                selected.add(meta)
            parent = parent.parent
    return selected, missing


def main() -> None:
    output = OUTPUT.resolve()
    if ROOT.resolve() not in output.parents:
        raise RuntimeError("Output must stay inside workspace")

    selected, external_guids = collect_unity_assets()
    for folder in ("Packages", "ProjectSettings"):
        add_tree(selected, PROJECT / folder)
    for name in ("README_TH.md", "SCORING_STATUS_TH.md"):
        selected.add(PROJECT / name)
    for name in (
        "bridge.cjs", "game-status.cjs", "rounds.cjs", "sheets.cjs", "sync-sheets.cjs",
        "game-status.test.cjs", "rounds.test.cjs", "sheets.test.cjs",
        "package.json", "package-lock.json", ".env.example",
    ):
        selected.add(ROOT / "ControllerBridge" / name)
    for name in ("CatchController.ino", "GameLcd.h", "LcdConfig.h", "Settings.example.h"):
        selected.add(ROOT / "Firmware" / "CatchController" / name)
    selected.add(ROOT / "Firmware" / "SensorCheck" / "SensorCheck.ino")
    selected.add(ROOT / "Submission" / "README_CODE_TH.md")

    names = {path.relative_to(ROOT).as_posix(): path for path in selected if path.is_file()}
    names["README_CODE_TH.md"] = names.pop("Submission/README_CODE_TH.md")
    for library in ("PubSubClient", "hd44780"):
        folder = Path.home() / "Documents" / "Arduino" / "libraries" / library
        for path in folder.rglob("*"):
            if path.is_file():
                names[f"ArduinoLibraries/{library}/{path.relative_to(folder).as_posix()}"] = path
    for name in names:
        if re.search(r"(^|/)(Settings\.h|\.env\.local|node_modules|Library|Temp|Logs)(/|$)|\.pem$|service-account.*\.json$", name, re.I):
            raise RuntimeError(f"Private or generated file selected: {name}")

    with tempfile.TemporaryDirectory(prefix="catchlab_zip_") as tmp:
        # Preserve sheet dimensions, alpha and Unity sprite slice metadata.
        sheet = "CatchLabFriend/Assets/Player_Sprite/My_first_asset_spritesheet(2).png"
        if sheet in names:
            compact_sheet = Path(tmp) / "spritesheet.png"
            with Image.open(names[sheet]) as image:
                image.convert("RGBA").quantize(
                    colors=256, method=Image.Quantize.FASTOCTREE,
                    dither=Image.Dither.NONE,
                ).save(compact_sheet, optimize=True)
            names[sheet] = compact_sheet

        # The original background track is ten minutes. A shorter loop keeps the
        # scene audio reference and .meta GUID while fitting the rubric's limit.
        music = "CatchLabFriend/Assets/Sounds/Mii Editor - Mii Maker (Wii U) Music.mp3"
        replacement = Path(tmp) / "game_music.mp3"
        if music in names:
            subprocess.run(
                ["ffmpeg", "-nostdin", "-hide_banner", "-loglevel", "error", "-y",
                 "-i", str(names[music]), "-t", "75", "-ac", "1", "-b:a", "48k", str(replacement)],
                check=True,
            )
            names[music] = replacement

        with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
            for name, path in sorted(names.items()):
                archive.write(path, name)

    size = output.stat().st_size
    print(f"Created {output} ({len(names)} files, {size:,} bytes; limit {MAX_BYTES:,})")
    print(f"Unity external/builtin GUIDs: {len(external_guids)}")
    if size > MAX_BYTES:
        raise RuntimeError(f"Archive exceeds 10 MB by {size - MAX_BYTES:,} bytes")
    with zipfile.ZipFile(output) as archive:
        bad = archive.testzip()
        if bad:
            raise RuntimeError(f"ZIP checksum failed: {bad}")


if __name__ == "__main__":
    main()
