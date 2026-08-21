export interface NurseryDto {
  id: string;
  name: string;
  timeZoneId: string;
  address: string | null;
  isActive: boolean;
}

export interface CreateNurseryRequest {
  name: string;
  timeZoneId: string;
  address: string | null;
}
