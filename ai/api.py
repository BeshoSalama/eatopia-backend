import base64
import logging
import os
import tempfile
from pathlib import Path
from urllib.parse import urlparse

import requests
from fastapi import FastAPI, File, HTTPException, Request, UploadFile
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel

from eatopia_ai_cli import build_diet_plan, scan_food

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("eatopia-ai")

API_CONTRACT_VERSION = "multipart-scan-v2"

app = FastAPI(title="Eatopia AI API", version=API_CONTRACT_VERSION)

MAX_SCAN_IMAGE_BYTES = 8 * 1024 * 1024
ALLOWED_IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}
CONTENT_TYPE_EXTENSIONS = {
    "image/jpeg": ".jpg",
    "image/png": ".png",
    "image/webp": ".webp",
}


class DietRequest(BaseModel):
    age: int
    weight: float
    height: float
    activity: str
    goal: str
    durationDays: int = 7


class ScanRequest(BaseModel):
    imagePath: str


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    logger.warning(
        "Validation error on %s %s content_type=%s errors=%s",
        request.method,
        request.url.path,
        request.headers.get("content-type"),
        safe_validation_errors(exc),
    )

    return JSONResponse(
        status_code=422,
        content={
            "success": False,
            "message": "Invalid request.",
            "errors": safe_validation_errors(exc),
        },
    )


@app.exception_handler(UnicodeDecodeError)
async def unicode_decode_exception_handler(request: Request, exc: UnicodeDecodeError):
    logger.warning(
        "Invalid request encoding on %s %s content_type=%s error=%s",
        request.method,
        request.url.path,
        request.headers.get("content-type"),
        exc,
    )

    return JSONResponse(
        status_code=400,
        content={
            "success": False,
            "message": "Invalid request encoding. Send images as multipart/form-data with field name image.",
            "errors": [],
        },
    )


@app.get("/")
def root():
    return {
        "message": "Eatopia AI API Running",
        "contract": API_CONTRACT_VERSION,
        "scanFoodRequest": "multipart/form-data field image",
    }


@app.get("/healthz")
def healthz():
    return {
        "success": True,
        "contract": API_CONTRACT_VERSION,
    }


@app.post("/diet-plan")
def diet_plan(data: DietRequest):
    result = build_diet_plan(data.dict())
    return result


@app.post("/scan-food")
async def scan_food_api(
    request: Request,
    image: UploadFile | None = File(default=None),
    file: UploadFile | None = File(default=None),
):
    request_content_type = request.headers.get("content-type", "")
    logger.info("scan-food request content_type=%s", request_content_type)

    upload = image or file
    if upload is not None:
        field_name = "image" if image is not None else "file"
        temp_path, size = await save_upload_to_temp_file(upload)
        logger.info(
            "scan-food upload field=%s filename=%s file_content_type=%s file_size=%s",
            field_name,
            upload.filename,
            upload.content_type,
            size,
        )
        try:
            return scan_food({"imagePath": temp_path})
        finally:
            delete_temp_file(temp_path)

    logger.info(
        "scan-food upload field=%s filename=%s file_content_type=%s file_size=%s",
        None,
        None,
        None,
        0,
    )

    if not request_content_type.lower().startswith("application/json"):
        raise HTTPException(
            status_code=400,
            detail="Send multipart/form-data with an image field, or JSON with imagePath.",
        )

    try:
        data = ScanRequest.parse_obj(await request.json())
    except Exception as exc:
        raise HTTPException(status_code=422, detail=f"Invalid scan request: {exc}") from exc

    temp_path = materialize_image_path(data.imagePath)
    try:
        return scan_food({"imagePath": temp_path})
    finally:
        if temp_path != data.imagePath:
            delete_temp_file(temp_path)


async def save_upload_to_temp_file(upload: UploadFile) -> tuple[str, int]:
    content_type = (upload.content_type or "").lower()
    if content_type not in CONTENT_TYPE_EXTENSIONS:
        raise HTTPException(status_code=400, detail="Uploaded file must be an image.")

    extension = normalize_extension(Path(upload.filename or "").suffix, content_type)
    if extension not in ALLOWED_IMAGE_EXTENSIONS:
        raise HTTPException(status_code=400, detail="Supported images are JPG, PNG, and WebP.")

    fd, temp_path = tempfile.mkstemp(prefix="eatopia-scan-", suffix=extension)
    size = 0

    try:
        with os.fdopen(fd, "wb") as output:
            while True:
                chunk = await upload.read(1024 * 1024)
                if not chunk:
                    break

                size += len(chunk)
                if size > MAX_SCAN_IMAGE_BYTES:
                    raise HTTPException(status_code=413, detail="Image is larger than 8 MB.")

                output.write(chunk)

        if size == 0:
            raise HTTPException(status_code=400, detail="No image uploaded.")

        return temp_path, size
    except Exception:
        delete_temp_file(temp_path)
        raise


def materialize_image_path(image_path: str) -> str:
    value = (image_path or "").strip()
    if not value:
        raise HTTPException(status_code=400, detail="imagePath is required.")

    if value.startswith("data:image/"):
        return save_data_url_to_temp_file(value)

    parsed = urlparse(value)
    if parsed.scheme in {"http", "https"}:
        return download_image_to_temp_file(value)

    return value


def download_image_to_temp_file(url: str) -> str:
    try:
        with requests.get(url, stream=True, timeout=30) as response:
            response.raise_for_status()
            content_type = response.headers.get("content-type", "").split(";")[0].lower()
            if not content_type.startswith("image/"):
                raise HTTPException(status_code=400, detail="imagePath URL did not return an image.")

            extension = normalize_extension(Path(urlparse(url).path).suffix, content_type)
            fd, temp_path = tempfile.mkstemp(prefix="eatopia-scan-url-", suffix=extension)
            size = 0

            try:
                with os.fdopen(fd, "wb") as output:
                    for chunk in response.iter_content(chunk_size=1024 * 1024):
                        if not chunk:
                            continue

                        size += len(chunk)
                        if size > MAX_SCAN_IMAGE_BYTES:
                            raise HTTPException(status_code=413, detail="Image is larger than 8 MB.")

                        output.write(chunk)

                return temp_path
            except Exception:
                delete_temp_file(temp_path)
                raise
    except HTTPException:
        raise
    except requests.RequestException as exc:
        raise HTTPException(status_code=400, detail=f"Could not download imagePath URL: {exc}") from exc


def save_data_url_to_temp_file(data_url: str) -> str:
    header, _, encoded = data_url.partition(",")
    if not encoded or ";base64" not in header:
        raise HTTPException(status_code=400, detail="Invalid image data URL.")

    content_type = header.removeprefix("data:").split(";")[0].lower()
    extension = normalize_extension("", content_type)
    fd, temp_path = tempfile.mkstemp(prefix="eatopia-scan-data-", suffix=extension)

    try:
        raw = base64.b64decode(encoded, validate=True)
        if len(raw) > MAX_SCAN_IMAGE_BYTES:
            raise HTTPException(status_code=413, detail="Image is larger than 8 MB.")

        with os.fdopen(fd, "wb") as output:
            output.write(raw)

        return temp_path
    except Exception:
        delete_temp_file(temp_path)
        raise


def normalize_extension(extension: str, content_type: str) -> str:
    extension = (extension or "").lower()
    if extension in ALLOWED_IMAGE_EXTENSIONS:
        return extension

    return CONTENT_TYPE_EXTENSIONS.get(content_type, ".jpg")


def delete_temp_file(path: str) -> None:
    try:
        os.remove(path)
    except FileNotFoundError:
        pass


def safe_validation_errors(exc: RequestValidationError) -> list[dict]:
    return [sanitize_value(error) for error in exc.errors()]


def sanitize_value(value):
    if isinstance(value, bytes):
        return f"<{len(value)} raw bytes>"

    if isinstance(value, bytearray):
        return f"<{len(value)} raw bytes>"

    if isinstance(value, dict):
        sanitized = {}
        for key, item in value.items():
            if key in {"input", "body"} and isinstance(item, (bytes, bytearray)):
                sanitized[key] = f"<{len(item)} raw bytes>"
            else:
                sanitized[key] = sanitize_value(item)
        return sanitized

    if isinstance(value, list):
        return [sanitize_value(item) for item in value]

    if isinstance(value, tuple):
        return [sanitize_value(item) for item in value]

    return value
