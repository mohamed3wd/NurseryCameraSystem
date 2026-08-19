import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { LangSwitcherComponent } from './core/i18n/lang-switcher.component';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, TranslatePipe, LangSwitcherComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  readonly authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
  }
}
