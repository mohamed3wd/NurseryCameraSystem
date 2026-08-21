import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateNurseryRequest, NurseryDto } from '../models/nursery.models';

@Injectable({ providedIn: 'root' })
export class NurseriesService {
  constructor(private readonly http: HttpClient) {}

  getNurseries(): Observable<NurseryDto[]> {
    return this.http.get<NurseryDto[]>(`${environment.apiUrl}/admin/nurseries`);
  }

  createNursery(request: CreateNurseryRequest): Observable<NurseryDto> {
    return this.http.post<NurseryDto>(`${environment.apiUrl}/admin/nurseries`, request);
  }
}
