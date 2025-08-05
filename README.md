# CameraFeed Solution - Local Debugging Setup

This repository contains a multi-component system for video feed processing, object/human detection, and web-based visualization. It includes:
- **CameraFeed.Web**: Razor Pages web frontend (.NET 9)
- **CameraFeed.API**: .NET 9 backend API for video and object detection
- **CameraFeed.ObjectDetectionAPI**: Python FastAPI service for human detection using YOLO (PyTorch)

---

## Prerequisites

- **.NET 9 SDK** (latest STS version)
- **Python 3.9+** (recommended: 3.10 or 3.11)
- **Node.js** (if modifying frontend assets)
- **ngrok** (optional, for public tunneling)
- **NVIDIA GPU + CUDA Toolkit** (for GPU-accelerated detection, optional)

---

## 1. CameraFeed.Web (Frontend)

### Install .NET SDK (Linux example)
```sh
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel STS --runtime dotnet
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

### Python Environment
```sh
cd CameraFeed.ObjectDetectionAPI
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
```

### Install Dependencies
```sh
pip install -r requirements.txt
```

### PyTorch & CUDA
- For **GPU acceleration**, install CUDA toolkit and install the correct PyTorch version for your CUDA toolkit:
  - [PyTorch Get Started](https://pytorch.org/get-started/locally/)
  - Example (CUDA 12.1):
    ```sh
    pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
    ```
- For **CPU only (not recommended)**:
    ```sh
    pip install torch torchvision torchaudio
    ```

### Run the API
```sh
python -m uvicorn app:app --reload --host 127.0.0.1 --port 8000
```
Or use the provided script:
```sh
launch.cmd
```

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

## License
See [CameraFeed.Web/wwwroot/lib/bootstrap/LICENSE](CameraFeed.Web/wwwroot/lib/bootstrap/LICENSE) for Bootstrap license.

---
