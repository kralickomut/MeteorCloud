import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {Observable, Subject} from 'rxjs';
import { enviroment } from "../../../enviroment";
import { ApiResult } from "../models/api-result";
import {InviteToWorkspace, Workspace} from "../models/WorkspaceFile";
import {UserService} from "./user.service";
import * as signalR from '@microsoft/signalr';

interface CreateWorkspace {
  name: string;
  description: string;
  ownerId: string;
  ownerName: string;
}

export interface WorkspaceInvitation {
  token: string;
  workspaceId: number;
  email: string;
  invitedByUserId: number;
  status: 'Accepted' | 'Declined' | 'Pending' | undefined;
  acceptedByUserId?: number;
  createdOn: string;
  acceptedOn?: string;
}

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {
  private apiUrl: string = `${enviroment.workspaceUrl}/api`;

  private workspaceCreatedSubject = new Subject<Workspace>();
  workspaceCreated$ = this.workspaceCreatedSubject.asObservable();

  private workspaceJoinedSubject = new Subject<Workspace>();
  workspaceJoined$ = this.workspaceJoinedSubject.asObservable();

  private workspaceHubConnection!: signalR.HubConnection;

  constructor(private http: HttpClient, private userService: UserService) {}

  startConnection(token: string) {
    this.workspaceHubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${enviroment.workspaceUrl}/hub/workspaces`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.workspaceHubConnection
      .start()
      .then(() => console.log('✅ Connected to WorkspaceHub'))
      .catch(err => console.error('❌ Error connecting to WorkspaceHub', err));

    this.workspaceHubConnection.on('WorkspaceJoined', (workspace: Workspace) => {
      console.log('📥 Workspace joined via SignalR', workspace);
      this.workspaceJoinedSubject.next(workspace);
    });
  }

  createWorkspace(data: CreateWorkspace): Observable<ApiResult<Workspace>> {
    data.ownerId = localStorage.getItem('user_id') || '';
    data.ownerName = this.userService.currentUser?.name || '';
    return this.http.post<ApiResult<Workspace>>(`${this.apiUrl}/workspace`, data);
  }

  getUserWorkspaces(userId: number, page: number = 1, pageSize: number = 10): Observable<ApiResult<Workspace[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResult<Workspace[]>>(`${this.apiUrl}/workspaces/user/${userId}`, { params });
  }

  emitWorkspaceCreated(workspace: Workspace) {
    this.workspaceCreatedSubject.next(workspace);
  }


  deleteWorkspace(id: number): Observable<ApiResult<boolean>> {
    return this.http.delete<ApiResult<boolean>>(`${this.apiUrl}/workspace/${id}`);
  }

  inviteToWorkspace(data: InviteToWorkspace) : Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/workspace/invite`, data);
  }

  getWorkspaceById(id: number): Observable<ApiResult<Workspace>> {
    return this.http.get<ApiResult<Workspace>>(`${this.apiUrl}/workspace/${id}`);
  }

  getWorkspaceInvitation(token: string) : Observable<WorkspaceInvitation> {
    return this.http.get<WorkspaceInvitation>(`${this.apiUrl}/workspace/invitations/${token}`);
  }

  respondToInvitation(token: string, accept: boolean): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/workspace/invite/respond`, {
      token,
      accept
    });
  }


}
