import concurrent.futures
import grpc
import object_detection_pb2_grpc
from concurrent import futures
from services.object_detection_service import ObjectDetectionService
from model_utils import load_trt_model

# Load the YOLO TensorRT engine
model = load_trt_model("yolov8s.engine")

executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)

def serve_grpc():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=4))
    object_detection_pb2_grpc.add_ObjectDetectionServicer_to_server(ObjectDetectionService(model), server)
    server.add_insecure_port('[::]:50051')
    server.start()
    print("gRPC server started on port 50051")
    server.wait_for_termination()

if __name__ == "__main__":
    serve_grpc()