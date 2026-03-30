import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../auth-api.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {

  form: FormGroup;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authApi: AuthApiService,
    private router: Router
  ) {
    this.form = this.fb.group({
      userNameOrEmail: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.error = null;

    this.authApi.login(this.form.value).subscribe({
      next: () => {
        const roles = this.authApi.getRoles();

        if (roles.includes('Admin') || roles.includes('Employee')) {
          this.router.navigate(['/admin']);
        } else {
          // default is Member (customer)
          this.router.navigate(['/store']);
        }
      },
      error: err => {
        console.error(err);
        this.error = 'Login failed. Please check your credentials.';
      }
    });
  }
}
