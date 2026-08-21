import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { App, type AppState } from '@capacitor/app';
import { SplashScreen } from '@capacitor/splash-screen';
import { StatusBar, Style } from '@capacitor/status-bar';
import { Subject } from 'rxjs';

/**
 * Native shell integration for the Capacitor-wrapped parent app.
 * No-ops cleanly in the browser so the same bundle runs on web and mobile.
 */
@Injectable({ providedIn: 'root' })
export class CapacitorService {
  readonly isNative = Capacitor.isNativePlatform();
  readonly platform = Capacitor.getPlatform();

  /** Fires when the app moves to the background (home button, app switcher). */
  readonly paused$ = new Subject<void>();

  /** Fires when the app returns to the foreground. */
  readonly resumed$ = new Subject<void>();

  async initialize(): Promise<void> {
    if (!this.isNative) {
      return;
    }

    this.applyNativeDocumentClass();

    await this.configureChrome();
    this.listenForAppState();
  }

  private applyNativeDocumentClass(): void {
    document.body.classList.add('capacitor-native', `platform-${this.platform}`);
  }

  private async configureChrome(): Promise<void> {
    try {
      await StatusBar.setStyle({ style: Style.Dark });
      if (this.platform === 'android') {
        await StatusBar.setBackgroundColor({ color: '#ffffff' });
      }
    } catch {
      // Plugin may be unavailable in some preview builds.
    }

    try {
      await SplashScreen.hide();
    } catch {
      // Splash already hidden or not shown.
    }
  }

  private listenForAppState(): void {
    App.addListener('appStateChange', (state: AppState) => {
      if (state.isActive) {
        this.resumed$.next();
      } else {
        this.paused$.next();
      }
    });
  }
}
