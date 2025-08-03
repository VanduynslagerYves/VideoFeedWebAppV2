from fastapi import FastAPI, Request #, UploadFile, File
from fastapi.responses import Response
from PIL import Image, ImageDraw
import numpy as np
import asyncio
from io import BytesIO
from ultralytics import YOLO
import torch
import concurrent.futures

app = FastAPI()
# Device selection (Nvidia CUDA needs to be installed, with the matching PyTorch version))
# If CUDA is not used, inference is slow af, resulting in choppy feed
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Inference with {DEVICE}")

# Load YOLO model once, move to device
model = YOLO("yolov8n.pt")
model.to(DEVICE)
model.half()
model.fuse()

# Shared thread pool for concurrent inference
executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)

@app.post("/detect-human-v2/")
async def detect_human_v2(request: Request):
    contents = await request.body()
    img = Image.open(BytesIO(contents)).convert("RGB")
    img_np = np.array(img)

    # Run YOLO model in thread pool
    loop = asyncio.get_event_loop()
    # Filter for person and car classes
    results = await loop.run_in_executor(executor, lambda: model.predict(img_np, classes=[0,2]))

    draw = ImageDraw.Draw(img)
    for result in results:
        for box in result.boxes:
            class_id = int(box.cls[0])
            label = model.names[class_id].lower()
            # if label not in ("person", "car"):
            #     continue
            bbox = box.xyxy[0].tolist()
            if label == "person":
                draw.rectangle(bbox, outline="green", width=2)
            elif label == "car":
                draw.rectangle(bbox, outline="blue", width=2)

    buf = BytesIO()
    img.save(buf, format="JPEG", quality=78)
    buf.seek(0)
    return Response(content=buf.getvalue(), media_type="application/octet-stream")

# @app.post("/detect-human/")
# async def detect_human(file: UploadFile = File(...)):
#     # Read file bytes and load image
#     contents = await file.read()
#     img = Image.open(BytesIO(contents)).convert("RGB")
#     img_np = np.array(img)

#     # Run YOLOv5 model on image
#     results = model.predict(img_np)

#     # Draw bounding boxes
#     draw = ImageDraw.Draw(img)
#     # human_detected = False

#     for r in results:
#         for box in r.boxes:
#             class_id = int(box.cls[0])
#             label = model.names[class_id]
#             if label.lower() == "person":
#                 # human_detected = True
#                 bbox = box.xyxy[0].tolist()  # [x1, y1, x2, y2]
#                 # Draw green bounding box
#                 draw.rectangle(bbox, outline="green", width=3)

#     # Return the image with bounding boxes
#     buf = BytesIO()
#     img.save(buf, format="JPEG")
#     buf.seek(0)
#     return Response(content=buf.getvalue(), media_type="image/jpeg")

#-----------------------------------------------

# import asyncio
# import concurrent.futures
# from io import BytesIO
# from PIL import Image, ImageDraw
# import numpy as np
# from ultralytics import YOLO
# import human_detection_pb2
# import human_detection_pb2_grpc
# import grpc
# import torch

# # Detect if CUDA is available and select device
# DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

# # Load pretrained YOLOv8 model once, on the right device
# model = YOLO("yolov8n.pt")
# model.to(DEVICE)

# # Use a thread pool for concurrent inference (tune max_workers as needed)
# executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)

# class HumanDetectionServicer(human_detection_pb2_grpc.HumanDetectionServicer):
#     async def DetectHumans(self, request_iterator, context):
#         async for request in request_iterator:
#             try:
#                 # Read image bytes and process
#                 img = Image.open(BytesIO(request.image_data)).convert("RGB")
#                 img_np = np.array(img)

#                 # Run YOLO model in thread pool
#                 results = await asyncio.get_event_loop().run_in_executor(
#                     executor, model.predict, img_np
#                 )

#                 # Draw bounding boxes for "person" and "car"
#                 draw = ImageDraw.Draw(img)
#                 for r in results:
#                     for box in r.boxes:
#                         class_id = int(box.cls[0])
#                         label = model.names[class_id].lower()
#                         if label not in ("person", "car"):
#                             continue
#                         bbox = box.xyxy[0].tolist()
#                         color = "green" if label == "person" else "blue"
#                         draw.rectangle(bbox, outline=color, width=2)

#                 # Return the image with bounding boxes as bytes
#                 buf = BytesIO()
#                 img.save(buf, format="JPEG", quality=78)
#                 buf.seek(0)
#                 yield human_detection_pb2.ImageResponse(processed_image=buf.getvalue())
#             except Exception as e:
#                 print(f"Error processing image: {e}")
#                 # Optionally, yield an empty or error response here

# async def serve():
#     server = grpc.aio.server()
#     human_detection_pb2_grpc.add_HumanDetectionServicer_to_server(HumanDetectionServicer(), server)
#     server.add_insecure_port('[::]:50051')
#     await server.start()
#     print(f"gRPC server started on port 50051 (device: {DEVICE})")
#     await server.wait_for_termination()

# if __name__ == "__main__":
#     asyncio.run(serve())