import numpy as np
import asyncio
import concurrent.futures
from fastapi import FastAPI, Request
from fastapi.responses import Response
from PIL import Image, ImageDraw
from io import BytesIO
from model_utils import load_trt_model

app = FastAPI()

# Load the YOLO TensorRT engine
model = load_trt_model("yolov8s.engine")

executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)

@app.post("/detect-objects/")
async def detect_objects(request: Request):
    #camera_id = request.headers.get("x-camera-id")
    contents = await request.body()
    img = Image.open(BytesIO(contents)).convert("RGB")
    img_np = np.array(img)

    loop = asyncio.get_event_loop()
    results = await loop.run_in_executor(
        executor,
        lambda: model.predict(
            img_np,
            classes=[0, 2, 3]
        )
    )

    draw_bounding_boxes(img, results, model)

    buf = BytesIO()
    img.save(buf, format="JPEG", quality=78)
    buf.seek(0)
    return Response(content=buf.getvalue(), media_type="application/octet-stream")

def draw_bounding_boxes(img, results, model):
    """
    Draws bounding boxes for detected persons, cars, and motorcycles on the image.
    """
    draw = ImageDraw.Draw(img)
    for result in results:
        for box in result.boxes:
            class_id = int(box.cls[0])
            label = model.names[class_id].lower()
            bbox = box.xyxy[0].tolist()
            if label == "person":
                draw_rectangle(draw, bbox, "green")
            elif label == "car":
                draw_rectangle(draw, bbox, "blue")
            elif label == "motorcycle":
                draw_rectangle(draw, bbox, "yellow")

def draw_rectangle(draw, bbox, color):
    """
    Draws a rectangle on the image.
    """
    draw.rectangle(bbox, outline=color, width=2)