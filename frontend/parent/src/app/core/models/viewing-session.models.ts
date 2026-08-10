export interface StartViewingSessionResponse {
  sessionId: string;
  streamToken: string;
  expiresAtUtc: string;
  mediaProtocol: string;
  signalingUrl: string | null;
}

export interface ViewingSessionDto {
  id: string;
  childId: string;
  cameraId: string;
  status: string;
  startedAtUtc: string;
  expiresAtUtc: string;
  endedAtUtc: string | null;
  endReason: string | null;
}
