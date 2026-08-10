import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ChildrenService } from '../../../core/services/children.service';
import { ChildDto } from '../../../core/models/child.models';

@Component({
  selector: 'app-children-list',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './children-list.component.html',
  styleUrl: './children-list.component.scss'
})
export class ChildrenListComponent implements OnInit {
  private readonly i18n = inject(I18nService);

  readonly children = signal<ChildDto[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor(private readonly childrenService: ChildrenService) {}

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

  initials(child: ChildDto): string {
    return `${child.firstName.charAt(0)}${child.lastName.charAt(0)}`.toUpperCase();
  }
}
