import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import { ApiEnumTypes } from 'src/app/models/ApiEnumTypes';
import { ApiEnum } from 'src/app/models/ApiEnum';
import { ContentLoading } from 'src/app/models/ContentLoading';
import { IAppSettings } from 'src/app/models/IAppSettings';
import { ApiService } from 'src/app/services/api.service';
import { EnumManagerService } from 'src/app/services/enum-manager.service';

@Component({
  selector: 'app-appsettings',
  templateUrl: './appsettings.component.html',
  styleUrls: ['./appsettings.component.css']
})
export class AppsettingsComponent implements OnInit {

  initRows: number = 2;
  settingsLoading: boolean = true;
  embedFormGroup!: FormGroup;
  settingsFormGroup!: FormGroup;

  maxLength256 = { length: 256 };
  maxLength4096 = { length: 4096 };

  public allLanguages: ApiEnum[] = [];

  constructor(private toastr: ToastrService, private api: ApiService, private _formBuilder: FormBuilder, private translator: TranslateService, private enumManager: EnumManagerService) { }

  ngOnInit(): void {
    this.embedFormGroup = this._formBuilder.group({
      title: ['', [ Validators.required, Validators.maxLength(256) ]],
      content: ['', [ Validators.maxLength(4096) ]]
    });
    this.settingsFormGroup = this._formBuilder.group({
      defaultLanguage: ['', Validators.required ],
      auditLogWebhookUrl: ['',  Validators.pattern('https://discord(app)?\.com/api/webhooks/[0-9]+/.+') ],
      publicFileMode: ['']
    });

    this.api.getSimpleData('/settings').subscribe((data: IAppSettings) => {
      this.initRows = Math.max(Math.min(data?.embedContent?.split(/\r\n|\r|\n/)?.length ?? 0, 15), 2);
      this.embedFormGroup.setValue({
        title: data.embedTitle,
        content: data.embedContent
      });
      this.settingsFormGroup.setValue({
        defaultLanguage: data.defaultLanguage,
        auditLogWebhookUrl: data.auditLogWebhookUrl,
        publicFileMode: data.publicFileMode
      });

      this.settingsLoading = false;
    }, error => {
      this.settingsLoading = false;
      console.error(error);
    });

    this.enumManager.getEnum(ApiEnumTypes.LANGUAGE).subscribe((data: ApiEnum[]) => {
      this.allLanguages = data;
    });
  }

  updateEmbed() {
    let body = {
      embedTitle: this.embedFormGroup.value.title,
      embedContent: this.embedFormGroup.value.content
    }
    this.api.putSimpleData('/settings/embed', body, undefined, true, true).subscribe((data: IAppSettings) => {
      this.toastr.success(this.translator.instant("AppSettings.Embed.Save.Message"));
      this.settingsLoading = false;
      this.initRows = Math.max(Math.min(data?.embedContent?.split(/\r\n|\r|\n/)?.length ?? 0, 15), 2);
      this.embedFormGroup.setValue({
        title: data.embedTitle,
        content: data.embedContent
      });
    }, error => {
      this.settingsLoading = false;
      console.error(error);
    });
  }

  updateSettings() {
    let body = {
      defaultLanguage: this.settingsFormGroup.value?.defaultLanguage ?? 0,
      auditLogWebhookUrl: this.settingsFormGroup.value?.auditLogWebhookUrl ?? null,
      publicFileMode: this.settingsFormGroup.value?.publicFileMode ?? false
    }
    this.api.putSimpleData('/settings/infrastructure', body, undefined, true, true).subscribe((data: IAppSettings) => {
      this.toastr.success(this.translator.instant("AppSettings.Embed.Save.Message"));
      this.settingsLoading = false;
      this.settingsFormGroup.setValue({
        defaultLanguage: data.defaultLanguage,
        auditLogWebhookUrl: data.auditLogWebhookUrl,
        publicFileMode: data.publicFileMode
      });
    }, error => {
      this.settingsLoading = false;
      console.error(error);
    });
  }

  get embedTitle() { return this.embedFormGroup.get('title'); }
  get embedContent() { return this.embedFormGroup.get('content'); }

  get settingsDefaultLanguage() { return this.settingsFormGroup.get('defaultLanguage'); }
  get settingsAuditLogWebhookURL() { return this.settingsFormGroup.get('auditLogWebhookUrl'); }
  get settingsPublicFileMode() { return this.settingsFormGroup.get('publicFileMode'); }
}
