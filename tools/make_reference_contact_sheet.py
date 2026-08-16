from __future__ import annotations

import argparse
import importlib
import os
import re
import subprocess
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
PROJECT_DIR = TOOLS_DIR.parent
PILLOW_VERSION = "11.3.0"
PYTHON_TAG = f"cp{sys.version_info.major}{sys.version_info.minor}"
PILLOW_RUNTIME = (
    PROJECT_DIR / "build" / "python-runtime" / PYTHON_TAG / f"pillow-{PILLOW_VERSION}"
)


def load_pillow() -> None:
    """Load a Pillow wheel built for the interpreter used by run_python.ps1."""
    if os.name == "nt" and PILLOW_RUNTIME.exists():
        subprocess.run(
            ["icacls", str(PILLOW_RUNTIME), "/inheritance:e", "/T", "/C"],
            check=False,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    marker = next(PILLOW_RUNTIME.glob(f"PIL/_imaging.{PYTHON_TAG}-*.pyd"), None)
    if marker is None:
        PILLOW_RUNTIME.mkdir(parents=True, exist_ok=True)
        print("首次生成联系表：正在下载固定版本的 Pillow……", file=sys.stderr)
        environment = dict(os.environ)
        if os.name == "nt":
            environment.setdefault("SystemDrive", "C:")
            environment.setdefault("SystemRoot", r"C:\Windows")
            environment.setdefault("WINDIR", environment["SystemRoot"])
        subprocess.run(
            [
                sys.executable,
                "-m",
                "pip",
                "install",
                "--disable-pip-version-check",
                "--upgrade",
                "--target",
                str(PILLOW_RUNTIME),
                f"Pillow=={PILLOW_VERSION}",
            ],
            check=True,
            env=environment,
        )
        if os.name == "nt":
            subprocess.run(
                ["icacls", str(PILLOW_RUNTIME), "/inheritance:e", "/T", "/C"],
                check=False,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
    sys.path.insert(0, str(PILLOW_RUNTIME))
    importlib.invalidate_caches()


load_pillow()

from PIL import Image, ImageDraw, ImageFont


CAPTURE_RE = re.compile(r"^(?P<index>\d{3})_(?P<name>.+)_(?P<language>zh|en)_(?P<frame>\d+)\.png$")


def selected_frames(paths: list[Path], all_frames: bool = False) -> list[Path]:
    """Return every frame, or the first, middle, and last distinct frame."""
    paths.sort(key=lambda path: int(CAPTURE_RE.match(path.name).group("frame")))
    if all_frames:
        return paths
    indexes = sorted({0, len(paths) // 2, len(paths) - 1})
    return [paths[index] for index in indexes]


def selected_frame_numbers(numbers: set[int], all_frames: bool = False) -> list[int]:
    """Return every common frame, or the first, middle, and last ones."""
    ordered = sorted(numbers)
    if all_frames:
        return ordered
    indexes = sorted({0, len(ordered) // 2, len(ordered) - 1})
    return [ordered[index] for index in indexes]


def load_font(size: int) -> ImageFont.ImageFont:
    for candidate in (
        Path(r"C:\Windows\Fonts\msyh.ttc"),
        Path(r"C:\Windows\Fonts\arial.ttf"),
    ):
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def main() -> int:
    parser = argparse.ArgumentParser(
        description="把参考页截图整理为每页首/中/末帧联系表，便于人工版面审查。"
    )
    parser.add_argument("capture_dir", type=Path)
    parser.add_argument(
        "--language",
        choices=("zh", "en", "pair"),
        default="zh",
        help="pair 会把同页、同滚动帧的中文和英文左右并排。",
    )
    parser.add_argument("--page", action="append", default=[], help="精确页面名；可重复。")
    parser.add_argument(
        "--all-frames",
        action="store_true",
        help="联系表包含全部滚动帧，而不是只取首、中、末帧。",
    )
    parser.add_argument("--output", type=Path)
    parser.add_argument("--thumb-width", type=int, default=480)
    args = parser.parse_args()
    selected_pages = {page.casefold() for page in args.page}

    grouped: dict[tuple[int, str], dict[str, list[Path]]] = {}
    for path in args.capture_dir.glob("*.png"):
        match = CAPTURE_RE.match(path.name)
        if not match:
            continue
        language = match.group("language")
        if args.language != "pair" and language != args.language:
            continue
        name = match.group("name")
        if selected_pages and name.casefold() not in selected_pages:
            continue
        key = (int(match.group("index")), name)
        grouped.setdefault(key, {}).setdefault(language, []).append(path)

    if not grouped:
        raise SystemExit("没有找到符合条件的截图。")

    font = load_font(23)
    frame_font = load_font(18)
    label_height = 34
    margin = 12
    rows: list[tuple[str, list[Image.Image]]] = []

    def thumbnail(path: Path, corner_label: str) -> Image.Image:
        with Image.open(path) as source:
            height = round(source.height * args.thumb_width / source.width)
            thumb = source.convert("RGB").resize(
                (args.thumb_width, height), Image.Resampling.LANCZOS
            )
        draw = ImageDraw.Draw(thumb)
        draw.rectangle((0, 0, 142, 28), fill=(0, 0, 0))
        draw.text((5, 1), corner_label, font=frame_font, fill=(255, 255, 0))
        return thumb

    for (index, name), languages in sorted(grouped.items()):
        if args.language == "pair":
            by_language = {
                language: {
                    int(CAPTURE_RE.match(path.name).group("frame")): path
                    for path in paths
                }
                for language, paths in languages.items()
            }
            common = set(by_language.get("zh", {})) & set(by_language.get("en", {}))
            if not common:
                continue
            for frame in selected_frame_numbers(common, args.all_frames):
                rows.append(
                    (
                        f"{index:03d}  {name}  帧 {frame:02d}",
                        [
                            thumbnail(by_language["zh"][frame], "中文"),
                            thumbnail(by_language["en"][frame], "English"),
                        ],
                    )
                )
        else:
            paths = languages.get(args.language, [])
            thumbs = []
            for path in selected_frames(paths, args.all_frames):
                frame = int(CAPTURE_RE.match(path.name).group("frame"))
                thumbs.append(thumbnail(path, f"帧 {frame:02d}"))
            rows.append((f"{index:03d}  {name}", thumbs))

    if not rows:
        raise SystemExit("没有找到中英文帧号一致的成对截图。")

    column_count = 2 if args.language == "pair" else 3
    row_width = margin * (column_count + 1) + args.thumb_width * column_count
    row_heights = [label_height + max(image.height for image in images) + margin for _, images in rows]
    sheet = Image.new("RGB", (row_width, margin + sum(row_heights)), (28, 28, 28))
    draw = ImageDraw.Draw(sheet)
    y = margin
    for (label, images), row_height in zip(rows, row_heights):
        draw.text((margin, y), label, font=font, fill=(255, 255, 255))
        image_y = y + label_height
        for column, image in enumerate(images):
            x = margin + column * (args.thumb_width + margin)
            sheet.paste(image, (x, image_y))
        y += row_height

    output = args.output or args.capture_dir / f"contact-sheet-{args.language}.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)
    print(output.resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
