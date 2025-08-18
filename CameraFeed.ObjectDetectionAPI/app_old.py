# import numpy as np
# import asyncio
# import concurrent.futures
# from fastapi import FastAPI, Request #, UploadFile, File # FastAPI framework for building APIs
# from fastapi.responses import Response
# from PIL import Image, ImageDraw
# from io import BytesIO
# from model_utils import load_model, get_device # Utility functions for model/device
# import requests  # For making HTTP requests to the backend service

# # Initialize FastAPI application
# app = FastAPI()
# @app.post("/detect-objects/")
# async def detect_objects(request: Request):
#     #camera_id = request.headers.get("x-camera-id")
#     contents = await request.body()
#     img = Image.open(BytesIO(contents)).convert("RGB")
#     img_np = np.array(img)

#     loop = asyncio.get_event_loop()
#     results = await loop.run_in_executor(
#         executor,
#         lambda: model.predict(
#             img_np,
#             classes=[0, 2, 3]
#         )
#     )

#     draw_bounding_boxes(img, results, model)

#     buf = BytesIO()
#     img.save(buf, format="JPEG", quality=78)
#     buf.seek(0)
#     return Response(content=buf.getvalue(), media_type="application/octet-stream")



# # Initialize FastAPI application
# app = FastAPI()

# # Select the best device for inference (GPU if available, else CPU)
# DEVICE = get_device()
# print(f"Inference with {DEVICE}")

# # Load the YOLO model onto the selected device
# model = load_model("yolov8s.pt", DEVICE)

# # Create a thread pool executor to handle concurrent inference requests
# executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)

# @app.post("/detect-objects/")
# async def detect_objects(request: Request):
#     """
#     Receives an image via POST request, runs YOLO inference to detect persons and cars,
#     draws bounding boxes, and returns the processed image as a JPEG byte stream.
#     """

#     camera_id = request.headers.get("x-camera-id")

#     # Read the raw bytes from the request body
#     contents = await request.body()
#     # Open the image and ensure it's in RGB format
#     img = Image.open(BytesIO(contents)).convert("RGB")
#     # Convert the image to a NumPy array for model input
#     img_np = np.array(img)

#     # Run the YOLO model prediction in a background thread to avoid blocking the event loop
#     loop = asyncio.get_event_loop()
#     results = await loop.run_in_executor(
#         executor,
#         lambda: model.predict(
#             img_np,
#             classes=[0, 2, 3])  # Only detect 'person' (class 0) and 'car' (class 2)
#     )

#     # Draw bounding boxes for detected persons and cars
#     draw = ImageDraw.Draw(img)
#     for result in results:
#         for box in result.boxes:
#             class_id = int(box.cls[0])
#             label = model.names[class_id].lower()
#             bbox = box.xyxy[0].tolist()
#             if label == "person":
#                 draw.rectangle(bbox, outline="green", width=2)
#                 # person_detected = True
#             elif label == "car":
#                 # person_detected = True
#                 draw.rectangle(bbox, outline="blue", width=2)
#             elif label == "motorcycle":
#                 draw.rectangle(bbox, outline="yellow", width=2)
#     # Notify backend if a person was detected
#     # if person_detected:
#     #     try:
#     #         requests.post(
#     #             "https://localhost:7214/api/camera/person-detected/",
#     #             json={"CameraId": camera_id}
#     #         )
#     #     except Exception as e:
#     #         print(f"Failed to notify backend: {e}")

#     # Save the processed image to a buffer as JPEG
#     buf = BytesIO()
#     img.save(buf, format="JPEG", quality=78)
#     buf.seek(0)
#     # Return the image bytes as a binary response
#     return Response(content=buf.getvalue(), media_type="application/octet-stream")