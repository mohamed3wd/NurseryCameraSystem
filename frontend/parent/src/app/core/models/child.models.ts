export interface ChildDto {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  roomId: string | null;
  roomName: string | null;
  enrollmentStatus: string;
  isActive: boolean;
  canViewCamera: boolean;
}
