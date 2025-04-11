import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { enviroment} from "../../../enviroment";
import { Observable } from 'rxjs';
import {ApiResult} from "../models/api-result";


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

  constructor(private http: HttpClient) { }

  register(data: RegisterRequest): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.authUrl}/auth/register`, data);
  }

  verifyCode(data: VerifyRequest): Observable<ApiResult<null>> {
    return this.http.post<ApiResult<null>>(`${this.authUrl}/auth/verify`, data);
  }

  resendVerificationCode(email: string): Observable<any> {
    return this.http.post(`${this.authUrl}/auth/resend`, { email });
  }

  login(data: LoginModel): Observable<ApiResult<{ token: string }>> {
    return this.http.post<ApiResult<{ token: string }>>(`${this.authUrl}/auth/login`, data);
  }

}
