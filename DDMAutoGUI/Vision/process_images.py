#!/usr/bin/env python3
"""
Resize images, run PaddleOCR, save results and timing info to JSON,
then delete temporary _compressed files.
"""

from pathlib import Path
from PIL import Image
import json
import os
import argparse
import time
from datetime import datetime

# Must be set before importing PaddleOCR
os.environ["PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK"] = "1"
from paddleocr import PaddleOCR

IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".tif", ".tiff"}

def find_images(folder: Path):
    return sorted(p for p in folder.iterdir() if p.suffix.lower() in IMAGE_EXTS and p.is_file())


def compressed_path(src: Path):
    return src.with_name(f"{src.stem}_compressed{src.suffix}")


def compress_image(src: Path, factor: int = 5):
    """Resize image and return (dst_path, compress_seconds)."""
    start = time.perf_counter()
    dst = compressed_path(src)
    if not dst.exists():
        with Image.open(src) as img:
            new_size = (max(1, img.width // factor), max(1, img.height // factor))
            resized = img.resize(new_size, Image.Resampling.BILINEAR)
            resized.save(dst)
    elapsed = time.perf_counter() - start
    return dst, round(elapsed, 3)


def run_ocr_on_images(ocr, images_info, score_threshold: float = 0.4):
    """
    images_info is a list of dicts: {"path": Path, "compress_time": float}
    Returns (results_list, per_image_timings)
    """
    results = []
    timings = []
    for info in images_info:
        img_path = info["path"]
        print(f"Processing: {img_path.name}")
        start = time.perf_counter()
        try:
            raw = ocr.predict(str(img_path))
        except Exception as exc:
            print(f"  OCR failed for {img_path.name}: {exc}")
            raw = None
        ocr_time = round(time.perf_counter() - start, 3)

        img_result = {"file": img_path.name, "strings": []}
        if raw:
            for page in raw:
                if isinstance(page, dict) and "rec_texts" in page and "rec_scores" in page:
                    for text, score in zip(page["rec_texts"], page["rec_scores"]):
                        if score > score_threshold:
                            img_result["strings"].append({"text": text, "score": float(f"{score:.3f}")})

        results.append(img_result)
        timings.append({"file": img_path.name, "compress_time_seconds": info["compress_time"], "ocr_time_seconds": ocr_time})
    return results, timings

def parse_args():
    parser = argparse.ArgumentParser(
        description="Run PaddleOCR on all images in a folder and write results to a JSON file."
    )

    parser.add_argument(
        "--input-folder",
        type=Path,
        required=True,
        help="Directory containing images to OCR"
    )

    parser.add_argument(
        "--output-file",
        type=Path,
        required=True,
        help="Output JSON file path"
    )
    
    parser.add_argument(
        "--factor", 
        type=int, 
        default=4, 
        help="Downscale factor (integer)")

    parser.add_argument(
        "--min-score",
        type=float,
        default=0.10,
        help="Minimum recognition confidence score to keep (default: 0.10)"
    )

    return parser.parse_args()


def main():
    args = parse_args()

    overall_start = time.perf_counter()
    start_iso = datetime.now().isoformat()

    input_folder = args.input_folder
    if not input_folder.exists() or not input_folder.is_dir():
        raise SystemExit(f"Input folder does not exist or is not a directory: {input_folder}")

    images = find_images(input_folder)
    print(f"Found {len(images)} images in {input_folder}. Resizing images...")

    compressed_infos = []
    for img in images:
        try:
            dst, compress_time = compress_image(img, factor=args.factor)
            compressed_infos.append({"path": dst, "compress_time": compress_time})
        except Exception as exc:
            print(f"  Failed to compress {img.name}: {exc}")

    print(f"Resized {len(compressed_infos)} images. Initializing OCR...")

    # Initialize OCR
    ocr = PaddleOCR(
        lang='en',
        ocr_version='PP-OCRv3',
        enable_mkldnn=False,
        #text_det_limit_side_len=960,  # try 960 or even 640 if quality isn't important
        use_textline_orientation=True  # This replaces the need for cls=True
    )

    try:
        results, per_image_timings = run_ocr_on_images(ocr, compressed_infos, score_threshold=args.min_score)
        overall_elapsed = round(time.perf_counter() - overall_start, 3)

        out_data = {
            "metadata": {
                "start_time": start_iso,
                "total_time_seconds": overall_elapsed,
                "image_count": len(per_image_timings)
            },
            "timings": per_image_timings,
            "results": results
        }

        args.output_file.parent.mkdir(parents=True, exist_ok=True)
        with args.output_file.open("w", encoding="utf-8") as f:
            json.dump(out_data, f, indent=2, ensure_ascii=False)
        print(f"Finished. Results and timings saved to {args.output_file}")

    finally:
        deleted = 0
        for info in compressed_infos:
            p = info["path"]
            try:
                p.unlink()
                deleted += 1
            except FileNotFoundError:
                pass
            except Exception as exc:
                print(f"  Warning: failed to delete {p.name}: {exc}")
        print(f"Cleaned up {deleted} temporary _compressed files.")


if __name__ == "__main__":
    main()
