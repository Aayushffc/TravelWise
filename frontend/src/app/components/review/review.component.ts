import { Component, Input, OnInit } from '@angular/core';
import { ReviewService, Review, CreateReview } from '../../services/review.service';
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

      // Create review
      const review: CreateReview = {
        dealId: this.dealId,
        text: this.reviewForm.value.text,
        photos: photoUrls,
        rating: this.selectedRating
      };

      await lastValueFrom(this.reviewService.createReview(review));

      // Reset form and reload reviews
      this.reviewForm.reset({ rating: 5 });
      this.selectedFiles = [];
      this.previewUrls = [];
      this.selectedRating = 5;
      this.loadReviews();
    } catch (error: any) {
      console.error('Error creating review:', error);
      this.errorMessage = error.error?.message || 'Failed to submit review. Please try again later.';
    } finally {
      this.isSubmitting = false;
    }
  }
}
