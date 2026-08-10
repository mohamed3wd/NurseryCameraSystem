import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CameraDto } from '../models/camera.models';

@Injectable({ providedIn: 'root' })
export class CamerasService {
  constructor(private readonly http: HttpClient) {}

  getCamerasForChild(childId: string): Observable<CameraDto[]> {
    return this.http.get<CameraDto[]>(`${environment.apiUrl}/children/${childId}/cameras`);
  }
}
