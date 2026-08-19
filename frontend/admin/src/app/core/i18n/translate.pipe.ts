import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from './i18n.service';
import { Lang } from './translations';

type TranslateParams = Record<string, string | number>;

/**
 * Impure by necessity: the rendered text depends on the active language, not only on the key
 * passed in, so a pure pipe would keep serving the old language after a switch. Reading the
 * `lang` signal inside `transform` is what makes the surrounding view re-render on a switch.
 *
 * Because an impure pipe runs on every change detection pass, each binding memoizes its own last
 * result: the steady-state cost is a few comparisons instead of a dictionary lookup plus
 * placeholder interpolation, across roughly 145 bindings per app.
 */
@Pipe({
  name: 't',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  private lastKey?: string;
  private lastParams?: TranslateParams;
  private lastLang?: Lang;
  private lastValue = '';

  transform(key: string, params?: TranslateParams): string {
    const lang = this.i18n.lang();

    if (lang === this.lastLang && key === this.lastKey && sameParams(this.lastParams, params)) {
      return this.lastValue;
    }

    this.lastLang = lang;
    this.lastKey = key;
    this.lastParams = params;
    this.lastValue = this.i18n.t(key, params);
    return this.lastValue;
  }
}

function sameParams(a?: TranslateParams, b?: TranslateParams): boolean {
  if (a === b) {
    return true;
  }
  if (!a || !b) {
    return false;
  }

  const keys = Object.keys(a);
  return keys.length === Object.keys(b).length && keys.every((key) => a[key] === b[key]);
}
