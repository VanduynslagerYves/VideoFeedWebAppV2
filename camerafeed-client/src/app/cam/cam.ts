import { Component, Input, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { SignalRConnection } from './cam.connection';

@Component({
  selector: 'app-cam',
  templateUrl: './cam.html',
  styleUrl: './cam.less'
})

export class Cam implements AfterViewInit, OnDestroy {
  /** Camera ID passed from the parent component */
  @Input() cameraId!: number;
  /** Reference to the canvas element in the template */
  @ViewChild('canvas', {static: false}) canvasRef!: ElementRef<HTMLCanvasElement>;

  /** SignalR connection for receiving real-time video frames */
  private connection!: SignalRConnection;

  /** Image object used to draw received frames onto the canvas */
  private img = new window.Image();
  /**
   * Stores the latest pending frame if a new frame arrives while the previous is still loading.
   * Only the most recent frame is kept to minimize latency and avoid backlog—
   * intermediate frames are dropped if multiple arrive during image loading.
   */
  private latestPendingFrame: Uint8Array | null = null;

  /** Indicates if an image is currently being loaded into the canvas */
  private isImageLoading = false;
  /** Indicates if the camera is currently loading */
  public isCameraLoading = true;

  constructor(private auth: AuthService, private cdr: ChangeDetectorRef) {}

  /**
   * Lifecycle hook: runs after the component's view has been initialized
   * Sets up the SignalR connection and image drawing logic
   */
  ngAfterViewInit() {
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
      this.isImageLoading = false;

      // If a new frame arrived while loading, display it now
      if (this.latestPendingFrame) {
        const nextData = this.latestPendingFrame;
        this.latestPendingFrame = null;
        this.displayFrame(nextData);
      }
    };
    
    this.connection = this.initConnection();
    this.connection.start();
  }

  private initConnection() : SignalRConnection{
    return new SignalRConnection(this.auth, `Camera ${this.cameraId}`,
      (data: Uint8Array) => this.displayFrame(data),
      (isLoading: boolean) => { this.isCameraLoading = isLoading; },
      this.cdr
    );
  }
  /** Store the last object URL to revoke it and prevent memory leaks */
  private lastObjectUrl: string | null = null;

  private displayFrame(data: Uint8Array) {
    if (this.isImageLoading) {
      this.latestPendingFrame = data;
      return;
    }
    this.isImageLoading = true;

  // Convert incoming data to a standard Uint8Array for Blob compatibility
  const uint8Data = new Uint8Array(data);
  const blob = new Blob([uint8Data], { type: 'image/jpeg' });

  // Revoke previous object URL to prevent memory leaks
  if (this.lastObjectUrl) {
    URL.revokeObjectURL(this.lastObjectUrl);
  }
  
  this.lastObjectUrl = URL.createObjectURL(blob);
  this.img.src = this.lastObjectUrl;
  }

  /**
   * Lifecycle hook: clean up the SignalR connection when the component is destroyed
   * Also remove SignalR event listeners here if you add more in the future
   */
  ngOnDestroy() {
    if (this.connection) {
      this.connection.stop();
    }
  }
}
