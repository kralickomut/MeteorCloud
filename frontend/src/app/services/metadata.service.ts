import { Injectable } from '@angular/core';
import {enviroment} from "../../../enviroment";
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {ApiResult} from "../models/api-result";
import {FolderNode} from "../models/folder-node";

interface CreateFolderRequest {
  workspaceId: number;
  name: string;
  path: string;
  uploadedBy: number
}

@Injectable({
  providedIn: 'root'
})
export class MetadataService {
  private apiUrl: string = `${enviroment.metadataUrl}/api`;

  constructor(private http: HttpClient) { }

  getTreeByWorkspaceId(id: number): Observable<ApiResult<FolderNode>> {
    return this.http.get<ApiResult<FolderNode>>(`${this.apiUrl}/metadata/tree/${id}`);
  }

  createFolder(request: CreateFolderRequest): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/metadata/folder`, request);
  }


}
