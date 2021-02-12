import { HttpParams } from '@angular/common/http';
import { Component, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AutoModerationEvent } from 'src/app/models/AutoModerationEvent';
import { AutoModerationEventInfo } from 'src/app/models/AutoModerationEventInfo';
import { ApiService } from 'src/app/services/api.service';

@Component({
  selector: 'app-modevents-table',
  templateUrl: './modevents-table.component.html',
  styleUrls: ['./modevents-table.component.scss']
})
export class ModeventsTableComponent implements OnInit {
  
  @Input() guildId: string;
  @Input() isAdminOrHigher: boolean = false;
  @Input() moderationEventsInfo!: Promise<AutoModerationEventInfo>;
  moderationEvents: AutoModerationEvent[] = new Array<AutoModerationEvent>();
  private lastDate: Date = undefined;
  startPage = 1;
  
  constructor(private api: ApiService, public router: Router) { }

  ngOnInit(): void {
    this.lastDate = undefined;
    this.moderationEventsInfo = this.api.getSimpleData(`/guilds/${this.guildId}/automoderations`).toPromise();
    this.moderationEventsInfo.then((data) => {
      data.events.forEach((element: AutoModerationEvent) => {
        this.moderationEvents.push(element);        
      });
      return data;
    });
  }

  loadMoreData(): void {
    let params = new HttpParams()
          .set('startPage', this.startPage.toString());

    this.startPage++;
    this.api.getSimpleData(`/guilds/${this.guildId}/automoderations`, true, params).subscribe((data) => {
      data.events.forEach((element: AutoModerationEvent) => {
        this.moderationEvents.push(element);        
      });
    })
  }

  isEqualToLastDate(date: Date): boolean {
    let b;
    if (this.lastDate) {
      b = new Date(date).getDate() === this.lastDate.getDate();
    } else {
      b = false;
    }
    this.lastDate = new Date(date);
    return b;
  }
}