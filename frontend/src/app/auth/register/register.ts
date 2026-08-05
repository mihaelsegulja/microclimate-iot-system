import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  error: string | null = null;
  loading = false;

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.error = null;

    this.auth.register(this.form.getRawValue()).subscribe({
      next: (res) => {
        if (res.success) {
          this.router.navigate(['/']);
        } else {
          this.error = res.message ?? 'Registration failed';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Registration failed. Try a different username.';
        this.loading = false;
      }
    });
  }
}
