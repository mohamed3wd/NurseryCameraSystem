export interface UserDto {
  id: string;
  email: string;
  fullName: string | null;
  phone: string | null;
  isActive: boolean;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}
