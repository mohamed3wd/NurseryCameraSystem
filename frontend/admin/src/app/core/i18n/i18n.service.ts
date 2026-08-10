import { Injectable, signal } from '@angular/core';
import { ADMIN_AR, ADMIN_EN, Lang, TranslationDict } from './translations';

const STORAGE_KEY = 'nurserycam.admin.lang';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly dictionaries: Record<Lang, TranslationDict> = {
    en: ADMIN_EN,
    ar: ADMIN_AR
  };

  readonly lang = signal<Lang>(this.readInitialLang());

  constructor() {
    this.applyDocument(this.lang());
  }

  t(key: string, params?: Record<string, string | number>): string {
    const dict = this.dictionaries[this.lang()];
    let value = dict[key] ?? ADMIN_EN[key] ?? key;
    if (params) {
      for (const [name, raw] of Object.entries(params)) {
        value = value.replaceAll(`{{${name}}}`, String(raw));
      }
    }
    return value;
  }

  setLang(lang: Lang): void {
    if (lang === this.lang()) {
      return;
    }
    this.lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    this.applyDocument(lang);
  }

  toggle(): void {
    this.setLang(this.lang() === 'en' ? 'ar' : 'en');
  }

  private readInitialLang(): Lang {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'ar' || stored === 'en') {
      return stored;
    }
    return navigator.language?.toLowerCase().startsWith('ar') ? 'ar' : 'en';
  }

  private applyDocument(lang: Lang): void {
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }
}
