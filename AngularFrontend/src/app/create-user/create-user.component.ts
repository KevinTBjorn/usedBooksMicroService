import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../auth-api.service';

@Component({
  selector: 'app-create-user',
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.css']
})
export class CreateUserComponent {

  form: FormGroup;
  error: string | null = null;
  success: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authApi: AuthApiService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      userName: ['', Validators.required],
      firstName: [''],
      lastName: [''],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    });
  }

  submit(): void {
    this.error = null;
    this.success = null;

    if (this.form.invalid) {
      this.error = 'Please fill in all required fields.';
      return;
    }

    const { email, userName, firstName, lastName, password, confirmPassword } = this.form.value;

    if (password !== confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }

    this.authApi.register({
      email,
      userName,
      firstName,
      lastName,
      password
    }).subscribe({
      next: () => {
        this.success = 'Account created. Redirecting to login...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1200);
      },
      error: err => {
        console.error(err);
        this.error = 'Failed to create account.';
      }
    });
  }
}
