import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {RegisterComponent} from './pages/register/register.component';
import {LoginComponent} from './pages/login/login.component';
import {HomeComponent} from './pages/home/home.component';
import {MainLayoutComponent} from './layouts/main-layout/main-layout.component';
import {WorkspacesComponent} from './pages/workspaces/workspaces.component';
import {WorkspaceDetailComponent} from './pages/workspace-detail/workspace-detail.component';
import {ProfileComponent} from './pages/profile/profile.component';
import {LinksComponent} from './pages/links/links.component';
import {WorkspaceHistoryPageComponent} from './pages/workspace-history-page/workspace-history-page.component';
import {WorkspaceGeneralPageComponent} from "./pages/workspace-general-page/workspace-general-page.component";
import {VerifyComponent} from "./pages/verify/verify.component";
import {AuthGuard} from "./guards/auth.guard";
import {ProfileUserComponent} from "./pages/profile-user/profile-user.component";

const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'workspaces', component: WorkspacesComponent },
      { path: '', redirectTo: '/home', pathMatch: 'full' },
      { path: 'workspaces/:id', component: WorkspaceDetailComponent },
      { path: 'workspaces/:id/history', component: WorkspaceHistoryPageComponent },
      { path: 'workspaces/:id/general', component: WorkspaceGeneralPageComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'profile/:id', component: ProfileUserComponent },
      { path: 'links', component: LinksComponent }
      //{ path: 'upload', component: UploadComponent },
      // other logged-in routes
    ]
  },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'verify', component: VerifyComponent },
  { path: '**', redirectTo: '/home' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
