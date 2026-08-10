export interface RoomDto {
  id: string;
  nurseryId: string;
  name: string;
  code: string;
  roomType: string | null;
  isActive: boolean;
}

export interface CreateRoomRequest {
  nurseryId: string;
  name: string;
  code: string;
  roomType: string | null;
}
