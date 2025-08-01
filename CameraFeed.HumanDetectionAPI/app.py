from fastapi import FastAPI, UploadFile, File, Request
from fastapi.responses import Response
from PIL import Image, ImageDraw
import numpy as np
import asyncio
from io import BytesIO
from ultralytics import YOLO

app = FastAPI()
# Load pretrained YOLOv5 model (YOLOv8 under the hood now)
model = YOLO("yolov8n.pt")  # 'n' is nano version, change to 's', 'm', 'l' for larger

@app.post("/detect-human-v2/")
async def detect_human(request: Request):
    # Read raw bytes from request body
    contents = await request.body()
    img = Image.open(BytesIO(contents)).convert("RGB")
    img = img.resize((640, 480))  # Resize to model input size
    img_np = np.array(img)

    # Run YOLO model on image in a separate thread
    loop = asyncio.get_event_loop()
    results = await loop.run_in_executor(None, model.predict, img_np)

    # Draw bounding boxes
    draw = ImageDraw.Draw(img)
    for r in results:
        for box in r.boxes:
            class_id = int(box.cls[0])
            label = model.names[class_id]
            if label.lower() == "person":
                bbox = box.xyxy[0].tolist()
                draw.rectangle(bbox, outline="green", width=3)

    # Return the image with bounding boxes as raw bytes
    buf = BytesIO()
    img.save(buf, format="JPEG", quality=78)
    buf.seek(0)
    return Response(content=buf.getvalue(), media_type="application/octet-stream")

@app.post("/detect-human/")
async def detect_human(file: UploadFile = File(...)):
    # Read file bytes and load image
    contents = await file.read()
    img = Image.open(BytesIO(contents)).convert("RGB")
    img_np = np.array(img)

    # Run YOLOv5 model on image
    results = model.predict(img_np)

    # Draw bounding boxes
    draw = ImageDraw.Draw(img)
    # human_detected = False

    for r in results:
        for box in r.boxes:
            class_id = int(box.cls[0])
            label = model.names[class_id]
            if label.lower() == "person":
                # human_detected = True
                bbox = box.xyxy[0].tolist()  # [x1, y1, x2, y2]
                # Draw green bounding box
                draw.rectangle(bbox, outline="green", width=3)

    # Return the image with bounding boxes
    buf = BytesIO()
    img.save(buf, format="JPEG")
    buf.seek(0)
    return Response(content=buf.getvalue(), media_type="image/jpeg")