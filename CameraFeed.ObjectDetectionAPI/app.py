import numpy as np
import asyncio
import concurrent.futures
from fastapi import FastAPI, Request #, UploadFile, File # FastAPI framework for building APIs
from fastapi.responses import Response
from PIL import Image, ImageDraw
from io import BytesIO
from model_utils import load_model, get_device # Utility functions for model/device

# Initialize FastAPI application
app = FastAPI()

# Select the best device for inference (GPU if available, else CPU)
DEVICE = get_device()
print(f"Inference with {DEVICE}")

# Load the YOLO model onto the selected device
model = load_model("yolov8n.pt", DEVICE)

# Create a thread pool executor to handle concurrent inference requests
executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)

@app.post("/detect-objects/")
async def detect_objects(request: Request):
    """
    Receives an image via POST request, runs YOLO inference to detect persons and cars,
    draws bounding boxes, and returns the processed image as a JPEG byte stream.
    """
    # Read the raw bytes from the request body
    contents = await request.body()
    # Open the image and ensure it's in RGB format
    img = Image.open(BytesIO(contents)).convert("RGB")
    # Convert the image to a NumPy array for model input
    img_np = np.array(img)

    # Run the YOLO model prediction in a background thread to avoid blocking the event loop
    loop = asyncio.get_event_loop()
    results = await loop.run_in_executor(
        executor,
        lambda: model.predict(
            img_np,
            classes=[0, 2])  # Only detect 'person' (class 0) and 'car' (class 2)
    )

    # Draw bounding boxes for detected persons and cars
    draw = ImageDraw.Draw(img)
    for result in results:
        for box in result.boxes:
            class_id = int(box.cls[0])
            label = model.names[class_id].lower()
            bbox = box.xyxy[0].tolist()
            if label == "person":
                draw.rectangle(bbox, outline="green", width=3)
            elif label == "car":
                draw.rectangle(bbox, outline="blue", width=3)

    # Save the processed image to a buffer as JPEG
    buf = BytesIO()
    img.save(buf, format="JPEG", quality=78)
    buf.seek(0)
    # Return the image bytes as a binary response
    return Response(content=buf.getvalue(), media_type="application/octet-stream")

#-----------------------------------------------
# The commented section below is an alternative gRPC-based implementation for human detection.
# It is not active in this FastAPI-based service.

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