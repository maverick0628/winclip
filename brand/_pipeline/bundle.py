"""Bundle PNGs into ICO and convert installer images to BMP.

Requires: Pillow. Install with `python -m pip install pillow`.
"""
from __future__ import annotations

import struct
from pathlib import Path
from PIL import Image

BRAND = Path(__file__).resolve().parent.parent


def write_multi_ico(out_path: Path, sources: list[Path]) -> None:
    """Build a multi-resolution ICO from pre-rendered PNGs.

    Each entry is stored as PNG-compressed data inside the ICO. This preserves
    the per-size rendering quality (no Pillow downscaling)."""
    images = []
    for p in sources:
        img = Image.open(p).convert("RGBA")
        # Convert PNG bytes for embedding
        from io import BytesIO
        buf = BytesIO()
        img.save(buf, format="PNG")
        images.append((img.size, buf.getvalue()))

    # ICO header: reserved(2) type(2)=1 count(2)
    header = struct.pack("<HHH", 0, 1, len(images))
    dir_entries = b""
    payload = b""
    offset = 6 + 16 * len(images)
    for (w, h), data in images:
        # ICO directory entry: width(1) height(1) colors(1) reserved(1)
        # planes(2) bpp(2) size(4) offset(4)
        ww = 0 if w >= 256 else w
        hh = 0 if h >= 256 else h
        dir_entries += struct.pack(
            "<BBBBHHII", ww, hh, 0, 0, 1, 32, len(data), offset
        )
        offset += len(data)
        payload += data
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "wb") as f:
        f.write(header + dir_entries + payload)
    print(f"[bundle] WROTE {out_path.relative_to(BRAND)} ({sum(len(d) for _, d in images)} bytes)")


def png_to_bmp(src: Path, dst: Path, bg: tuple = (15, 23, 42)) -> None:
    """Convert PNG to BMP, flattening any alpha against `bg` (slate)."""
    img = Image.open(src).convert("RGBA")
    bg_img = Image.new("RGB", img.size, bg)
    bg_img.paste(img, mask=img.split()[3])
    dst.parent.mkdir(parents=True, exist_ok=True)
    bg_img.save(dst, format="BMP")
    print(f"[bundle] WROTE {dst.relative_to(BRAND)}")


def main():
    # 1) App icon ICO (multi-resolution from pre-rendered PNGs)
    icon_sizes = [16, 24, 32, 48, 64, 128, 256]
    sources = [BRAND / "app-icon" / "png" / f"icon-{s}.png" for s in icon_sizes]
    write_multi_ico(BRAND / "app-icon" / "WinClip.ico", sources)

    # 2) Installer .exe icon (same as app icon, copied for installer namespace)
    write_multi_ico(BRAND / "installer" / "setup-icon.ico", sources)

    # 3) Web favicon ICO (smaller subset: 16, 32, 48)
    fav_sources = [BRAND / "web" / "favicon" / f"favicon-{s}.png" for s in (16, 32, 48)]
    write_multi_ico(BRAND / "web" / "favicon" / "favicon.ico", fav_sources)

    # 4) Tray ICO bundles (one per theme)
    for theme in ("white", "dark", "active"):
        sources = [BRAND / "tray" / "transparent" / f"tray-{theme}-{s}.png" for s in (16, 20, 24, 32)]
        write_multi_ico(BRAND / "tray" / f"tray-{theme}.ico", sources)

    # 5) Inno Setup BMPs (must be Windows BMP, no alpha)
    png_to_bmp(BRAND / "installer" / "WizardImage.png", BRAND / "installer" / "WizardImage.bmp")
    png_to_bmp(BRAND / "installer" / "WizardSmallImage.png", BRAND / "installer" / "WizardSmallImage.bmp", bg=(255, 255, 255))


if __name__ == "__main__":
    main()
