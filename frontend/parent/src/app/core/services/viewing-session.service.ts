import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StartViewingSessionResponse, ViewingSessionDto } from '../models/viewing-session.models';

@Injectable({ providedIn: 'root' })
export class ViewingSessionService {
  constructor(private readonly http: HttpClient) {}

  start(
    childId: string,
    cameraId: string,
    clientType: string = 'web'
  ): Observable<StartViewingSessionResponse> {
    return this.http.post<StartViewingSessionResponse>(
      `${environment.apiUrl}/children/${childId}/cameras/${cameraId}/viewing-sessions`,
      { clientType, deviceId: null }
    );
  }

  get(sessionId: string): Observable<ViewingSessionDto> {
    return this.http.get<ViewingSessionDto>(`${environment.apiUrl}/viewing-sessions/${sessionId}`);
  }

  stop(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/viewing-sessions/${sessionId}`);
  }
}
