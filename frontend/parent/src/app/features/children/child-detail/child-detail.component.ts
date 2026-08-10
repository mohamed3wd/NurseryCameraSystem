import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AttendanceService } from '../../../core/services/attendance.service';
import { CamerasService } from '../../../core/services/cameras.service';
import { ChildrenService } from '../../../core/services/children.service';
import { AttendanceDto } from '../../../core/models/attendance.models';
import { CameraDto } from '../../../core/models/camera.models';
import { ChildDto } from '../../../core/models/child.models';

@Component({
  selector: 'app-child-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './child-detail.component.html',
  styleUrl: './child-detail.component.scss'
})
export class ChildDetailComponent implements OnInit {
  private readonly i18n = inject(I18nService);

  readonly child = signal<ChildDto | null>(null);
  readonly attendance = signal<AttendanceDto | null>(null);
  readonly cameras = signal<CameraDto[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  private childId = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly childrenService: ChildrenService,
    private readonly attendanceService: AttendanceService,
    private readonly camerasService: CamerasService
  ) {}

  get isCheckedIn(): boolean {
    return this.attendance()?.status === 'PRESENT';
  }

  get canShowCameras(): boolean {
    return this.isCheckedIn && !!this.child()?.canViewCamera;
  }

  ngOnInit(): void {
    this.childId = this.route.snapshot.paramMap.get('childId') ?? '';

    forkJoin({
      child: this.childrenService.getChild(this.childId),
      attendance: this.attendanceService.getCurrentAttendance(this.childId)
    }).subscribe({
      next: ({ child, attendance }) => {
        this.child.set(child);
        this.attendance.set(attendance);
        this.isLoading.set(false);

        if (attendance?.status === 'PRESENT' && child.canViewCamera) {
          this.loadCameras();
        }
      },
      error: () => {
        this.errorMessage.set(this.i18n.t('child.loadError'));
        this.isLoading.set(false);
      }
    });
  }

  startViewing(camera: CameraDto): void {
    this.router.navigate(['/children', this.childId, 'cameras', camera.id, 'view']);
  }

  private loadCameras(): void {
    this.camerasService.getCamerasForChild(this.childId).subscribe({
      next: (cameras) => this.cameras.set(cameras),
      error: () => this.cameras.set([])
    });
  }
}
