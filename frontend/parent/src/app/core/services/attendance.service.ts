import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AttendanceDto } from '../models/attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  constructor(private readonly http: HttpClient) {}

  getCurrentAttendance(childId: string): Observable<AttendanceDto | null> {
    return this.http.get<AttendanceDto | null>(
      `${environment.apiUrl}/children/${childId}/attendance/current`
    );
  }
}
