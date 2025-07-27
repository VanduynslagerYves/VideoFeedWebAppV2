from fastapi import FastAPI, UploadFile, File
from fastapi.responses import JSONResponse
from PIL import Image
import numpy as np
from io import BytesIO
from ultralytics import YOLO

app = FastAPI()

# Load pretrained YOLOv5 model (YOLOv8 under the hood now)
model = YOLO("yolov8m.pt")  # 'n' is nano version, change to 's', 'm', 'l' for larger

@app.post("/detect-human/")
async def detect_human(file: UploadFile = File(...)):
    # Read file bytes and load image
    contents = await file.read()
    img = Image.open(BytesIO(contents)).convert("RGB")
    img_np = np.array(img)

    # Run YOLOv5 model on image
    results = model.predict(img_np)

    # Parse detections
    human_detected = False
    detections = []

    for r in results:
        for box in r.boxes:
            class_id = int(box.cls[0])
            confidence = float(box.conf[0])
            label = model.names[class_id]
            if label.lower() == "person":
                human_detected = True
                detections.append({
                    "label": label,
                    "confidence": confidence,
                    "bbox": box.xyxy[0].tolist()
                })

    return JSONResponse({
        "human_detected": human_detected,
        "detections": detections
    })
