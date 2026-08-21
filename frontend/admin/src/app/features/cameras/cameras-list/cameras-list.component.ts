import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { CameraAdminDto } from '../../../core/models/camera.models';
import { NurseryDto } from '../../../core/models/nursery.models';
import { CamerasService } from '../../../core/services/cameras.service';
import { NurseriesService } from '../../../core/services/nurseries.service';

@Component({
  selector: 'app-cameras-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './cameras-list.component.html',
  styleUrl: './cameras-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CamerasListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly cameras = signal<CameraAdminDto[]>([]);
  readonly nurseries = signal<NurseryDto[]>([]);
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

  constructor(
    private readonly camerasService: CamerasService,
    private readonly nurseriesService: NurseriesService
  ) {}

  ngOnInit(): void {
    this.loadCameras();
    this.loadNurseries();
  }

  loadCameras(): void {
    this.isLoading.set(true);
    this.camerasService.getCameras().subscribe({
      next: (cameras) => {
        this.cameras.set(cameras);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('cameras.loadError');
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
          this.form.reset({ streamProfile: 'main', nurseryId: this.defaultNurseryId() });
          this.showForm.set(false);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.formError.set('cameras.createError');
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
