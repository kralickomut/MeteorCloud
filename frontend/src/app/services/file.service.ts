import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import {ApiResult} from "../models/api-result";
import {enviroment} from "../../../enviroment";

@Injectable({
  providedIn: 'root'
})
export class FileService {
  public apiUrl = enviroment.fileUrl + '/api/file';

  constructor(private http: HttpClient) {}

  uploadFile(file: File, workspaceId: number, folderPath: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('workspaceId', workspaceId.toString());
    formData.append('folderPath', folderPath);

    return this.http.post<any>(`${this.apiUrl}/upload`, formData, {
      reportProgress: true,
      observe: 'events' // Useful if you want to track upload progress
    });
  }

  deleteFile(fileGuid: string): Observable<ApiResult<boolean>> {
    return this.http.delete<ApiResult<boolean>>(`${this.apiUrl}/delete/${fileGuid}`);
  }

  deleteFolder(path: string): Observable<ApiResult<boolean>> {
    return this.http.delete<ApiResult<boolean>>(`${this.apiUrl}/delete-folder/${encodeURIComponent(path)}`);
  }

  downloadFile(filePath: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download/${encodeURIComponent(filePath)}`, {
      responseType: 'blob'
    });
  }
}
