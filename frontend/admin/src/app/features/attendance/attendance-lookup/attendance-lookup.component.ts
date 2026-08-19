import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AttendanceDto } from '../../../core/models/attendance.models';

@Component({
  selector: 'app-attendance-lookup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './attendance-lookup.component.html',
  styleUrl: './attendance-lookup.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AttendanceLookupComponent {
  private readonly formBuilder = inject(FormBuilder);

  readonly lookupForm = this.formBuilder.group({
    childId: ['', [Validators.required]]
  });
  readonly notesControl = this.formBuilder.control('');

  readonly attendance = signal<AttendanceDto | null>(null);
  readonly hasLookedUp = signal(false);
  readonly isLoading = signal(false);
  readonly isActing = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  private childId = '';

  constructor(private readonly attendanceService: AttendanceService) {}

  readonly isCheckedIn = computed(() => this.attendance()?.status === 'PRESENT');

  lookup(): void {
    if (this.lookupForm.invalid) {
      this.lookupForm.markAllAsTouched();
      return;
    }

    this.childId = this.lookupForm.getRawValue().childId!;
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.actionError.set(null);

    this.attendanceService.getCurrent(this.childId).subscribe({
      next: (attendance) => {
        this.attendance.set(attendance);
        this.hasLookedUp.set(true);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('We couldn\u2019t look up attendance for that child ID.');
        this.hasLookedUp.set(false);
        this.isLoading.set(false);
      }
    });
  }

  checkIn(): void {
    this.isActing.set(true);
    this.actionError.set(null);

    this.attendanceService.checkIn(this.childId, this.notesControl.value || undefined).subscribe({
      next: (attendance) => {
        this.attendance.set(attendance);
        this.isActing.set(false);
        this.notesControl.reset('');
      },
      error: () => {
        this.isActing.set(false);
        this.actionError.set('Check-in failed. The child may already be checked in.');
      }
    });
  }

  checkOut(): void {
    this.isActing.set(true);
    this.actionError.set(null);

    this.attendanceService.checkOut(this.childId).subscribe({
      next: (attendance) => {
        this.attendance.set(attendance);
        this.isActing.set(false);
      },
      error: () => {
        this.isActing.set(false);
        this.actionError.set('Check-out failed. The child may already be checked out.');
      }
    });
  }
}
