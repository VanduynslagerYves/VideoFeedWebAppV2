import torch
from ultralytics import YOLO

def load_model(model_path: str, device: str):
    """
    Loads a YOLO model from the specified path and prepares it for inference.

    Args:
        model_path (str): Path to the YOLO model file.
        device (str): Device to use for inference ('cuda' or 'cpu').

    Returns:
        YOLO: The loaded and prepared YOLO model.
    """
    # Load the YOLO model from file
    model = YOLO(model_path)
    # Move the model to the specified device
    model.to(device)

    # Explicitly set the device (redundant with .to(), but ensures correct context)
    model.cuda() if device == "cuda" else model.cpu()
    # Convert model parameters to half-precision for faster inference (if supported)
    model.half()
    # Fuse model layers for improved inference speed
    model.fuse()
    
    return model

def get_device() -> str:
    """
    Determines the best available device for inference.

    Returns:
        str: 'cuda' if a compatible Nvidia GPU is available, otherwise 'cpu'.
    """
    # Use CUDA if available for much faster inference; otherwise, fall back to CPU
    return "cuda" if torch.cuda.is_available() else "cpu"