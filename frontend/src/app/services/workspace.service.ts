import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {Observable, Subject} from 'rxjs';
import { enviroment } from "../../../enviroment";
import { ApiResult } from "../models/api-result";
import {InviteToWorkspace, Workspace} from "../models/WorkspaceFile";
import {UserService} from "./user.service";

interface CreateWorkspace {
  name: string;
  description: string;
  ownerId: string;
  ownerName: string;
}


@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {
  private apiUrl: string = `${enviroment.workspaceUrl}/api`;
  private workspaceCreatedSubject = new Subject<Workspace>();
  workspaceCreated$ = this.workspaceCreatedSubject.asObservable();

  constructor(private http: HttpClient, private userService: UserService) {}

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


}
