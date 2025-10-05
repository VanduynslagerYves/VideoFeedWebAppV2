# Technical Summary
## Architecture Overview
The system is designed for real-time camera feed processing and delivery, using a broker/relay (SocketServer) to decouple internal processing from external clients. The architecture consists of three main components:
- Processor:
Runs on the internal network, manages camera workers, processes video streams, and sends frames to the SocketServer.
-	SocketServer:
Hosted with the Processor and in the cloud, acts as a broker and fallback for real-time communication. It relays frames and control messages between the Processor and frontend clients.
-	Frontend (Angular):
Connects to the SocketServer to receive video frames and send control commands.
## Key Challenges & Solutions
-	NAT Traversal:
By using a cloud-hosted SocketServer, the architecture avoids direct inbound connections to the internal Processor, bypassing NAT/firewall issues.
-	Cost Optimization:
The current relay model is simple but can be costly due to high data transfer through the SocketServer. Considering a hybrid approach (using the SocketServer for control and direct streaming for video) can reduce costs, but introduces new challenges (NAT, browser limitations).
-	Browser Compatibility:
The frontend uses standard browser technologies (WebSockets) to communicate with the SocketServer, ensuring compatibility and security.
## Reasoning
This architecture provides a secure, scalable, and browser-friendly solution for real-time video streaming, while keeping internal resources protected and minimizing complexity for client connections.

# CameraFeed Solution - Local Debugging Setup

This repository contains a multi-component system for video feed processing, object/human detection, and web-based visualization. It includes:
- **CameraFeed.Web**: Razor Pages web frontend (.NET 9)
- **camerafeed-client**: Angular frontend
- **CameraFeed.API**: .NET 9 backend API for video and object detection
- **CameraFeed.ObjectDetectionAPI**: Python FastAPI service for human detection using YOLO (PyTorch)
---

## Prerequisites

- **.NET 9 SDK** (latest STS version)
- **Python 3.10**
- **Node.js and angular** (if using the angular frontend)
- **ngrok** (optional, for public tunneling)
- **NVIDIA GPU + CUDA Toolkit 12.1 or up** (for GPU-accelerated detection)
---

## 1. CameraFeed.Web (Frontend)

### Install .NET SDK (Linux example)
```sh
curl -ssL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel STS --runtime dotnet
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc
dotnet --list-runtimes
```

### Run Locally
```sh
cd CameraFeed.Web
dotnet run --urls "https://0.0.0.0:7006"
```

- The app will be available at https://localhost:7006
- For local debugging, set the environment variable `ASPNETCORE_ENVIRONMENT=Development`.
---

## 2. CameraFeed.API (Backend API)

### Run Locally
```sh
cd CameraFeed.API
dotnet run
```

- The API will be available at the port specified in `launchSettings.json` or by default.
- CORS is configured for localhost and Azure. Update origins in `Program.cs` if needed.

---

## 3. CameraFeed.ObjectDetectionAPI (Python FastAPI)

### Python Environment (3.10)
```sh
cd CameraFeed.ObjectDetectionAPI
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
``` 
---

### Install Dependencies
```sh
pip install -r requirements.txt
```
---

### PyTorch & CUDA
For **GPU acceleration**, install CUDA toolkit and install the correct PyTorch version for your CUDA toolkit:
[PyTorch Get Started](https://pytorch.org/get-started/locally/)
- Uninstall cpu only torch
```sh
pip uninstall torch torchvision torchaudio
```
- Install gpu capable torch (CUDA 12.1 is latest supported for PyTorch, but also works with CUDA 12.9)
```sh
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
```
---

### ONNX (needed for TensorRT)
```sh
pip install "onnx>=1.12.0,<1.18.0" onnxslim>=0.1.59 onnxruntime-gpu
```
---

### Nvidia TensorRT
- Go to the [NVIDIA TensorRT GitHub page](https://github.com/NVIDIA/TensorRT?tab=readme-ov-file)
- Download the TensorRT zip package for Windows
- Extract the zip file (e.g., to C:\tools\TensorRT-10.13.0.35)
```sh
cd C:\tools\TensorRT-10.13.0.35
py -3.10 -m pip install tensorrt_rtx-1.1.1.26-cp310-none-win_amd64.whl
```
- Add the extracted folder to your system PATH
---

### Run the API
```sh
python -m uvicorn app:app --reload --host 127.0.0.1 --port 8000
```
Or use the provided script:
```sh
launch.cmd
```
---

### Test the API
```sh
curl -X POST "http://127.0.0.1:8000/detect-objects/" --data-binary @test.jpeg --output result.jpeg
```
---

## 5. ngrok (Optional, for public tunneling)

```sh
ngrok http 7006
# or use the provided script:
cd CameraFeed.Web/ngrok-config
start_ngrok_api.cmd
```
---

## 6. Auth0 Setup
- Set up callback URLs in your Auth0 dashboard to match your local and deployed URLs.
- For local debugging, ensure Auth0 allows `https://localhost:7006/signin-oidc` as a callback.
- Authentication settings are read from environment variables or `appsettings.json`.
---

## Troubleshooting
- **CUDA not detected**: Ensure NVIDIA drivers and CUDA toolkit match the PyTorch version.
- **Authentication issues**: Double-check Auth0 settings and callback URLs.
- **Port conflicts**: Make sure the ports used by each service are available.
- **Development mode**: Set `ASPNETCORE_ENVIRONMENT=Development` for detailed error messages.
---

