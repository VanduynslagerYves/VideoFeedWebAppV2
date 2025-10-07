import { Component, Input, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { take } from 'rxjs/operators';
import { AuthService } from '@auth0/auth0-angular';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';

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
  private connection!: signalR.HubConnection;

  private readonly hubUrl: string = 'https://localhost:7244/clienthub';
  private readonly fallbackHubUrl = 'https://localhost:7000/clienthub';
  private readonly fallbackAudience = 'https://localhost:7000';
  /** Image object used to draw received frames onto the canvas */
  private img = new window.Image();
  /**
   * Stores the latest pending frame if a new frame arrives while the previous is still loading.
   * Only the most recent frame is kept to minimize latency and avoid backlog—
   * intermediate frames are dropped if multiple arrive during image loading.
   */
  private latestPendingFrame: Uint8Array | null = null;

  /** Indicates if an image is currently being loaded into the canvas */
  private loading = false;

  /** Indicates if the camera is currently loading */
  public isCameraLoading = true;

  /** Store the beforeunload handler so it can be removed */
  private beforeUnloadHandler: (() => void) | null = null;

  // Inject the CameraService for starting the camera via HTTP
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
      this.loading = false;

      // If a new frame arrived while loading, display it now
      if (this.latestPendingFrame) {
        const nextData = this.latestPendingFrame;
        this.latestPendingFrame = null;
        this.displayFrame(nextData);
      }
    };
    
    // Get an access token
    this.auth.isAuthenticated$.pipe(take(1)).subscribe(isAuth => {
      if (isAuth) {
        this.auth.getAccessTokenSilently().pipe(take(1)).subscribe({
          next: (token) => this.setupSignalRConnection(token),
          error: (err) => console.error('Error getting access token', err)
        });
      }
    });
  }

  private setupSignalRConnection(token: string, hubUrl: string = this.hubUrl, triedFallback: boolean = false) {
    // Set up the SignalR connection to receive video frames
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    const groupName = `Camera ${this.cameraId}`;
    
    // Start the SignalR connection and join the group for this camera
    this.connection.start()
      .then(() => {
        this.connection.invoke('JoinGroup', groupName);
        this.connection.invoke('StartStreaming', groupName);
      })
      .catch(err => {
      if (!triedFallback && this.fallbackHubUrl && hubUrl !== this.fallbackHubUrl && this.fallbackAudience) {
        console.warn("Trying remote URL:", this.fallbackHubUrl);
        // Clean up previous connection and event listeners
        window.removeEventListener('beforeunload', this.beforeUnloadHandler!);
        this.connection.off('ReceiveForwardedMessage');
        this.connection = undefined as any;
        // Get a new token for the fallback audience
        this.auth.getAccessTokenSilently({ audience: this.fallbackAudience } as any).pipe(take(1)).subscribe({
          next: (fallbackToken: any) => {
            const tokenStr = typeof fallbackToken === 'string'
              ? fallbackToken
              : fallbackToken?.access_token;
            this.setupSignalRConnection(tokenStr, this.fallbackHubUrl, true);
          },
          error: (fallbackErr) => {
            console.error('Error getting fallback access token', fallbackErr);
          }
        });
      } else {
        console.error('SignalR Error:', err.toString());
      }
    });

    // Listen for incoming image bytes from the server
    this.connection.on('ReceiveForwardedMessage', (data: Uint8Array) => {
      this.isCameraLoading = false; // Update camera loading state
      this.cdr.detectChanges(); // Trigger change detection to update the UI
      this.displayFrame(data);
    });

    // Show reconnecting state
    this.connection.onreconnecting((error) => {
      this.isCameraLoading = true;
      this.cdr.detectChanges();
      console.warn('SignalR reconnecting...', error);
    });

    // After reconnect, re-join group and restart streaming
    this.connection.onreconnected(() => {
      this.isCameraLoading = false;
      this.cdr.detectChanges();
      this.connection.invoke('JoinGroup', groupName)
        .then(() => this.connection.invoke('StartStreaming', groupName))
        .catch(err => console.error('Error rejoining group after reconnect', err));
    });

    // If connection is closed and will NOT reconnect, update UI
    this.connection.onclose((error) => {
      this.isCameraLoading = true;
      this.cdr.detectChanges();
      console.error('SignalR connection closed', error);
    });

    // Stop streaming on page unload (add and store handler)
    this.beforeUnloadHandler = () => {
      this.connection.invoke('StopStreaming', groupName)
        .catch(() => { /* Ignore errors if connection is already closed */ });
    };

    window.addEventListener('beforeunload', this.beforeUnloadHandler);
  }
  /** Store the last object URL to revoke it and prevent memory leaks */
  private lastObjectUrl: string | null = null;

  private displayFrame(data: Uint8Array) {
    if (this.loading) {
    this.latestPendingFrame = data;
    return;
  }
  this.loading = true;

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

    // Remove beforeunload event listener if it was added
    if (this.beforeUnloadHandler) {
      window.removeEventListener('beforeunload', this.beforeUnloadHandler);
      this.beforeUnloadHandler = null;
    }

    // Revoke the last object URL to free memory
    if (this.lastObjectUrl) {
      URL.revokeObjectURL(this.lastObjectUrl);
      this.lastObjectUrl = null;
    }
    
  }
}
