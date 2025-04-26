import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import {HTTP_INTERCEPTORS, HttpClientModule} from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';


// My modules
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { RegisterComponent } from './pages/register/register.component';
import { NotificationBarComponent } from './shared/notification-bar/notification-bar.component';
import { FastLinkComponent } from './shared/fast-link/fast-link.component';
import { WorkspaceCreateComponent } from './shared/workspace-create/workspace-create.component';
import { LoginComponent } from './pages/login/login.component';
import { HomeComponent } from './pages/home/home.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { SidebarComponent } from './shared/sidebar/sidebar.component';
import { UserWorkspacesComponent } from './shared/user-workspaces/user-workspaces.component';
import { WorkspacesComponent } from './pages/workspaces/workspaces.component';
import { WorkspaceDetailComponent } from './pages/workspace-detail/workspace-detail.component';
import { WorkspaceTableComponent } from './shared/workspace-table/workspace-table.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { ProfileDetailComponent } from './shared/profile-detail/profile-detail.component';
import { LinksComponent } from './pages/links/links.component';
import { LinksTableComponent } from './shared/links-table/links-table.component';
import { InviteWorkspaceComponent } from './shared/invite-workspace/invite-workspace.component';
import { WorkspaceHistoryComponent } from './shared/workspace-history/workspace-history.component';
import { WorkspaceHistoryPageComponent } from './pages/workspace-history-page/workspace-history-page.component';
import { WorkspaceGeneralComponent } from './shared/workspace-general/workspace-general.component';
import { WorkspaceGeneralPageComponent } from './pages/workspace-general-page/workspace-general-page.component';

// PrimeNG modules
import { ToastModule } from 'primeng/toast';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { FloatLabelModule } from 'primeng/floatlabel';
import { PasswordModule } from 'primeng/password';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DividerModule } from 'primeng/divider';
import { DropdownModule } from 'primeng/dropdown';
import { FileUploadModule } from 'primeng/fileupload';
import { SidebarModule } from 'primeng/sidebar';
import { KeyFilterModule } from 'primeng/keyfilter';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { MenuModule } from 'primeng/menu';
import { VerifyComponent } from './pages/verify/verify.component';
import {AuthInterceptor} from "./interceptors/auth.interceptor";
import { ProfileUserComponent } from './pages/profile-user/profile-user.component';
import {SliderModule} from "primeng/slider";
import {OverlayPanelModule} from "primeng/overlaypanel";
import {RadioButtonModule} from "primeng/radiobutton";



// TODO: Logout button at sidebar - DONE
// TODO: Add a new user to workspace - DONE
// TODO: Page for viewing files from a link or downloading
// TODO: Page for workspace history - DONE
// TODO: Drag and drop files to upload - DONE
// TODO: Recent work on main page
// TODO: Color of workspace list names / links - DONE
// TODO: General workspace page info - DONE
// TODO: Settings page
// TODO: Workspace detail page blank space at bottom fix
// TODO: RESPONSIVENESS ACROSS WHOLE APP !IMPORTANT
// TODO: Filters at tables
// TODO: Click on whole row to open file tab
// TODO: Add verified animation to verify page
// TODO: When user removed from a workspace -> show notification + if he is in the workspace cancel the view immediately
// TODO: When user is invited to a workspace -> show notification
// TODO: Resend code on verification page
// TODO: Add a new user to workspace - DONE
// TODO: Progress bar for file upload


@NgModule({
  declarations: [
    AppComponent,
    RegisterComponent,
    LoginComponent,
    HomeComponent,
    MainLayoutComponent,
    SidebarComponent,
    FastLinkComponent,
    WorkspaceCreateComponent,
    NotificationBarComponent,
    UserWorkspacesComponent,
    WorkspacesComponent,
    WorkspaceDetailComponent,
    WorkspaceTableComponent,
    ProfileComponent,
    ProfileDetailComponent,
    LinksComponent,
    LinksTableComponent,
    InviteWorkspaceComponent,
    WorkspaceHistoryComponent,
    WorkspaceHistoryPageComponent,
    WorkspaceGeneralComponent,
    WorkspaceGeneralPageComponent,
    VerifyComponent,
    ProfileUserComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,

    // PrimeNG modules
    ToastModule,
    ButtonModule,
    InputTextModule,
    CardModule,
    FloatLabelModule,
    PasswordModule,
    TooltipModule,
    TableModule,
    DialogModule,
    InputTextareaModule,
    DividerModule,
    DropdownModule,
    FileUploadModule,
    HttpClientModule,
    SidebarModule,
    KeyFilterModule,
    ConfirmDialogModule,
    MenuModule,
    SliderModule,
    OverlayPanelModule,
    RadioButtonModule,
  ],
  providers: [MessageService, ConfirmationService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }],
  bootstrap: [AppComponent]
})
export class AppModule { }
