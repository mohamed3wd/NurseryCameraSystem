import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NurseryDto } from '../../../core/models/nursery.models';
import { NurseriesService } from '../../../core/services/nurseries.service';

@Component({
  selector: 'app-nurseries-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './nurseries-list.component.html',
  styleUrl: './nurseries-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NurseriesListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly nurseries = signal<NurseryDto[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly showForm = signal(false);

  readonly form = this.formBuilder.group({
    name: ['', [Validators.required]],
    timeZoneId: ['UTC', [Validators.required]],
    address: ['']
  });

  constructor(private readonly nurseriesService: NurseriesService) {}

  ngOnInit(): void {
    this.loadNurseries();
  }

  loadNurseries(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.nurseriesService.getNurseries().subscribe({
      next: (nurseries) => {
        this.nurseries.set(nurseries);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('nurseries.loadError');
        this.isLoading.set(false);
      }
    });
  }

  toggleForm(): void {
    this.showForm.set(!this.showForm());
    this.formError.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);

    const { name, timeZoneId, address } = this.form.getRawValue();

    this.nurseriesService
      .createNursery({
        name: name!,
        timeZoneId: timeZoneId!,
        address: address || null
      })
      .subscribe({
        next: (nursery) => {
          this.nurseries.set([nursery, ...this.nurseries()]);
          this.isSubmitting.set(false);
          this.form.reset({ timeZoneId: 'UTC' });
          this.showForm.set(false);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.formError.set('nurseries.createError');
        }
      });
  }
}
