import { Input } from '@angular/core';
import { Component, OnInit } from '@angular/core';
import { DiscordGuild } from 'src/app/models/DiscordGuild';

@Component({
  selector: 'app-guild-icon',
  templateUrl: './guild-icon.component.html',
  styleUrls: ['./guild-icon.component.css']
})
export class GuildIconComponent {

  @Input() public guild?: DiscordGuild = undefined;
  @Input() public width: string = '24px';
  @Input() public height: string = '24px';
  @Input() public showBorder: boolean = true;
  @Input() public darkBorder: boolean = false;
  @Input() public staticBackground: boolean = true;

  constructor() { }

}
