import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CameraAdminDto, CreateCameraRequest } from '../models/camera.models';

@Injectable({ providedIn: 'root' })
export class CamerasService {
  constructor(private readonly http: HttpClient) {}

  getCameras(nurseryId?: string): Observable<CameraAdminDto[]> {
    const params: Record<string, string> = nurseryId ? { nurseryId } : {};
    return this.http.get<CameraAdminDto[]>(`${environment.apiUrl}/admin/cameras`, { params });
  }

  createCamera(request: CreateCameraRequest): Observable<CameraAdminDto> {
    return this.http.post<CameraAdminDto>(`${environment.apiUrl}/admin/cameras`, request);
  }

  enableCamera(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/admin/cameras/${id}/enable`, {});
  }

  disableCamera(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/admin/cameras/${id}/disable`, {});
  }
}
