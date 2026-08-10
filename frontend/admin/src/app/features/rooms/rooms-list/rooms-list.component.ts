import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { RoomsService } from '../../../core/services/rooms.service';
import { RoomDto } from '../../../core/models/room.models';

@Component({
  selector: 'app-rooms-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './rooms-list.component.html',
  styleUrl: './rooms-list.component.scss'
})
export class RoomsListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly rooms = signal<RoomDto[]>([]);
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

  constructor(private readonly roomsService: RoomsService) {}

  ngOnInit(): void {
    this.loadRooms();
  }

  loadRooms(): void {
    this.isLoading.set(true);
    this.roomsService.getRooms().subscribe({
      next: (rooms) => {
        this.rooms.set(rooms);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('We couldn\u2019t load rooms right now.');
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

    const { nurseryId, name, code, roomType } = this.form.getRawValue();

    this.roomsService
      .createRoom({ nurseryId: nurseryId!, name: name!, code: code!, roomType: roomType || null })
      .subscribe({
        next: (room) => {
          this.rooms.set([room, ...this.rooms()]);
          this.isSubmitting.set(false);
          this.form.reset();
          this.showForm.set(false);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.formError.set('Couldn\u2019t create this room. Check the nursery ID and try again.');
        }
      });
  }
}
