import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuditService } from '../../../core/services/audit.service';
import { AuditLogDto } from '../../../core/models/audit.models';

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './audit-log-list.component.html',
  styleUrl: './audit-log-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuditLogListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly filterForm = this.formBuilder.group({
    action: [''],
    userId: ['']
  });

  readonly logs = signal<AuditLogDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 25;
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor(private readonly auditService: AuditService) {}

  ngOnInit(): void {
    this.load();
  }

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }
    this.page.set(page);
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { action, userId } = this.filterForm.getRawValue();

    this.auditService
      .getAuditLogs({
        action: action || undefined,
        userId: userId || undefined,
        page: this.page(),
        pageSize: this.pageSize
      })
      .subscribe({
        next: (result) => {
          this.logs.set(result.items);
          this.totalCount.set(result.totalCount);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('audit.loadError');
          this.isLoading.set(false);
        }
      });
  }
}
