import { ChangeDetectorRef } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { take } from 'rxjs/operators';
import { AuthService } from '@auth0/auth0-angular';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';

export class SignalRConnection {
  private connection!: signalR.HubConnection;
  private beforeUnloadHandler: (() => void) | null = null;

    private readonly hubUrl: string = 'https://localhost:7244/clienthub';
  private readonly fallbackHubUrl = 'https://localhost:7000/clienthub';
  private readonly fallbackAudience = 'https://localhost:7000';

  constructor(
    private auth: AuthService,
    private groupName: string,
    private onFrameReceivedCallback: (data: Uint8Array) => void,
    private onLoadingChangeCallback: (isLoading: boolean) => void,
    private cdr: ChangeDetectorRef
  ) {}

  public start() {
    this.auth.isAuthenticated$.pipe(take(1)).subscribe(isAuth => {
      if (isAuth) {
        this.auth.getAccessTokenSilently().pipe(take(1)).subscribe({
          next: (token) => this.setupConnection(token),
          error: (err) => console.error('Error getting access token', err)
        });
      }
    });
  }

  private setupConnection(token: string, hubUrl: string = this.hubUrl, triedFallback: boolean = false) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    this.connection.start()
      .then(() => {
        this.connection.invoke('JoinGroup', this.groupName);
        this.connection.invoke('StartStreaming', this.groupName);
      })
      .catch(err => {
        if (!triedFallback && this.fallbackHubUrl && hubUrl !== this.fallbackHubUrl && this.fallbackAudience) {
          console.warn("Trying remote URL:", this.fallbackHubUrl);
          window.removeEventListener('beforeunload', this.beforeUnloadHandler!);
          this.connection.off('ReceiveForwardedMessage');
          this.connection = undefined as any;
          this.auth.getAccessTokenSilently({ audience: this.fallbackAudience } as any).pipe(take(1)).subscribe({
            next: (fallbackToken: any) => {
              const tokenStr = typeof fallbackToken === 'string'
                ? fallbackToken
                : fallbackToken?.access_token;
              this.setupConnection(tokenStr, this.fallbackHubUrl, true);
            },
            error: (fallbackErr) => {
              console.error('Error getting fallback access token', fallbackErr);
            }
          });
        } else {
          console.error('SignalR Error:', err.toString());
        }
      });

    this.connection.on('ReceiveForwardedMessage', (data: Uint8Array) => {
      this.onLoadingChangeCallback(false);
      this.cdr.detectChanges();
      this.onFrameReceivedCallback(data);
    });

    this.connection.onreconnecting((error) => {
      this.onLoadingChangeCallback(true);
      this.cdr.detectChanges();
      console.warn('SignalR reconnecting...', error);
    });

    this.connection.onreconnected(() => {
      this.onLoadingChangeCallback(false);
      this.cdr.detectChanges();
      this.connection.invoke('JoinGroup', this.groupName)
        .then(() => this.connection.invoke('StartStreaming', this.groupName))
        .catch(err => console.error('Error rejoining group after reconnect', err));
    });

    this.connection.onclose((error) => {
      this.onLoadingChangeCallback(true);
      this.cdr.detectChanges();
      console.error('SignalR connection closed', error);
    });

    this.beforeUnloadHandler = () => {
      this.connection.invoke('StopStreaming', this.groupName)
        .catch(() => { /* Ignore errors if connection is already closed */ });
    };

    window.addEventListener('beforeunload', this.beforeUnloadHandler);
  }

  public stop() {
    if (this.connection) {
      this.connection.stop();
    }
    if (this.beforeUnloadHandler) {
      window.removeEventListener('beforeunload', this.beforeUnloadHandler);
      this.beforeUnloadHandler = null;
    }
  }
}