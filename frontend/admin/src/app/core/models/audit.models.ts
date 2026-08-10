export interface AuditLogDto {
  id: number;
  userId: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  result: string;
  createdAtUtc: string;
  metadataJson: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
