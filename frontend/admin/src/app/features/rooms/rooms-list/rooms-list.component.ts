import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NurseryDto } from '../../../core/models/nursery.models';
import { RoomDto } from '../../../core/models/room.models';
import { NurseriesService } from '../../../core/services/nurseries.service';
import { RoomsService } from '../../../core/services/rooms.service';

@Component({
  selector: 'app-rooms-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './rooms-list.component.html',
  styleUrl: './rooms-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RoomsListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly rooms = signal<RoomDto[]>([]);
  readonly nurseries = signal<NurseryDto[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly showForm = signal(false);

  readonly form = this.formBuilder.group({
    nurseryId: ['', [Validators.required]],
    name: ['', [Validators.required]],
    code: ['', [Validators.required]],
    roomType: ['']
  });

  constructor(
    private readonly roomsService: RoomsService,
    private readonly nurseriesService: NurseriesService
  ) {}

  ngOnInit(): void {
    this.loadRooms();
    this.loadNurseries();
  }

  loadRooms(): void {
    this.isLoading.set(true);
    this.roomsService.getRooms().subscribe({
      next: (rooms) => {
        this.rooms.set(rooms);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('rooms.loadError');
        this.isLoading.set(false);
      }
    });
  }

  toggleForm(): void {
    this.showForm.set(!this.showForm());
    this.formError.set(null);
    if (this.showForm()) {
      this.applyDefaultNursery();
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);

    const { nurseryId, name, code, roomType } = this.form.getRawValue();

    this.roomsService
      .createRoom({ nurseryId: nurseryId!, name: name!, code: code!, roomType: roomType || null })
      .subscribe({
        next: (room) => {
          this.rooms.set([room, ...this.rooms()]);
          this.isSubmitting.set(false);
          this.form.reset({ nurseryId: this.defaultNurseryId() });
          this.showForm.set(false);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.formError.set('rooms.createError');
        }
      });
  }

  private loadNurseries(): void {
    this.nurseriesService.getNurseries().subscribe({
      next: (nurseries) => {
        this.nurseries.set(nurseries);
        this.applyDefaultNursery();
      },
      error: () => undefined
    });
  }

  private applyDefaultNursery(): void {
    const current = this.form.controls.nurseryId.value;
    if (current) {
      return;
    }

    const defaultId = this.defaultNurseryId();
    if (defaultId) {
      this.form.patchValue({ nurseryId: defaultId });
    }
  }

  private defaultNurseryId(): string {
    const list = this.nurseries();
    return list.length === 1 ? list[0].id : '';
  }
}
