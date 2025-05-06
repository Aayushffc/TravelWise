import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { WishlistService } from '../../services/wishlist.service';
import { DealResponseDto } from '../../models/deal.model';
import { AuthService, AuthResponse } from '../../services/auth.service';
import { SidebarComponent } from '../side-bar/sidebar.component';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent],
  templateUrl: './wishlist.component.html',
  styleUrls: ['./wishlist.component.css']
})
export class WishlistComponent implements OnInit {
  wishlistItems: DealResponseDto[] = [];
  isLoading = true;
  error: string | null = null;
  isLoggedIn = false;
  user: AuthResponse | null = null;

  constructor(
    private wishlistService: WishlistService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.isLoggedIn = this.authService.isAuthenticated();
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.user = this.authService.getCurrentUser();
    this.loadWishlist();
  }

  goBack() {
    this.router.navigate(['/home']);
  }

  loadWishlist() {
    this.isLoading = true;
    this.wishlistService.getWishlist().subscribe({
      next: (items) => {
        this.wishlistItems = items;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading wishlist:', error);
        this.error = 'Failed to load wishlist';
        this.isLoading = false;
      }
    });
  }

  removeFromWishlist(dealId: number) {
    this.wishlistService.removeFromWishlist(dealId).subscribe({
      next: () => {
        this.wishlistItems = this.wishlistItems.filter(item => item.id !== dealId);
      },
      error: (error) => {
        console.error('Error removing from wishlist:', error);
        this.error = 'Failed to remove from wishlist';
      }
    });
  }

  getDiscountPercentage(price: number, discountedPrice: number | null): number {
    if (!discountedPrice) return 0;
    return Math.round(((price - discountedPrice) / price) * 100);
  }
}
