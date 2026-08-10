import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { CamerasService } from '../../../core/services/cameras.service';
import { CameraAdminDto } from '../../../core/models/camera.models';

@Component({
  selector: 'app-cameras-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './cameras-list.component.html',
  styleUrl: './cameras-list.component.scss'
})
export class CamerasListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly cameras = signal<CameraAdminDto[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly togglingId = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    nurseryId: ['', [Validators.required]],
    name: ['', [Validators.required]],
    location: [''],
    rtspUrl: ['', [Validators.required]],
    username: [''],
    password: [''],
    streamProfile: ['main']
  });

  constructor(private readonly camerasService: CamerasService) {}

  ngOnInit(): void {
    this.loadCameras();
  }

  loadCameras(): void {
    this.isLoading.set(true);
    this.camerasService.getCameras().subscribe({
      next: (cameras) => {
        this.cameras.set(cameras);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('We couldn\u2019t load cameras right now.');
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

    const { nurseryId, name, location, rtspUrl, username, password, streamProfile } =
      this.form.getRawValue();

    this.camerasService
      .createCamera({
        nurseryId: nurseryId!,
        name: name!,
        location: location || null,
        rtspUrl: rtspUrl!,
        username: username || null,
        password: password || null,
        streamProfile: streamProfile || null
      })
      .subscribe({
        next: (camera) => {
          this.cameras.set([camera, ...this.cameras()]);
          this.isSubmitting.set(false);
          this.form.reset({ streamProfile: 'main' });
          this.showForm.set(false);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.formError.set('Couldn\u2019t create this camera. Check the fields and try again.');
        }
      });
  }

  toggleActive(camera: CameraAdminDto): void {
    this.togglingId.set(camera.id);
    const request = camera.isActive
      ? this.camerasService.disableCamera(camera.id)
      : this.camerasService.enableCamera(camera.id);

    request.subscribe({
      next: () => {
        this.cameras.set(
          this.cameras().map((c) => (c.id === camera.id ? { ...c, isActive: !c.isActive } : c))
        );
        this.togglingId.set(null);
      },
      error: () => this.togglingId.set(null)
    });
  }
}
