import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Observable, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.authService.user$.pipe(
      map(user => {
        if (user !== null) {
          return true;
        }

        // If user is not authenticated, redirect to login
        // but preserve the attempted URL for redirect after login
        this.router.navigate(['/login'], {
          queryParams: { returnUrl: this.router.url }
        });
        return false;
      })
    );
  }
}
