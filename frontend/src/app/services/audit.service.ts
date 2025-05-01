import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {ApiResult} from "../models/api-result";
import {enviroment} from "../../../enviroment";

export interface AuditEventModel {
  auditEventId: number;
  fileName: string;
  actionByName: string;
  action: string;
  createdOn: string; // or Date
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}


@Injectable({ providedIn: 'root' })
export class AuditService {

  private apiUrl = enviroment.auditUrl + '/api/audit';
  constructor(private http: HttpClient) {}

  getFileHistory(workspaceId: number, page: number, pageSize: number) {
    return this.http.get<ApiResult<PagedResult<AuditEventModel>>>(
      `${this.apiUrl}/file-history/${workspaceId}?page=${page}&pageSize=${pageSize}`
    );
  }
}
