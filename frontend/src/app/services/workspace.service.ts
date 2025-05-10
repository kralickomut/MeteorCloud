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

export interface RemoveUserRequest {
  userId: number;
  workspaceId: number;
}

export interface ChangeUserRoleRequest {
  userId: number;
  workspaceId: number;
  role: number;
}

export interface WorkspaceInvitationHistoryDto {
  email: string;
  invitedByName: string;
  acceptedByName: string;
  status: string;
  date: string;
  acceptedOn?: string;
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

export interface UpdateWorkspaceRequest {
  workspaceId: number;
  name: string;
  description: string;
}

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {
  private apiUrl: string = `${enviroment.workspaceUrl}/api`;
  public suppressNextRefreshToast = false;

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

  getUserWorkspaces(
    userId: number,
    page: number,
    pageSize: number,
    filters: {
      sizeFrom?: number;
      sizeTo?: number;
      sortByDate?: 'asc' | 'desc';
      sortByFiles?: 'asc' | 'desc';
    }
  ) {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filters.sizeFrom !== undefined) {
      params = params.set('sizeFrom', filters.sizeFrom.toString());
    }

    if (filters.sizeTo !== undefined) {
      params = params.set('sizeTo', filters.sizeTo.toString());
    }

    if (filters.sortByDate) {
      params = params.set('sortByDate', filters.sortByDate);
    }

    if (filters.sortByFiles) {
      params = params.set('sortByFiles', filters.sortByFiles);
    }

    return this.http.get<ApiResult<Workspace[]>>(
      `${this.apiUrl}/workspaces/user/${userId}`, { params }
    );
  }

  emitWorkspaceCreated(workspace: Workspace) {
    this.workspaceCreatedSubject.next(workspace);
  }

  removeUserFromWorkspace(request: RemoveUserRequest): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/workspaces/remove-user`, request);
  }

  changeUserRole(request: ChangeUserRoleRequest): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/workspaces/change-user-role`, request);
  }

  getInvitationHistory(workspaceId: number, page: number, pageSize: number) {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResult<{ items: WorkspaceInvitationHistoryDto[]; totalCount: number }>>(
      `${this.apiUrl}/workspace/invitations-history/${workspaceId}`,
      { params }
    );
  }

  deleteWorkspace(id: number): Observable<ApiResult<boolean>> {
    return this.http.delete<ApiResult<boolean>>(`${this.apiUrl}/workspace/${id}`);
  }

  updateWorkspace(data: { workspaceId: number, name: string, description: string }): Observable<ApiResult<boolean>> {
    return this.http.put<ApiResult<boolean>>(`${this.apiUrl}/workspaces/update`, data);
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

  getRecentWorkspaces(userId: number, limit: number = 3): Observable<ApiResult<Workspace[]>> {
    return this.http.get<ApiResult<Workspace[]>>(
      `${this.apiUrl}/workspaces/recent/${userId}?limit=${limit}`
    );
  }

  respondToInvitation(token: string, accept: boolean): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/workspace/invite/respond`, {
      token,
      accept
    });
  }

  searchWorkspaces(userId: number, query: string): Observable<ApiResult<Workspace[]>> {
    return this.http.get<ApiResult<Workspace[]>>(
      `${this.apiUrl}/workspaces/search?userId=${userId}&query=${encodeURIComponent(query)}`
    );
  }


}
