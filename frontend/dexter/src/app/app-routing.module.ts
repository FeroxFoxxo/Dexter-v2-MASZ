import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminStatsComponent } from './components/api/adminstats/adminstats.component';
import { AppSettingsComponent } from './components/api/appsettings/appsettings.component';
import { IndexComponent } from './components/basic/index/index.component';
import { LegalComponent } from './components/basic/legal/legal.component';
import { PatchnotesComponent } from './components/basic/patchnotes/patchnotes.component';
import { TermsComponent } from './components/basic/terms/terms.component';
import { NotFoundComponent } from './components/errors/not-found/not-found.component';
import { OauthFailedComponent } from './components/errors/oauth-failed/oauth-failed.component';
import { GameDisplayComponent } from './components/games/game-display/game-display.component';
import { GamesDashboardComponent } from './components/games/games-dashboard/games-dashboard.component';
import { GuildAddComponent } from './components/guilds/guild-add/guild-add.component';
import { GuildEditComponent } from './components/guilds/guild-edit/guild-edit.component';
import { GuildLeaderboardComponent } from './components/guilds/guild-leaderboard/guild-leaderboard.component';
import { GuildListComponent } from './components/guilds/guild-list/guild-list.component';
import { GuildOverviewComponent } from './components/guilds/guild-overview/guild-overview.component';
import { ModCaseAddComponent } from './components/modcase/modcase-add/modcase-add.component';
import { ModCaseEditComponent } from './components/modcase/modcase-edit/modcase-edit.component';
import { ModCaseViewComponent } from './components/modcase/modcase-view/modcase-view.component';
import { ProfileDashboardComponent } from './components/profiles/profile-dashboard/profile-dashboard.component';
import { StoreDashboardComponent } from './components/profiles/store/store-dashboard/store-dashboard.component';
import { UserScanComponent } from './components/usergraph/userscan/userscan.component';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: 'guilds', component: GuildListComponent, canActivate: [AuthGuard] },
  { path: 'guilds/new', component: GuildAddComponent, canActivate: [AuthGuard] },
  { path: 'guilds/:guildid', component: GuildOverviewComponent, canActivate: [AuthGuard] },
  { path: 'guilds/:guildid/edit', component: GuildEditComponent, canActivate: [AuthGuard] },
  { path: 'guilds/:guildid/cases/new', component: ModCaseAddComponent, canActivate: [AuthGuard] },
  { path: 'guilds/:guildid/cases/:caseid', component: ModCaseViewComponent, canActivate: [AuthGuard] },
  { path: 'guilds/:guildid/cases/:caseid/edit', component: ModCaseEditComponent, canActivate: [AuthGuard] },
  { path: 'userscan', component: UserScanComponent, canActivate: [AuthGuard] },
  { path: 'scanning', component: UserScanComponent, canActivate: [AuthGuard] },
  { path: 'adminstats', component: AdminStatsComponent, canActivate: [AuthGuard] },
  { path: 'settings', component: AppSettingsComponent, canActivate: [AuthGuard] },
  { path: 'patchnotes', component: PatchnotesComponent },
  { path: 'oauthfailed', component: OauthFailedComponent },
  { path: 'store', component: StoreDashboardComponent },
  { path: 'profile', component: ProfileDashboardComponent },
  { path: 'games', component: GamesDashboardComponent },
  { path: 'games/:gameid', component: GameDisplayComponent },
  { path: 'guilds/:guildid/leaderboard', component: GuildLeaderboardComponent},
  { path: 'terms', component: TermsComponent },
  { path: 'legal', component: LegalComponent },
  { path: 'login',  component: IndexComponent, pathMatch: 'full' },
  { path: '',  redirectTo: 'login', pathMatch: 'full' },
  { path: '**', component: NotFoundComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
