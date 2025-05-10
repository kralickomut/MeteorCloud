// src/app/services/user.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { enviroment } from '../../../enviroment';
import { ApiResult } from '../models/api-result';

export interface User {
  id: number;
  name: string;
  email: string;
  description?: string;
  inTotalWorkspaces: number;
  registrationDate: string;
  lastLoginDate: string;
  updatedAt: string;
  profileImageUrl?: string;
}

export interface UserUpdateRequest {
  name?: string;
  email?: string;
  description?: string;
}

export interface UserUpdateResponse extends UserUpdateRequest {}

interface UserResponse {
  user: User;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private userUrl = enviroment.userUrl + '/api';

  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {}

  getUser(userId: number): Observable<ApiResult<UserResponse>> {
    return this.http.get<ApiResult<UserResponse>>(`${this.userUrl}/users/${userId}`);
  }

  updateUser(data: UserUpdateRequest) : Observable<ApiResult<UserUpdateResponse>> {
    return this.http.put<ApiResult<UserUpdateResponse>>(`${this.userUrl}/user/`, data);
  }

  getUsersBulk(userIds: number[]): Observable<ApiResult<User[]>> {
    return this.http.post<ApiResult<User[]>>(`${this.userUrl}/users/bulk`, { userIds });
  }

  setActualLoggedUser(user: User): void {
    this.userSubject.next(user);
  }

  get currentUser(): User | null {
    return this.userSubject.getValue();
  }

  getProfileImageUrl(userId: number): string {
    return `${enviroment.fileUrl}/api/file/view/profile-images/${userId}/profile.jpg`;
  }


}
