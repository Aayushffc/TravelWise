import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-100">
      <div class="text-center">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto"></div>
        <p class="mt-4 text-gray-600">Processing login...</p>
      </div>
    </div>
  `,
  standalone: true
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['success'] === 'true') {
        // The token is now stored in an HTTP-only cookie
        // We can just navigate to home
        this.authService.navigateBasedOnRole();
      } else {
        const error = params['error'] || 'Authentication failed';
        this.router.navigate(['/login'], {
          queryParams: { error }
        });
      }
    });
  }
}
