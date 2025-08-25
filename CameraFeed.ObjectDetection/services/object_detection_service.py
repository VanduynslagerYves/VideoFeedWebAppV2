import object_detection_pb2
import object_detection_pb2_grpc
import numpy as np
from PIL import Image, ImageDraw
from io import BytesIO
from model_utils import load_trt_model

# Load the YOLO TensorRT engine
model = load_trt_model("yolov8s.engine")

def draw_bounding_boxes(img, results, model):
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
            elif label == "bicycle":
                draw_rectangle(draw, bbox, "orange")

def draw_rectangle(draw, bbox, color):
    draw.rectangle(bbox, outline=color, width=2)

class ObjectDetectionService(object_detection_pb2_grpc.ObjectDetectionServicer):
    def __init__(self, model):
        self.model = model

    def DetectObjects(self, request_iterator, context):
        for request in request_iterator:
            img = Image.open(BytesIO(request.image_data)).convert("RGB")
            img_np = np.array(img)
            results = self.model.predict(
                img_np,
                classes=[0, 1, 2, 3]
            )
            draw_bounding_boxes(img, results, self.model)
            buf = BytesIO()
            img.save(buf, format="JPEG", quality=78)
            buf.seek(0)
            yield object_detection_pb2.ImageResponse(processed_image=buf.getvalue())