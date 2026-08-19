import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface ChildCheckedOutPayload {
  childId: string;
}

/**
 * Thin wrapper around the NurseryHub SignalR connection (spec section 22).
 * Server-pushed events are a courtesy signal only, not a substitute for
 * server-side authorization checks that already happen on every REST call.
 */
@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection: signalR.HubConnection | null = null;

  readonly childCheckedOut$ = new Subject<ChildCheckedOutPayload>();
  readonly viewingSessionRevoked$ = new Subject<{ sessionId: string; reason?: string }>();

  constructor(private readonly authService: AuthService) {}

  async connect(): Promise<void> {
    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    // A previous attempt that failed to start leaves a disconnected connection behind, still
    // holding its handlers. Dispose it before building a replacement.
    await this.disconnect();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => this.authService.accessToken ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ChildCheckedOut', (payload: ChildCheckedOutPayload) => {
      this.childCheckedOut$.next(payload);
    });

    this.connection.on('ViewingSessionRevoked', (payload: { sessionId: string; reason?: string }) => {
      this.viewingSessionRevoked$.next(payload);
    });

    try {
      await this.connection.start();
    } catch (error) {
      console.error('SignalR connection failed to start', error);
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;

    if (!connection) {
      return;
    }

    connection.off('ChildCheckedOut');
    connection.off('ViewingSessionRevoked');

    try {
      await connection.stop();
    } catch {
      // Already dropped by the server or the network; nothing left to release.
    }
  }
}
