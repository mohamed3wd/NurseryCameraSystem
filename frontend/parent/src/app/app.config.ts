import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { PreloadAllModules, provideRouter, withPreloading } from '@angular/router';

import { authInterceptor } from './core/interceptors/auth.interceptor';
import { CapacitorService } from './core/services/capacitor.service';
import { routes } from './app.routes';

function initializeCapacitor(capacitor: CapacitorService) {
  return () => capacitor.initialize();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    // Every route is lazy, so without preloading a parent tapping through to a live camera pays
    // for a chunk download at each hop. Preloading runs after the initial render, so it costs
    // nothing at startup and makes the child -> camera -> live view path feel instant.
    provideRouter(routes, withPreloading(PreloadAllModules)),
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeCapacitor,
      deps: [CapacitorService],
      multi: true
    }
  ]
};
