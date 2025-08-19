# Use this script to create a TensorRT model from a YOLOv8 model (Run this with Python 3.10 only,
# TensorRT required ONNX and is only supported on 3.10 at the moment of writing)

# Ensure you have the required packages installed:
# ultralytics with torch enabled for gpu
# tensorrt

from ultralytics import YOLO
import tensorrt
print(tensorrt.__version__)

model = YOLO("yolov8n.pt")
# Export to TensorRT engine
model.export(
    format="engine",
    device=0,       # device=0 for GPU, device=cpu for CPU
    #imgsz=416,      # smaller input size
    half=True,      # use FP16 (half-precision)
    dynamic=True,  # set True if you want dynamic shapes
    simplify=True   # simplify ONNX graph before TensorRT conversion
)