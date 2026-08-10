import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LangSwitcherComponent } from './core/i18n/lang-switcher.component';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, LangSwitcherComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  constructor(readonly authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}
