import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChildDto } from '../models/child.models';

@Injectable({ providedIn: 'root' })
export class ChildrenService {
  constructor(private readonly http: HttpClient) {}

  getMyChildren(): Observable<ChildDto[]> {
    return this.http.get<ChildDto[]>(`${environment.apiUrl}/parent/children`);
  }

  getChild(childId: string): Observable<ChildDto> {
    return this.http.get<ChildDto>(`${environment.apiUrl}/parent/children/${childId}`);
  }
}
