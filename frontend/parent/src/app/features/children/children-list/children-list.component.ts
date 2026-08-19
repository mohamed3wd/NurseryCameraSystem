import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ChildrenService } from '../../../core/services/children.service';
import { ChildDto } from '../../../core/models/child.models';

interface ChildCard {
  readonly child: ChildDto;
  readonly initials: string;
}

@Component({
  selector: 'app-children-list',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './children-list.component.html',
  styleUrl: './children-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChildrenListComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  private readonly childrenService = inject(ChildrenService);

  readonly children = signal<ChildDto[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  // Derived once per list change instead of per child per change detection pass.
  readonly cards = computed<ChildCard[]>(() =>
    this.children().map((child) => ({
      child,
      initials: `${child.firstName.charAt(0)}${child.lastName.charAt(0)}`.toUpperCase()
    }))
  );

  ngOnInit(): void {
    this.childrenService.getMyChildren().subscribe({
      next: (children) => {
        this.children.set(children);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set(this.i18n.t('children.loadError'));
        this.isLoading.set(false);
      }
    });
  }
}
