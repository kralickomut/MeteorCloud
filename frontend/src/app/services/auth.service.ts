import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { enviroment} from "../../../enviroment";
import {catchError, map, Observable, switchMap, tap, throwError} from 'rxjs';
import {ApiResult} from "../models/api-result";
import {User, UserService} from "./user.service";


interface RegisterRequest {
  name: string,
  email: string,
  password: string,
}

interface LoginModel {
  email: string,
  password: string,
}

interface VerifyRequest {
  code: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private authUrl = enviroment.authUrl + '/api';

  constructor(private http: HttpClient, private userService: UserService) { }

  register(data: RegisterRequest): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.authUrl}/auth/register`, data);
  }

  verifyCode(data: VerifyRequest): Observable<ApiResult<null>> {
    return this.http.post<ApiResult<null>>(`${this.authUrl}/auth/verify`, data);
  }

  resendVerificationCode(email: string): Observable<any> {
    return this.http.post(`${this.authUrl}/auth/resend`, { email });
  }

  loginAndStore(data: LoginModel): Observable<boolean> {
    return this.http
      .post<ApiResult<{ token: string; userId: number }>>(`${this.authUrl}/auth/login`, data)
      .pipe(
        map(res => {
          if (!res.success || !res.data?.token || !res.data.userId) {
            throw new Error(res.error?.message || 'Login failed.');
          }

          localStorage.setItem('auth_token', res.data.token);
          localStorage.setItem('user_id', res.data.userId.toString());

          return true;
        }),
        catchError(err => {
          const errorMessage = err?.error?.message || err?.message || 'Login failed.';
          return throwError(() => new Error(errorMessage));
        })
      );
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_id');
  }

}
