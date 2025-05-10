import { Injectable } from '@angular/core';
import {enviroment} from "../../../enviroment";
import {HttpClient} from "@angular/common/http";
import {Observable, ReplaySubject, Subject} from "rxjs";
import {PagedResult} from "./audit.service";
import {ApiResult} from "../models/api-result";


export interface FastLink {
  name: string;
  token: string;
  fileId: string;
  createdAt: string;
  expiresAt: string;
  accessCount: number;
}


export interface GetLinkByTokenResponse {
  name: string;
  fileId: string;
  fileName: string;
  fileSize: number;
  createdAt: string;
  expiresAt: string;
  accessCount: number;
  createdByUser: string;
  ownerId: number;
}

@Injectable({
  providedIn: 'root'
})
export class LinkService {
  private apiUrl = `${enviroment.linkUrl}/api/links`;
  private fileUrl = `${enviroment.fileUrl}/api/file`;

  constructor(private http: HttpClient) {}

  private fastLinkCreated = new ReplaySubject<void>(1); // replays the last emission to new subscribers
  fastLinkCreated$ = this.fastLinkCreated.asObservable();


  notifyFastLinkCreated() {
    this.fastLinkCreated.next();
  }

  getUserLinks(userId: number, page: number, pageSize: number): Observable<ApiResult<PagedResult<FastLink>>> {
    return this.http.get<ApiResult<PagedResult<FastLink>>>(`${this.apiUrl}/user/${userId}?page=${page}&pageSize=${pageSize}`);
  }

  uploadFastLink(file: File, linkName: string, expirationInHours: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('linkName', linkName);
    formData.append('expiresInHours', expirationInHours.toString());

    return this.http.post(`${this.fileUrl}/fast-link/upload`, formData, {
      reportProgress: true,
      observe: 'events'
    });
  }

  refreshLink(token: string, hours: number): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/refresh`, {
      token,
      hours
    });
  }

  getLinkByToken(token: string): Observable<ApiResult<GetLinkByTokenResponse>> {
    return this.http.get<ApiResult<GetLinkByTokenResponse>>(`${this.apiUrl}/${token}`);
  }

  deleteFastLinkFile(path: string) {
    return this.http.delete<ApiResult<boolean>>(`${this.fileUrl}/fast-link/delete/${path}`);
  }
}
