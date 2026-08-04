import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LoginRequest, RegisterRequest } from '../../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  credentials: LoginRequest = { username: '', password: '' };
  registerData: RegisterRequest = { username: '', password: '', email: '', role: 'User' };
  isRegistering = false;
  showPassword = false;
  isLoading = false;
  errorMsg = '';

  constructor(private authService: AuthService, private router: Router) {}

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  toggleMode() {
    this.isRegistering = !this.isRegistering;
    this.errorMsg = '';
  }

  fillDemo(role: 'admin' | 'user') {
    this.isRegistering = false;
    if (role === 'admin') {
      this.credentials.username = 'admin';
      this.credentials.password = 'admin123';
    } else {
      this.credentials.username = 'user';
      this.credentials.password = 'user123';
    }
    this.errorMsg = '';
  }

  login() {
    if (!this.credentials.username || !this.credentials.password) {
      this.errorMsg = 'Please enter both username and password';
      return;
    }

    this.isLoading = true;
    this.errorMsg = '';

    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.error?.message || 'Invalid credentials or server error. Please try again.';
      }
    });
  }

  register() {
    if (!this.registerData.username || !this.registerData.password) {
      this.errorMsg = 'Please enter username and password';
      return;
    }

    this.isLoading = true;
    this.errorMsg = '';

    this.authService.register(this.registerData).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.error?.message || 'Registration failed. Username may already exist.';
      }
    });
  }
}
