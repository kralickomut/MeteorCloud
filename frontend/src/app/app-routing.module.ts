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
import {SharedFilePageComponent} from "./pages/shared-file-page/shared-file-page.component";
import {PublicLayoutComponent} from "./layouts/public-layout/public-layout.component";
import {SharedLayoutRouterComponent} from "./pages/shared-layout-router/shared-layout-router.component";
import {ForgotPasswordComponent} from "./pages/forgot-password/forgot-password.component";
import {ResetPasswordComponent} from "./pages/reset-password/reset-password.component";

const routes: Routes = [
  {
    path: 'shared/:token',
    component: SharedLayoutRouterComponent,
    children: [
      { path: '', component: SharedFilePageComponent }
    ]
  },

  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'verify', component: VerifyComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password/:token', component: ResetPasswordComponent },

  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      { path: 'workspaces', component: WorkspacesComponent },
      { path: 'workspaces/:id', component: WorkspaceDetailComponent },
      { path: 'workspaces/:id/history', component: WorkspaceHistoryPageComponent },
      { path: 'workspaces/:id/general', component: WorkspaceGeneralPageComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'profile/:id', component: ProfileUserComponent },
      { path: 'links', component: LinksComponent }
    ]
  },

  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
