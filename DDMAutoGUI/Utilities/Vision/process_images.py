
import os
import argparse
from pathlib import Path
import glob
import json
import warnings

os.environ["PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK"] = "1" # Must be set before paddleocr import
#warnings.filterwarnings("ignore", message=".*ccache.*", category=UserWarning)


from paddleocr import PaddleOCR

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
        "--min-score",
        type=float,
        default=0.10,
        help="Minimum recognition confidence score to keep (default: 0.10)"
    )

    return parser.parse_args()


def main():
    args = parse_args()

    input_folder: Path = args.input_folder
    output_file: Path = args.output_file
    min_score: float = args.min_score

    # Validate input folder
    if not input_folder.exists() or not input_folder.is_dir():
        raise NotADirectoryError(f"Input folder does not exist or is not a directory: {input_folder}")

    # Ensure output directory exists
    output_file.parent.mkdir(parents=True, exist_ok=True)

    # Initialize OCR
    ocr = PaddleOCR(
        lang='en',
        ocr_version='PP-OCRv3',
        enable_mkldnn=False,
        #text_det_limit_side_len=960,  # try 960 or even 640 if quality isn't important
        use_textline_orientation=True  # This replaces the need for cls=True
    )

    # Find all images
    image_extensions = ('*.jpg', '*.jpeg', '*.png', '*.bmp')
    images = []
    for ext in image_extensions:
        # Use pathlib/glob for clean path joining
        images.extend([str(p) for p in input_folder.glob(ext)])

    print(f"Found {len(images)} images in {input_folder}. Starting OCR...")


    # OCR and write results
    data = {"results": []}
    with output_file.open('w', encoding='utf-8') as json_file:
        for img_path in images:
            file_name = Path(img_path).name
            print(f"Processing: {file_name}")

            result = ocr.predict(img_path)
            img_result = {
                "file": file_name,
                "strings": []
            }

            if result:
                for page in result:
                    if isinstance(page, dict) and 'rec_texts' in page:
                        for text, score in zip(page['rec_texts'], page['rec_scores']):
                            if score > min_score:
                                img_result["strings"].append({
                                    "text": text,
                                    "score": float(f"{score:.3f}")  # ensure JSON-serializable
                                })

            data["results"].append(img_result)

        json.dump(data, json_file, indent=2)

    print(f"Finished. Results saved to {output_file}")


if __name__ == "__main__":
    main()
