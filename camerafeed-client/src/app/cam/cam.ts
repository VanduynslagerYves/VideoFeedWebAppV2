import { Component, Input, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { CameraService } from './camera.service';

@Component({
  selector: 'app-cam',
  templateUrl: './cam.html',
  styleUrl: './cam.less'
})

export class Cam implements AfterViewInit, OnDestroy {
  @Input() cameraId!: number; // Can be set from parent
  @ViewChild('canvas', {static: false}) canvasRef!: ElementRef<HTMLCanvasElement>;

  private connection!: signalR.HubConnection;
  private img = new window.Image();
  private latestData: string | null = null;
  private loading = false;
  private hubUrl: string = 'https://localhost:7214/videoHub';

  constructor(private cameraService: CameraService) {}

  startCamera() {
    this.cameraService.startCamera(this.cameraId).subscribe({
      next: (res) => console.log('Camera started', res),
      error: (err) => console.error('Error starting camera', err)
    });
  }

ngAfterViewInit() {
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d')!;

    this.img.onload = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(this.img, 0, 0);
      this.loading = false;

      if (this.latestData) {
        const nextData = this.latestData;
        this.latestData = null;
        this.displayFrame(nextData);
      }
    };

    this.displayFrame = (data: string) => {
      if (this.loading) {
        this.latestData = data;
        return;
      }
      this.loading = true;
      this.img.src = 'data:image/jpeg;base64,' + data;
    };

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .build();

    this.connection.on('ReceiveImgBytes', (data: string) => {
      this.displayFrame(data);
    });

    this.connection
      .start()
      .then(() => {
        this.connection.invoke('JoinGroup', `camera_${this.cameraId}`);
      })
      .catch(err => console.error('SignalR Error:', err.toString()));
  }

  ngOnDestroy() {
    if (this.connection) {
      this.connection.stop();
    }
  }

  // TypeScript requires this to be declared
  private displayFrame(data: string) {}
}
