export interface AttendanceDto {
  id: string;
  childId: string;
  staffId: string | null;
  checkInUtc: string;
  checkOutUtc: string | null;
  status: string;
  source: string;
  notes: string | null;
}
