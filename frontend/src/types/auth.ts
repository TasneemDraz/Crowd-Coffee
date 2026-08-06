export type StaffRole = 'Manager' | 'Barista';

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  username: string;
  role: StaffRole;
}
