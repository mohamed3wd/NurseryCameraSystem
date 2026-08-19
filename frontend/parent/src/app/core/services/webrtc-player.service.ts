import { Injectable } from '@angular/core';

export interface WebRtcPlayRequest {
  signalingUrl: string;
  streamToken: string;
  videoElement: HTMLVideoElement;
}

/**
 * Browser-side WebRTC receiver. Talks only to the media gateway signaling URL
 * returned by the API — never to RTSP / go2rtc directly.
 */
@Injectable({ providedIn: 'root' })
export class WebRtcPlayerService {
  private peerConnection: RTCPeerConnection | null = null;
  private iceTimeoutId: ReturnType<typeof setTimeout> | null = null;

  async play(request: WebRtcPlayRequest): Promise<void> {
    this.stop();

    const pc = new RTCPeerConnection({
      iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
    });
    this.peerConnection = pc;

    pc.addTransceiver('video', { direction: 'recvonly' });
    pc.addTransceiver('audio', { direction: 'recvonly' });

    pc.ontrack = (event) => {
      const stream = event.streams[0] ?? new MediaStream([event.track]);
      request.videoElement.srcObject = stream;
      void request.videoElement.play().catch(() => undefined);
    };

    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);

    await this.waitForIceGathering(pc);

    const response = await fetch(request.signalingUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        streamToken: request.streamToken,
        sdp: pc.localDescription?.sdp ?? offer.sdp
      })
    });

    if (!response.ok) {
      const errorBody = await response.text();
      throw new Error(errorBody || `WebRTC signaling failed (${response.status})`);
    }

    const payload = (await response.json()) as { sdp: string };
    await pc.setRemoteDescription({ type: 'answer', sdp: payload.sdp });
  }

  stop(): void {
    if (this.iceTimeoutId !== null) {
      clearTimeout(this.iceTimeoutId);
      this.iceTimeoutId = null;
    }

    if (this.peerConnection) {
      this.peerConnection.getSenders().forEach((sender) => sender.track?.stop());
      this.peerConnection.getReceivers().forEach((receiver) => receiver.track?.stop());
      this.peerConnection.close();
      this.peerConnection = null;
    }
  }

  private waitForIceGathering(pc: RTCPeerConnection): Promise<void> {
    if (pc.iceGatheringState === 'complete') {
      return Promise.resolve();
    }

    return new Promise((resolve) => {
      const finish = () => {
        if (this.iceTimeoutId !== null) {
          clearTimeout(this.iceTimeoutId);
          this.iceTimeoutId = null;
        }
        pc.removeEventListener('icegatheringstatechange', check);
        resolve();
      };

      const check = () => {
        if (pc.iceGatheringState === 'complete') {
          finish();
        }
      };

      pc.addEventListener('icegatheringstatechange', check);
      // Fallback so slow ICE doesn't hang the UI forever.
      this.iceTimeoutId = setTimeout(finish, 2000);
    });
  }
}
