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
}

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

  setActualLoggedUser(user: User): void {
    this.userSubject.next(user);
  }

  get currentUser(): User | null {
    return this.userSubject.getValue();
  }


}
