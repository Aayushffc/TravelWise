import { Injectable } from '@angular/core';
import { Router, CanActivate } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Observable, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AgencyGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean> {
    return this.authService.getUserRole().pipe(
      map(role => {
        if (role === 'Agency') {
          return true;
        }
        this.router.navigate(['/home']);
        return false;
      })
    );
  }
}
