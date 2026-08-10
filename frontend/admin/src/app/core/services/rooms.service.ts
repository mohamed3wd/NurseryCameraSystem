import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateRoomRequest, RoomDto } from '../models/room.models';

@Injectable({ providedIn: 'root' })
export class RoomsService {
  constructor(private readonly http: HttpClient) {}

  getRooms(nurseryId?: string): Observable<RoomDto[]> {
    const params: Record<string, string> = nurseryId ? { nurseryId } : {};
    return this.http.get<RoomDto[]>(`${environment.apiUrl}/admin/rooms`, { params });
  }

  createRoom(request: CreateRoomRequest): Observable<RoomDto> {
    return this.http.post<RoomDto>(`${environment.apiUrl}/admin/rooms`, request);
  }
}
