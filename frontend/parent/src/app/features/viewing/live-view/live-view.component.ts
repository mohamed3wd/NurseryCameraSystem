import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
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
  styleUrl: './live-view.component.scss'
})
export class LiveViewComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('liveVideo') liveVideo?: ElementRef<HTMLVideoElement>;

  private readonly i18n = inject(I18nService);
  private readonly webrtcPlayer = inject(WebRtcPlayerService);

  readonly state = signal<ViewingState>('starting');
  readonly session = signal<StartViewingSessionResponse | null>(null);
  readonly endReason = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  childId = '';
  cameraId = '';

  private subscriptions = new Subscription();
  private hasEndedOnServer = false;
  private pendingMediaAttach = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly viewingSessionService: ViewingSessionService,
    private readonly signalrService: SignalrService
  ) {}

  ngOnInit(): void {
    this.childId = this.route.snapshot.paramMap.get('childId') ?? '';
    this.cameraId = this.route.snapshot.paramMap.get('cameraId') ?? '';

    this.startSession();
    this.listenForRealtimeEvents();
  }

  ngAfterViewChecked(): void {
    if (this.pendingMediaAttach && this.liveVideo?.nativeElement && this.session()) {
      this.pendingMediaAttach = false;
      void this.attachWebRtc(this.session()!);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.webrtcPlayer.stop();
    this.stopOnServerIfNeeded();
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
    this.viewingSessionService.start(this.childId, this.cameraId).subscribe({
      next: (response) => {
        this.session.set(response);
        this.state.set('connecting');
        this.pendingMediaAttach = true;
      },
      error: () => {
        this.state.set('error');
        this.errorMessage.set(this.i18n.t('view.startError'));
      }
    });
  }

  private async attachWebRtc(response: StartViewingSessionResponse): Promise<void> {
    const video = this.liveVideo?.nativeElement;
    if (!video) {
      this.state.set('error');
      this.errorMessage.set(this.i18n.t('view.mediaError'));
      return;
    }

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
    this.signalrService.connect();

    this.subscriptions.add(
      this.signalrService.childCheckedOut$.subscribe((payload) => {
        if (payload.childId === this.childId && (this.state() === 'live' || this.state() === 'connecting')) {
          this.hasEndedOnServer = true;
          this.webrtcPlayer.stop();
          this.clearVideo();
          this.state.set('ended');
          this.endReason.set(this.i18n.t('view.checkedOut'));
        }
      })
    );

    this.subscriptions.add(
      this.signalrService.viewingSessionRevoked$.subscribe((payload) => {
        if (payload.sessionId === this.session()?.sessionId && (this.state() === 'live' || this.state() === 'connecting')) {
          this.hasEndedOnServer = true;
          this.webrtcPlayer.stop();
          this.clearVideo();
          this.state.set('ended');
          this.endReason.set(payload.reason ?? this.i18n.t('view.revoked'));
        }
      })
    );
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
    const video = this.liveVideo?.nativeElement;
    if (video) {
      video.srcObject = null;
    }
  }
}
