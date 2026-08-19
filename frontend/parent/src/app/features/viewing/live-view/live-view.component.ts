import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { SignalrService } from '../../../core/services/signalr.service';
import { ViewingSessionService } from '../../../core/services/viewing-session.service';
import { WebRtcPlayerService } from '../../../core/services/webrtc-player.service';
import { StartViewingSessionResponse } from '../../../core/models/viewing-session.models';

type ViewingState = 'starting' | 'connecting' | 'live' | 'ended' | 'error';

@Component({
  selector: 'app-live-view',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './live-view.component.html',
  styleUrl: './live-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LiveViewComponent implements OnInit {
  private readonly liveVideo = viewChild<ElementRef<HTMLVideoElement>>('liveVideo');

  private readonly i18n = inject(I18nService);
  private readonly webrtcPlayer = inject(WebRtcPlayerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly viewingSessionService = inject(ViewingSessionService);
  private readonly signalrService = inject(SignalrService);
  private readonly destroyRef = inject(DestroyRef);

  readonly state = signal<ViewingState>('starting');
  readonly session = signal<StartViewingSessionResponse | null>(null);
  readonly endReason = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly canStop = computed(() => this.state() === 'live' || this.state() === 'connecting');
  readonly statusLabel = computed(() => {
    switch (this.state()) {
      case 'live':
        return this.i18n.t('view.live');
      case 'starting':
      case 'connecting':
        return this.i18n.t('view.connecting');
      default:
        return this.i18n.t('view.endedStatus');
    }
  });

  childId = '';
  cameraId = '';

  private hasEndedOnServer = false;
  private mediaAttached = false;

  constructor() {
    // Replaces an AfterViewChecked poll: the query signal fires exactly once, when the video
    // element is first rendered, instead of on every change detection pass for the whole session.
    effect(() => {
      const video = this.liveVideo()?.nativeElement;
      const session = this.session();

      if (video && session && !this.mediaAttached) {
        this.mediaAttached = true;
        void this.attachWebRtc(session, video);
      }
    });

    this.destroyRef.onDestroy(() => {
      this.webrtcPlayer.stop();
      void this.signalrService.disconnect();
      this.stopOnServerIfNeeded();
    });
  }

  ngOnInit(): void {
    this.childId = this.route.snapshot.paramMap.get('childId') ?? '';
    this.cameraId = this.route.snapshot.paramMap.get('cameraId') ?? '';

    this.startSession();
    this.listenForRealtimeEvents();
  }

  stopViewing(): void {
    this.webrtcPlayer.stop();
    this.clearVideo();
    this.stopOnServerIfNeeded();
    this.state.set('ended');
    this.endReason.set(this.i18n.t('view.youEnded'));
  }

  goBack(): void {
    this.router.navigate(['/children', this.childId]);
  }

  private startSession(): void {
    this.viewingSessionService
      .start(this.childId, this.cameraId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.session.set(response);
          this.state.set('connecting');
        },
        error: () => {
          this.state.set('error');
          this.errorMessage.set(this.i18n.t('view.startError'));
        }
      });
  }

  private async attachWebRtc(
    response: StartViewingSessionResponse,
    video: HTMLVideoElement
  ): Promise<void> {
    if (!response.signalingUrl || response.mediaProtocol === 'webrtc-mock') {
      // Fallback for mock provider: show live state without a real peer connection.
      this.state.set('live');
      return;
    }

    try {
      await this.webrtcPlayer.play({
        signalingUrl: response.signalingUrl,
        streamToken: response.streamToken,
        videoElement: video
      });
      this.state.set('live');
    } catch {
      this.state.set('error');
      this.errorMessage.set(this.i18n.t('view.mediaError'));
      this.stopOnServerIfNeeded();
    }
  }

  private listenForRealtimeEvents(): void {
    void this.signalrService.connect();

    this.signalrService.childCheckedOut$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((payload) => {
        if (payload.childId === this.childId && this.canStop()) {
          this.endSession(this.i18n.t('view.checkedOut'));
        }
      });

    this.signalrService.viewingSessionRevoked$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((payload) => {
        if (payload.sessionId === this.session()?.sessionId && this.canStop()) {
          this.endSession(payload.reason ?? this.i18n.t('view.revoked'));
        }
      });
  }

  private endSession(reason: string): void {
    this.hasEndedOnServer = true;
    this.webrtcPlayer.stop();
    this.clearVideo();
    this.state.set('ended');
    this.endReason.set(reason);
  }

  private stopOnServerIfNeeded(): void {
    const sessionId = this.session()?.sessionId;
    if (!sessionId || this.hasEndedOnServer) {
      return;
    }

    this.hasEndedOnServer = true;
    this.viewingSessionService.stop(sessionId).subscribe({ error: () => undefined });
  }

  private clearVideo(): void {
    const video = this.liveVideo()?.nativeElement;
    if (video) {
      video.srcObject = null;
    }
  }
}
