import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  readonly loading = signal(false);
  readonly error = signal('');

  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {
    this.form = this.fb.nonNullable.group({
      // Use required only — Angular email validator can reject .local demo emails
      email: ['admin@softaxe.local', [Validators.required]],
      password: ['Admin@123', [Validators.required, Validators.minLength(4)]],
    });
  }

  submit(): void {
    this.error.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Please enter email and password.');
      return;
    }

    this.loading.set(true);
    const payload = this.form.getRawValue();

    this.auth.login(payload).subscribe({
      next: async () => {
        this.loading.set(false);
        const ok = await this.router.navigateByUrl('/dashboard');
        if (!ok) {
          // Hard fallback if router blocks for any reason
          window.location.assign('/dashboard');
        }
      },
      error: (err) => {
        this.loading.set(false);
        const msg =
          err?.error?.message ||
          (err?.status === 0
            ? 'Cannot reach API. Is the backend running on port 5080?'
            : 'Invalid credentials. Please try again.');
        this.error.set(msg);
      },
    });
  }
}
