import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-shell',
  imports: [
    RouterLink, RouterLinkActive, RouterOutlet,
    MatSidenavModule, MatListModule, MatIconModule,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss'
})
export class AppShellComponent {
  private readonly auth = inject(AuthService);

  readonly username = this.auth.username;

  signOut(): void {
    this.auth.signOut();
  }
}