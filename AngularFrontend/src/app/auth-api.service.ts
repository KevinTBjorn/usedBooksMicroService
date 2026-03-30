import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';

export interface RegisterDto {
  email: string;
  userName: string;
  firstName?: string;
  lastName?: string;
  password: string;
}

export interface LoginDto {
  userNameOrEmail: string;
  password: string;
}

export interface LoginResponse {
  message: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {

  private currentRoles: string[] = [];
  private readonly baseUrl = 'http://localhost:5139';

  constructor(private http: HttpClient) { }

  register(dto: RegisterDto) {
    return this.http.post(
      `${this.baseUrl}/auth/register`,
      dto,
      {
        withCredentials: true,
        responseType: 'text' as const
      }
    );
  }


  login(dto: LoginDto): Observable<void> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, dto, {
      withCredentials: true
    }).pipe(
      tap(res => {
        this.currentRoles = res.roles ?? [];
      }),
      map(() => void 0)
    );
  }

  logout() {
    return this.http.post(
      `${this.baseUrl}/auth/logout`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(() => this.currentRoles = [])
    );
  }

  getRoles(): string[] {
    return this.currentRoles;
  }

  isMember(): boolean {
    return this.currentRoles.includes('Member');
  }

  isAdmin(): boolean {
    return this.currentRoles.includes('Admin');
  }

  isEmployee(): boolean {
    return this.currentRoles.includes('Employee');
  }
}
