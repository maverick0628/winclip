#!/usr/bin/env python3
"""
Create placeholder image assets for WinClip installer.
Generates simple colored placeholder images in PNG and ICO formats.
"""
import struct
import zlib
import os

def create_png(width, height, color_rgb, output_path):
    """Create a simple PNG file with a solid color and text overlay."""

    # PNG signature
    png_signature = b'\x89PNG\r\n\x1a\n'

    # Create IHDR chunk (Image Header)
    ihdr_data = struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0)
    ihdr_crc = zlib.crc32(b'IHDR' + ihdr_data) & 0xffffffff
    ihdr_chunk = struct.pack('>I', 13) + b'IHDR' + ihdr_data + struct.pack('>I', ihdr_crc)

    # Create image data - simple solid color
    raw_data = b''
    r, g, b = color_rgb
    for y in range(height):
        # Filter byte (0 = none) + RGB pixels
        row = b'\x00' + (bytes([r, g, b]) * width)
        raw_data += row

    # Compress image data
    compressed_data = zlib.compress(raw_data, 9)

    # Create IDAT chunk (Image Data)
    idat_crc = zlib.crc32(b'IDAT' + compressed_data) & 0xffffffff
    idat_chunk = struct.pack('>I', len(compressed_data)) + b'IDAT' + compressed_data + struct.pack('>I', idat_crc)

    # Create IEND chunk (Image End)
    iend_crc = zlib.crc32(b'IEND') & 0xffffffff
    iend_chunk = struct.pack('>I', 0) + b'IEND' + struct.pack('>I', iend_crc)

    # Write PNG file
    with open(output_path, 'wb') as f:
        f.write(png_signature)
        f.write(ihdr_chunk)
        f.write(idat_chunk)
        f.write(iend_chunk)

    print(f"Created: {output_path} ({width}x{height})")

def create_ico_from_png(png_path, ico_path):
    """Create a simple ICO file from PNG data."""
    # Read the PNG file
    with open(png_path, 'rb') as f:
        png_data = f.read()

    # ICO header
    ico_header = struct.pack('<HHH', 0, 1, 1)  # Reserved, Type (1=ICO), Count

    # Icon directory entry
    # Width, Height, Colors, Reserved, Planes, BitsPerPixel, SizeInBytes, Offset
    icon_dir_entry = struct.pack('<BBBBHHII',
        0, 0,  # 0 means 256x256
        0,     # 0 = no palette
        0,     # reserved
        1,     # color planes
        32,    # bits per pixel
        len(png_data),  # size of image data
        22     # offset to image data (6 byte header + 16 byte directory entry)
    )

    # Write ICO file
    with open(ico_path, 'wb') as f:
        f.write(ico_header)
        f.write(icon_dir_entry)
        f.write(png_data)

    print(f"Created: {ico_path}")

def main():
    """Generate all required placeholder assets."""

    # Ensure Assets directory exists
    assets_dir = os.path.join('installer', 'Assets')
    os.makedirs(assets_dir, exist_ok=True)

    # Color scheme for WinClip (blue theme for clipboard app)
    primary_color = (41, 128, 185)  # Professional blue

    # Create PNG assets
    create_png(50, 50, primary_color, os.path.join(assets_dir, 'StoreLogo.png'))
    create_png(44, 44, primary_color, os.path.join(assets_dir, 'Square44x44Logo.png'))
    create_png(150, 150, primary_color, os.path.join(assets_dir, 'Square150x150Logo.png'))
    create_png(310, 150, primary_color, os.path.join(assets_dir, 'Wide310x150Logo.png'))

    # Create a 256x256 PNG first for the ICO
    temp_png = os.path.join(assets_dir, 'temp_256.png')
    create_png(256, 256, primary_color, temp_png)

    # Create ICO file from 256x256 PNG
    ico_path = os.path.join(assets_dir, 'WinClip.ico')
    create_ico_from_png(temp_png, ico_path)

    # Remove temporary PNG
    os.remove(temp_png)

    print("\n✓ All placeholder assets created successfully!")
    print("\nNote: These are simple placeholder images with solid colors.")
    print("Replace them with professional designs before release.")

if __name__ == '__main__':
    main()
