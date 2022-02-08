import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import { forkJoin, interval, ReplaySubject } from 'rxjs';
import { AppVersion } from 'src/app/models/AppVersion';
import { AdminStats } from 'src/app/models/AdminStats';
import { ContentLoading } from 'src/app/models/ContentLoading';
import { ApiService } from 'src/app/services/api.service';
import { ConfirmationDialogComponent } from '../../dialogs/confirmation-dialog/confirmation-dialog.component';
import { compare } from 'compare-versions';

@Component({
  selector: 'app-adminstats',
  templateUrl: './adminstats.component.html',
  styleUrls: ['./adminstats.component.css']
})
export class AdminStatsComponent implements OnInit {

  private subscription?: any;
  private timeDifference?: number;
  public secondsToNewCache?: string = '--';
  public minutesToNewCache?: string = '--';
  public stats: ContentLoading<AdminStats> = { loading: true, content: undefined };

  constructor(private api: ApiService, private toastr: ToastrService, private dialog: MatDialog, private translator: TranslateService) { }

  ngOnInit(): void {
    this.reload();
  }

  public reload() {
    this.stats = { loading: true, content: undefined };
    this.subscription?.unsubscribe();

    this.api.getSimpleData(`/meta/adminstats`).subscribe((data: AdminStats) => {
      this.stats = { loading: false, content: data };
      this.subscription = interval(1000)
           .subscribe(x => { this.getTimeDifference(); });
    }, error => {
      console.error(error);
      this.stats.loading = false;
      this.toastr.error(this.translator.instant('AdminStats.FailedToLoad'));
    });
  }

  private getTimeDifference() {
    if (this.stats.content == undefined) return;
    if (new Date(this.stats.content!.nextCache ?? '') < new Date()) {
      this.reload();
      return;
    }
    this.timeDifference = new Date(this.stats.content!.nextCache ?? '').getTime() - new Date().getTime();
    let seconds = Math.floor((this.timeDifference) / 1000 % 60);
    this.secondsToNewCache = seconds < 10 ? `0${seconds}` : seconds.toString();
    let minutes = Math.floor((this.timeDifference) / (1000 * 60) % 60);
    this.minutesToNewCache = minutes < 10 ? `0${minutes}` : minutes.toString();
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  public triggerCache() {
    const confirmDialogRef = this.dialog.open(ConfirmationDialogComponent);
    confirmDialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.api.postSimpleData(`/meta/cache`, {}).subscribe(() => {
          this.stats = { loading: true, content: undefined };
          this.toastr.success(this.translator.instant('AdminStats.Cleared'));
          setTimeout(() => {
            this.reload();
          }, 2000);
        }, (error) => {
          console.error(error);
          this.toastr.error(this.translator.instant('AdminStats.FailedToClear'));
        });
      }
    });
  }
}
