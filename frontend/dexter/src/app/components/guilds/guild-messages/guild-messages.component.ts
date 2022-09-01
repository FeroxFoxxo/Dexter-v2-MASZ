import { HttpParams } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Moment } from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ReplaySubject } from 'rxjs';
import { DiscordChannel } from 'src/app/models/DiscordChannel';
import { ApiService } from 'src/app/services/api.service';

@Component({
  selector: 'app-guild-messages',
  templateUrl: './guild-messages.component.html',
  styleUrls: ['./guild-messages.component.css']
})
export class GuildMessagesComponent implements OnInit {

  public guildId!: string;

  public messages: any[] = [];
  public apiPage: number = 1;
  public loadingFurtherMessages: boolean = false;

  public channels: DiscordChannel[] = [];

  maxLength4096 = { length: 4096 };
  public newMessageLoading: boolean = false;
  public newMessageForm!: FormGroup;
  public scheduledForChangedForPicker: ReplaySubject<Date> = new ReplaySubject<Date>(1);

  constructor(private _formBuilder: FormBuilder, private toastr: ToastrService, private api: ApiService, private route: ActivatedRoute, private translator: TranslateService) { }

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('guildid') as string;

    this.newMessageForm = this._formBuilder.group({
      name: ['', [ Validators.required ]],
      content: ['', [ Validators.required, Validators.maxLength(4096) ]],
      channel: ['', [ Validators.required ]],
      scheduledFor: ['', [ Validators.required ]]
    });

    this.api.getSimpleData(`/guilds/${this.guildId}/scheduledmessages`).subscribe((data) => {
      this.messages.push(...data);
    });

    this.api.getSimpleData(`/discord/guilds/${this.guildId}/channels`).subscribe((data: DiscordChannel[]) => {
      this.channels = data.filter(x => x.type === 0).sort((a, b) => (a.position > b.position) ? 1 : -1);
    });
  }

  queueMessage() {
    let body = {
      name: this.newMessageForm.value.name,
      content: this.newMessageForm.value.content,
      channelId: this.newMessageForm.value.channel,
      scheduledFor: this.newMessageForm.value.scheduledFor?.toISOString()
    }

    this.newMessageLoading = true;

    this.api.postSimpleData(`/guilds/${this.guildId}/scheduledmessages`, body, undefined, true, true).subscribe((data) => {
      this.newMessageForm.setValue({
        name: "",
        content: "",
        channel: "",
        scheduledFor: ""
      });
      this.newMessageForm.markAsPristine();
      this.newMessageForm.markAsUntouched();
      this.scheduledForChangedForPicker.next(undefined);
      this.messages.unshift(data);
      this.newMessageLoading = false;
    }, error => {
      console.error(error);
      this.newMessageLoading = false;
    });
  }

  newMessageDateChanged(date: Moment) {
    this.newMessageScheduledFor?.setValue(date);
  }

  messageDeleted(id: number) {
    const index = this.messages.findIndex(x => x.id === id);
    if (index > -1) {
      this.messages.splice(index, 1);
    }
  }

  loadFurtherMessages() {
    this.loadingFurtherMessages = true;
    const params = new HttpParams()
                        .set('page', this.apiPage.toString())

    this.api.getSimpleData(`/guilds/${this.guildId}/scheduledmessages`, true, params).subscribe((data) => {
      this.messages.push(...data);
      this.loadingFurtherMessages = false;
      this.apiPage++;
    }, error => {
      console.error(error);
      this.toastr.error(this.translator.instant('GuildMessages.LoadMore.Failed'))
    });
  }

  get newMessageName() { return this.newMessageForm.get('name'); }
  get newMessageContent() { return this.newMessageForm.get('content'); }
  get newMessageChannel() { return this.newMessageForm.get('channel'); }
  get newMessageScheduledFor() { return this.newMessageForm.get('scheduledFor'); }
}
