import { Component, Input, OnInit, HostListener } from '@angular/core';
import { ReviewService, Review, CreateReview, UpdateReview } from '../../services/review.service';
import { FileUploadService } from '../../services/file-upload.service';
import { AuthService, AuthResponse } from '../../services/auth.service';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { trigger, transition, style, animate } from '@angular/animations';
import { lastValueFrom } from 'rxjs';

interface ReviewWithUser extends Review {
  user: {
    name: string;
    photoUrl: string;
  };
}

@Component({
  selector: 'app-review',
  templateUrl: './review.component.html',
  styleUrls: ['./review.component.css'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class ReviewComponent implements OnInit {
  @Input() dealId!: number;
  @Input() agencyId!: string;

  reviews: ReviewWithUser[] = [];
  reviewForm: FormGroup;
  isSubmitting = false;
  selectedFiles: File[] = [];
  previewUrls: string[] = [];
  errorMessage: string | null = null;
  currentUser: AuthResponse | null = null;
  hoveredRating: number = 0;
  selectedRating: number = 5;
  activeMenuId: number | null = null;
  editingReview: ReviewWithUser | null = null;
  showDeleteModal = false;
  reviewToDelete: number | null = null;

  constructor(
    private reviewService: ReviewService,
    private fileUploadService: FileUploadService,
    private authService: AuthService,
    private fb: FormBuilder
  ) {
    this.reviewForm = this.fb.group({
      text: ['', [Validators.required, Validators.minLength(10)]],
      rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]]
    });

    // Subscribe to auth state changes
    this.authService.user$.subscribe(user => {
      this.currentUser = user;
    });
  }

  ngOnInit(): void {
    this.loadReviews();
  }

  async loadReviews(): Promise<void> {
    try {
      const reviews = await lastValueFrom(this.reviewService.getDealReviews(this.dealId));
      this.reviews = reviews.map(review => ({
        ...review,
        user: {
          name: review.userName || 'Anonymous',
          photoUrl: review.userPhoto || 'assets/default-avatar.png'
        }
      }));
    } catch (error) {
      console.error('Error loading reviews:', error);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.selectedFiles = Array.from(input.files);
      this.previewUrls = [];
      this.selectedFiles.forEach(file => {
        const reader = new FileReader();
        reader.onload = (e: any) => {
          this.previewUrls.push(e.target.result);
        };
        reader.readAsDataURL(file);
      });
    }
  }

  removeImage(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.previewUrls.splice(index, 1);
  }

  setRating(rating: number): void {
    this.selectedRating = rating;
    this.reviewForm.patchValue({ rating });
  }

  onStarHover(rating: number): void {
    this.hoveredRating = rating;
  }

  onStarLeave(): void {
    this.hoveredRating = 0;
  }

  openImage(url: string): void {
    window.open(url, '_blank');
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    // Close menu when clicking outside
    if (!(event.target as HTMLElement).closest('.relative')) {
      this.activeMenuId = null;
    }
  }

  toggleMenu(reviewId: number): void {
    this.activeMenuId = this.activeMenuId === reviewId ? null : reviewId;
  }

  editReview(review: ReviewWithUser): void {
    this.editingReview = review;
    this.reviewForm.patchValue({
      text: review.text,
      rating: review.rating
    });
    this.selectedRating = review.rating;
    this.previewUrls = review.photos || [];
    this.activeMenuId = null;

    // Scroll to the review form
    const formElement = document.querySelector('.review-form');
    if (formElement) {
      formElement.scrollIntoView({ behavior: 'smooth' });
    }
  }

  async deleteReview(reviewId: number): Promise<void> {
    this.reviewToDelete = reviewId;
    this.showDeleteModal = true;
    this.activeMenuId = null;
  }

  cancelDelete(): void {
    this.showDeleteModal = false;
    this.reviewToDelete = null;
  }

  async confirmDelete(): Promise<void> {
    if (!this.reviewToDelete) return;

    try {
      await lastValueFrom(this.reviewService.deleteReview(this.reviewToDelete));
      // Remove the review from the list
      this.reviews = this.reviews.filter(review => review.id !== this.reviewToDelete);
      this.errorMessage = null;
    } catch (error: any) {
      console.error('Error deleting review:', error);
      this.errorMessage = error.error?.message || 'Failed to delete review. Please try again later.';
    } finally {
      this.showDeleteModal = false;
      this.reviewToDelete = null;
    }
  }

  async onSubmit(): Promise<void> {
    if (this.reviewForm.invalid || !this.currentUser) {
      this.errorMessage = 'Please fill in all required fields and ensure you are logged in.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    try {
      // Upload photos first
      const photoUrls: string[] = [];
      for (const file of this.selectedFiles) {
        const response = await lastValueFrom(this.fileUploadService.uploadFile(file, 'reviews'));
        if (response && response.url) {
          photoUrls.push(response.url);
        }
      }

      if (this.editingReview) {
        // Update existing review
        const updatedReview: UpdateReview = {
          text: this.reviewForm.value.text,
          photos: photoUrls.length > 0 ? photoUrls : this.editingReview.photos,
          rating: this.selectedRating
        };
        await lastValueFrom(this.reviewService.updateReview(this.editingReview.id, updatedReview));
        this.editingReview = null;
      } else {
        // Create new review
        const review: CreateReview = {
          dealId: this.dealId,
          text: this.reviewForm.value.text,
          photos: photoUrls,
          rating: this.selectedRating
        };
        await lastValueFrom(this.reviewService.createReview(review));
      }

      // Reset form and reload reviews
      this.reviewForm.reset({ rating: 5 });
      this.selectedFiles = [];
      this.previewUrls = [];
      this.selectedRating = 5;
      await this.loadReviews();
    } catch (error: any) {
      console.error('Error submitting review:', error);
      this.errorMessage = error.error?.message || 'Failed to submit review. Please try again later.';
    } finally {
      this.isSubmitting = false;
    }
  }
}
