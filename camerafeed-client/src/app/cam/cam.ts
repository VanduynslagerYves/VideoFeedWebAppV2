import { Component, Input, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { CameraService } from './camera.service';

@Component({
  selector: 'app-cam',
  templateUrl: './cam.html',
  styleUrl: './cam.less'
})

export class Cam implements AfterViewInit, OnDestroy {
  /** Camera ID passed from the parent component */
  @Input() cameraId!: number;
  /** Optionally allow the hub URL to be set from the parent */
  @Input() hubUrl: string = 'https://localhost:7214/videoHub';
  /** Reference to the canvas element in the template */
  @ViewChild('canvas', {static: false}) canvasRef!: ElementRef<HTMLCanvasElement>;

  /** SignalR connection for receiving real-time video frames */
  private connection!: signalR.HubConnection;

  /** Image object used to draw received frames onto the canvas */
  private img = new window.Image();
  /**
   * Stores the latest pending frame if a new frame arrives while the previous is still loading.
   * Only the most recent frame is kept to minimize latency and avoid backlog—
   * intermediate frames are dropped if multiple arrive during image loading.
   */
  private latestPendingFrame: string | null = null;

  /** Indicates if an image is currently being loaded into the canvas */
  private loading = false;

  /** Indicates if the camera is currently loading */
  public isCameraLoading = true;

  // Inject the CameraService for starting the camera via HTTP
  constructor(private cameraService: CameraService, private cdr: ChangeDetectorRef) {}

  /** Starts the camera stream */
  private startCamera() {
    this.cameraService.startCamera(this.cameraId).subscribe({
      next: (res) => console.log('Camera started', res),
      error: (err) => console.error('Error starting camera', err)
    });
  }

  /**
   * Lifecycle hook: runs after the component's view has been initialized
   * Sets up the SignalR connection and image drawing logic
   */
  ngAfterViewInit() {
    this.startCamera();

    if (!this.canvasRef) {
      console.error('Canvas reference not found');
      return;
    }
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      console.error('2D context not available on canvas');
      return;
    }

    // When the image loads, draw it on the canvas
    this.img.onload = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(this.img, 0, 0);
      this.loading = false;

      // If a new frame arrived while loading, display it now
      if (this.latestPendingFrame) {
        const nextData = this.latestPendingFrame;
        this.latestPendingFrame = null;
        this.displayFrame(nextData);
      }
    };

    // Set up the SignalR connection to receive video frames
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .build();

    // Listen for incoming image bytes from the server
    this.connection.on('ReceiveImgBytes', (data: string) => {
      this.isCameraLoading = false; // Update camera loading state
      this.cdr.detectChanges(); // Trigger change detection to update the UI
      this.displayFrame(data);
    });
    // If you add more SignalR events, consider removing them in ngOnDestroy for cleanup

    // Start the SignalR connection and join the group for this camera
    this.connection.start()
      .then(() => {
        this.connection.invoke('JoinGroup', `camera_${this.cameraId}`);
      })
      .catch(err => console.error('SignalR Error:', err.toString()));
  }

  /**
   * Draw a frame (base64 JPEG) on the canvas, with loading logic
   */
  private displayFrame(data: string) {
    if (this.loading) {
      this.latestPendingFrame = data;
      return;
    }
    this.loading = true;
    this.img.src = 'data:image/jpeg;base64,' + data;
  }

  /**
   * Lifecycle hook: clean up the SignalR connection when the component is destroyed
   * Also remove SignalR event listeners here if you add more in the future
   */
  ngOnDestroy() {
    if (this.connection) {
      this.connection.stop();
    }
    // If you add more SignalR event listeners, remove them here for memory safety
  }
}
