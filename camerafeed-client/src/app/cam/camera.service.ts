import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { AuthService } from '@auth0/auth0-angular';

@Injectable({ providedIn: 'root' })
export class CameraService {
  private baseUrl = 'https://localhost:7214/api/camera';

  constructor(private http: HttpClient, private auth: AuthService) {
    console.log("camera service instantiated");
  }

  startCamera(cameraId: number): Observable<any> {
    return this.auth.getAccessTokenSilently().pipe(
      switchMap(token => {
        const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
        return this.http.post(`${this.baseUrl}/startcam/${cameraId}`, {}, { headers });
      })
    );
  }
}
