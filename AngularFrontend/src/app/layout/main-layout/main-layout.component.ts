import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApiService } from '../../auth-api.service';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.css']
})
export class MainLayoutComponent {

  constructor(
    private authApi: AuthApiService,
    private router: Router
  ) {}

  get isAdminOrEmployee() {
    return this.authApi.isAdmin() || this.authApi.isEmployee();
  }

  logout() {
    this.authApi.logout().subscribe({
      next: (res) => {
        console.log('Logout successful:', res);
        this.router.navigate(['/login']);
      },
      error: err => {
        console.error('Logout failed:', err);
        this.router.navigate(['/login']);
      },
    });
  }

}
