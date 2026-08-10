export interface CameraAdminDto {
  id: string;
  nurseryId: string;
  name: string;
  location: string | null;
  status: string;
  streamProfile: string | null;
  isActive: boolean;
  lastHealthCheckUtc: string | null;
  roomIds: string[];
}

export interface CreateCameraRequest {
  nurseryId: string;
  name: string;
  location: string | null;
  rtspUrl: string;
  username: string | null;
  password: string | null;
  streamProfile: string | null;
}
