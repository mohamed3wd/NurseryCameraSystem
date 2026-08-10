import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditLogDto, PagedResult } from '../models/audit.models';

export interface AuditLogFilter {
  fromUtc?: string;
  toUtc?: string;
  action?: string;
  userId?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  constructor(private readonly http: HttpClient) {}

  getAuditLogs(filter: AuditLogFilter = {}): Observable<PagedResult<AuditLogDto>> {
    const params: Record<string, string> = {};
    if (filter.fromUtc) params['fromUtc'] = filter.fromUtc;
    if (filter.toUtc) params['toUtc'] = filter.toUtc;
    if (filter.action) params['action'] = filter.action;
    if (filter.userId) params['userId'] = filter.userId;
    params['page'] = String(filter.page ?? 1);
    params['pageSize'] = String(filter.pageSize ?? 50);

    return this.http.get<PagedResult<AuditLogDto>>(`${environment.apiUrl}/admin/audit-logs`, {
      params
    });
  }
}
