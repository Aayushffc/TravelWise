import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { DealResponseDto } from '../../models/deal.model';
import { LocationService } from '../../services/location.service';
import { AuthService } from '../../services/auth.service';
import { AgencyProfileService } from '../../services/agency-profile.service';
import { BookingService, CreateBookingDTO } from '../../services/booking.service';
import { firstValueFrom } from 'rxjs';
import { NgxIntlTelInputModule } from 'ngx-intl-tel-input';
import { SearchCountryField, CountryISO, PhoneNumberFormat } from 'ngx-intl-tel-input';
import { ReviewComponent } from '../review/review.component';
import { WishlistService } from '../../services/wishlist.service';
import { CurrencyService, Currency } from '../../services/currency.service';

// Use DealResponseDto directly instead of custom Deal interface
type Deal = DealResponseDto;

interface ItineraryDay {
  dayNumber: number;
  title: string;
  description: string;
  activities: string[];
}

interface PackageOption {
  name: string;
  description: string;
  price: number;
  inclusions: string[];
}

interface Policy {
  title: string;
  description: string;
}

interface EnquiryForm {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  phoneNumberObject: any; // This will store the full phone number object
  travelDate: string;
  travelerCount: number;
  message: string;
}

@Component({
  selector: 'app-deal-details',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NgxIntlTelInputModule, ReviewComponent],
  templateUrl: './deal-details.component.html',
  styleUrls: ['./deal-details.component.css']
})
export class DealDetailsComponent implements OnInit {
  deal: DealResponseDto | null = null;
  agencyProfile: any = null;
  isLoading = true;
  error: string | null = null;
  currentImageIndex = 0;
  isWishlisted = false;
  locationName = 'Loading...';
  isLoggedIn = false;
  selectedDate: string = '';
  selectedTravelers: number = 2;
  activeTab: 'overview' | 'inclusions' | 'itinerary' | 'reviews' = 'overview';
  showFeatures = false;
  showTransfers = false;
  showEnquiryPopup = false;
  successMessage = '';
  hasExistingInquiry = false;
  showShareDropdown = false;
  shareSuccessMessage = '';
  shareErrorMessage = '';

  // Phone input configuration
  SearchCountryField = SearchCountryField;
  CountryISO = CountryISO;
  PhoneNumberFormat = PhoneNumberFormat;
  preferredCountries: CountryISO[] = [CountryISO.UnitedStates, CountryISO.UnitedKingdom, CountryISO.India];
  separateDialCode = true;
  searchCountryPlaceholder = 'Search country';
  searchCountryField = [SearchCountryField.All];
  maxLength = 15;
  phoneValidation = true;
  autoCountrySelect = true;
  numberFormat = PhoneNumberFormat.International;

  enquiryForm = {
    firstName: '',
    lastName: '',
    email: '',
    phoneNumberObject: null,
    travelDate: null,
    travelerCount: 1,
    message: '',
    specialRequirements: ''
  };

  currentCurrency: Currency = {
    code: 'USD',
    name: 'US Dollar',
    symbol: '$',
    flag: 'https://flagcdn.com/32x24/us.png'
  };

  get includedFeatures(): string[] {
    if (!this.deal) return [];

    const features = [];
    if (this.deal.elderlyFriendly) features.push('Elderly Friendly');
    if (this.deal.internetIncluded) features.push('Internet Access');
    if (this.deal.travelIncluded) features.push('Travel Included');
    if (this.deal.mealsIncluded) features.push('Meals Included');
    if (this.deal.sightseeingIncluded) features.push('Sightseeing');
    if (this.deal.stayIncluded) features.push('Accommodation');
    if (this.deal.travelCostIncluded) features.push('Travel Cost Included');
    if (this.deal.guideIncluded) features.push('Guide Included');
    if (this.deal.photographyIncluded) features.push('Photography');
    if (this.deal.insuranceIncluded) features.push('Insurance');
    if (this.deal.visaIncluded) features.push('Visa Included');

    return features;
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dealService: DealService,
    private locationService: LocationService,
    private authService: AuthService,
    private location: Location,
    private agencyProfileService: AgencyProfileService,
    private bookingService: BookingService,
    private wishlistService: WishlistService,
    private currencyService: CurrencyService
  ) {}

  async ngOnInit() {
    try {
      this.isLoggedIn = this.authService.isAuthenticated();
      const dealId = this.route.snapshot.paramMap.get('id');
      if (!dealId) {
        this.error = 'Deal ID not found';
        return;
      }

      this.deal = await firstValueFrom(this.dealService.getDealById(parseInt(dealId, 10)));
      this.loadLocationName(this.deal.locationId);

      // Get agency profile using the new API
      if (this.deal?.userId) {
        this.agencyProfile = await firstValueFrom(this.agencyProfileService.getAgencyInfoByUserId(this.deal.userId));
      }

      // Check if deal is in wishlist
      if (this.isLoggedIn && this.deal) {
        this.isWishlisted = await firstValueFrom(this.wishlistService.isInWishlist(this.deal.id));
      }

      // Pre-fill user information if logged in
      if (this.isLoggedIn) {
        const user = this.authService.getCurrentUser();
        if (user) {
          this.enquiryForm = {
            ...this.enquiryForm,
            firstName: user.firstName || '',
            lastName: user.lastName || '',
            email: user.email || ''
          };
        }
      }

      this.currencyService.getCurrentCurrency().subscribe(currency => {
        this.currentCurrency = currency;
      });
    } catch (error) {
      this.error = 'Error loading deal details';
      console.error('Error:', error);
    } finally {
      this.isLoading = false;
    }
  }

  private loadLocationName(locationId: number): void {
    this.locationService.getLocationById(locationId).subscribe({
      next: (location) => {
        this.locationName = location.name;
      },
      error: (err) => {
        console.error('Error loading location:', err);
        this.locationName = 'Location not specified';
      }
    });
  }

  nextImage(): void {
    if (this.deal?.photos && this.deal.photos.length > 0) {
      this.currentImageIndex = (this.currentImageIndex + 1) % this.deal.photos.length;
    }
  }

  previousImage(): void {
    if (this.deal?.photos && this.deal.photos.length > 0) {
      this.currentImageIndex = (this.currentImageIndex - 1 + this.deal.photos.length) % this.deal.photos.length;
    }
  }

  setCurrentImage(index: number): void {
    this.currentImageIndex = index;
  }

  toggleWishlist(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }

    if (!this.deal) return;

    if (this.isWishlisted) {
      this.wishlistService.removeFromWishlist(this.deal.id).subscribe({
        next: () => {
          this.isWishlisted = false;
        },
        error: (error) => {
          console.error('Error removing from wishlist:', error);
          this.error = 'Failed to remove from wishlist';
        }
      });
    } else {
      this.wishlistService.addToWishlist(this.deal.id).subscribe({
        next: () => {
          this.isWishlisted = true;
        },
        error: (error) => {
          console.error('Error adding to wishlist:', error);
          this.error = 'Failed to add to wishlist';
        }
      });
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const shareButton = (event.target as HTMLElement).closest('.share-button');
    const shareDropdown = (event.target as HTMLElement).closest('.share-dropdown');

    if (!shareButton && !shareDropdown) {
      this.showShareDropdown = false;
    }
  }

  share(event: MouseEvent): void {
    event.stopPropagation();
    this.showShareDropdown = !this.showShareDropdown;
  }

  async copyLink(): Promise<void> {
    try {
      const url = window.location.href;
      await navigator.clipboard.writeText(url);
      this.shareSuccessMessage = 'Link copied to clipboard!';
      this.showShareDropdown = false;
      setTimeout(() => {
        this.shareSuccessMessage = '';
      }, 3000);
    } catch (err) {
      this.shareErrorMessage = 'Failed to copy link';
      setTimeout(() => {
        this.shareErrorMessage = '';
      }, 3000);
    }
  }

  shareViaEmail(): void {
    if (!this.deal) return;

    const subject = `Check out this amazing deal on TravelWise: ${this.deal.title}`;
    const body = `Hi there!

    I found this amazing travel deal on TravelWise that I thought you might be interested in:

    ${this.deal.title}
    ${this.deal.headlines || ''}

    Price: ${this.currentCurrency.symbol}${this.getConvertedPrice(this.deal.price)}
    ${this.deal.discountedPrice ? `Discounted Price: ${this.currentCurrency.symbol}${this.getConvertedPrice(this.deal.discountedPrice)}` : ''}

    Duration: ${this.deal.daysCount} Days / ${this.deal.nightsCount} Nights
    Location: ${this.locationName}

    Check it out here: ${window.location.href}

    Best regards,
    ${this.isLoggedIn ? this.authService.getCurrentUser()?.firstName || '' : ''}`;

    const mailtoLink = `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    window.open(mailtoLink, '_blank');
    this.showShareDropdown = false;
  }

  shareViaWhatsApp(): void {
    if (!this.deal) return;

    const text = `Check out this amazing deal on TravelWise: ${this.deal.title}\n\n${window.location.href}`;
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(text)}`;
    window.open(whatsappUrl, '_blank');
    this.showShareDropdown = false;
  }

  shareViaFacebook(): void {
    if (!this.deal) return;

    const url = encodeURIComponent(window.location.href);
    const facebookUrl = `https://www.facebook.com/sharer/sharer.php?u=${url}`;
    window.open(facebookUrl, '_blank', 'width=600,height=400');
    this.showShareDropdown = false;
  }

  shareViaTwitter(): void {
    if (!this.deal) return;

    const text = `Check out this amazing deal on TravelWise: ${this.deal.title}`;
    const url = encodeURIComponent(window.location.href);
    const twitterUrl = `https://x.com/intent/tweet?text=${encodeURIComponent(text)}&url=${url}`;
    window.open(twitterUrl, '_blank', 'width=600,height=400');
    this.showShareDropdown = false;
  }

  shareViaLinkedIn(): void {
    if (!this.deal) return;

    const url = encodeURIComponent(window.location.href);
    const title = encodeURIComponent(this.deal.title);
    const summary = encodeURIComponent(this.deal.headlines || '');
    const linkedinUrl = `https://www.linkedin.com/sharing/share-offsite/?url=${url}&title=${title}&summary=${summary}`;
    window.open(linkedinUrl, '_blank', 'width=600,height=400');
    this.showShareDropdown = false;
  }

  getDiscountPercentage(): number {
    if (!this.deal?.price || !this.deal?.discountedPrice) return 0;
    return Math.round(((this.deal.price - this.deal.discountedPrice) / this.deal.price) * 100);
  }

  getIncludedFeatures(): { icon: string, label: string }[] {
    const features = [];
    if (this.deal?.elderlyFriendly) features.push({ icon: '👵', label: 'Elderly Friendly' });
    if (this.deal?.internetIncluded) features.push({ icon: '🌐', label: 'Internet Included' });
    if (this.deal?.travelIncluded) features.push({ icon: '🚗', label: 'Travel Included' });
    if (this.deal?.mealsIncluded) features.push({ icon: '🍽️', label: 'Meals Included' });
    if (this.deal?.sightseeingIncluded) features.push({ icon: '🏛️', label: 'Sightseeing Included' });
    if (this.deal?.stayIncluded) features.push({ icon: '🏨', label: 'Stay Included' });
    if (this.deal?.airTransfer) features.push({ icon: '✈️', label: 'Air Transfer' });
    if (this.deal?.roadTransfer) features.push({ icon: '🚌', label: 'Road Transfer' });
    if (this.deal?.trainTransfer) features.push({ icon: '🚂', label: 'Train Transfer' });
    if (this.deal?.travelCostIncluded) features.push({ icon: '💰', label: 'Travel Cost Included' });
    if (this.deal?.guideIncluded) features.push({ icon: '👨‍🏫', label: 'Guide Included' });
    if (this.deal?.photographyIncluded) features.push({ icon: '📸', label: 'Photography Included' });
    if (this.deal?.insuranceIncluded) features.push({ icon: '🛡️', label: 'Insurance Included' });
    if (this.deal?.visaIncluded) features.push({ icon: '📝', label: 'Visa Included' });
    return features;
  }

  getPackageOptions(): { name: string, price: number, inclusions: string[] }[] {
    return this.deal?.packageOptions || [];
  }

  getItinerary(): { dayNumber: number, title: string, description: string, activities: string[] }[] {
    return this.deal?.itinerary || [];
  }

  getPolicies(): { title: string, description: string }[] {
    return this.deal?.policies || [];
  }

  setActiveTab(tab: 'overview' | 'inclusions' | 'itinerary' | 'reviews'): void {
    this.activeTab = tab;
  }

  checkAvailability(): void {
    // Implement availability check logic
    console.log('Checking availability for:', {
      date: this.selectedDate,
      travelers: this.selectedTravelers
    });
  }

  goBack(): void {
    this.location.back();
  }

  bookNow(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.showEnquiryPopup = true;
  }

  toggleFeatures() {
    this.showFeatures = !this.showFeatures;
  }

  toggleTransfers() {
    this.showTransfers = !this.showTransfers;
  }

  submitEnquiry() {
    if (!this.deal) return;

    const bookingData: CreateBookingDTO = {
      agencyId: this.deal.userId || '',
      dealId: this.deal.id,
      numberOfPeople: this.enquiryForm.travelerCount,
      email: this.enquiryForm.email,
      phoneNumber: this.enquiryForm.phoneNumberObject ? (this.enquiryForm.phoneNumberObject as any).internationalNumber || '' : '',
      bookingMessage: this.enquiryForm.message,
      travelDate: this.enquiryForm.travelDate ? new Date(this.enquiryForm.travelDate) : undefined,
      specialRequirements: this.enquiryForm.specialRequirements
    };

    this.bookingService.createBooking(bookingData).subscribe({
      next: (response) => {
        this.showEnquiryPopup = false;
        this.successMessage = 'Booking request sent successfully!';
        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      },
      error: (error) => {
        console.error('Error creating booking:', error);
        if (error.error?.message?.includes('already made an inquiry')) {
          this.hasExistingInquiry = true;
          this.error = error.error.message;
          this.showEnquiryPopup = false;
        } else {
          this.error = 'Failed to send booking request. Please try again.';
        }
        setTimeout(() => {
          this.error = '';
          this.hasExistingInquiry = false;
        }, 5000);
      }
    });
  }

  getConvertedPrice(price: number): number {
    return this.currencyService.convertPrice(price, 'USD', this.currentCurrency.code);
  }
}
