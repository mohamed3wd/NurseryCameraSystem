import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { I18nService } from './i18n.service';
import { TranslatePipe } from './translate.pipe';

@Component({
  selector: 'app-lang-switcher',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="lang-switcher" role="group" [attr.aria-label]="'Language'">
      <button
        type="button"
        class="lang-btn"
        [class.active]="i18n.lang() === 'en'"
        (click)="i18n.setLang('en')"
      >
        {{ 'common.lang.en' | t }}
      </button>
      <button
        type="button"
        class="lang-btn"
        [class.active]="i18n.lang() === 'ar'"
        (click)="i18n.setLang('ar')"
      >
        {{ 'common.lang.ar' | t }}
      </button>
    </div>
  `,
  styles: `
    .lang-switcher {
      display: inline-flex;
      border: 1px solid var(--color-border);
      border-radius: 999px;
      overflow: hidden;
      background: var(--color-surface);
    }

    .lang-btn {
      border: 0;
      background: transparent;
      padding: 0.35rem 0.7rem;
      font: inherit;
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--color-text-muted);
      cursor: pointer;
    }

    .lang-btn.active {
      background: var(--color-teal-500);
      color: #fff;
    }
  `
})
export class LangSwitcherComponent {
  readonly i18n = inject(I18nService);
}
