import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AgencyProfileService } from '../../services/agency-profile.service';
import { BookingService } from '../../services/booking.service';
import { ChatService } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { FileUploadService } from '../../services/file-upload.service';
import { Observable, firstValueFrom } from 'rxjs';
import { Location } from '@angular/common';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { trigger, state, style, animate, transition } from '@angular/animations';

interface SocialMediaLink {
  platform: string;
  url: string;
}

interface TeamMember {
  name: string;
  role: string;
}

interface Certification {
  name: string;
  provider: string;
  date: Date;
}

interface Award {
  title: string;
  date: Date;
}

interface Testimonial {
  name: string;
  title: string;
  message: string;
}

type TabType = 'profile' | 'bookings' | 'payments';

@Component({
  selector: 'app-agency-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  schemas: [NO_ERRORS_SCHEMA],
  templateUrl: './agency-dashboard.component.html',
  styleUrls: ['./agency-dashboard.component.css'],
  animations: [
    trigger('fadeSlide', [
      state('void', style({ opacity: 0, transform: 'translateY(20px)' })),
      state('*', style({ opacity: 1, transform: 'translateY(0)' })),
      transition('void <=> *', animate('300ms ease-in-out')),
    ]),
  ],
})
export class AgencyDashboardComponent implements OnInit {
  activeTab: TabType = 'profile';
  tabs: TabType[] = ['profile', 'bookings', 'payments'];
  isLoading = true;
  profile: any = null;
  bookings: any[] = [];
  filteredBookings: any[] = [];
  bookingFilter: 'all' | 'confirmed' | 'pending' | 'cancelled' = 'all';
  unreadMessages: number = 0;
  user: any = null;
  selectedBooking: any = null;
  chatMessages: any[] = [];
  newMessage: string = '';
  showProfileForm = false;

  // Payment related properties
  payments: any[] = [];
  filteredPayments: any[] = [];
  selectedStatus: string = 'all';
  isLoadingPayments: boolean = false;
  totalEarnings: number = 0;
  completedPayments: number = 0;
  pendingPayments: number = 0;
  failedPayments: number = 0;

  profileForm = {
    website: '',
    email: '',
    logoUrl: '',
    coverImageUrl: '',
    officeHours: '',
    languages: '',
    specializations: '',
    socialMediaLinks: [] as SocialMediaLink[],
    teamMembers: [] as TeamMember[],
    certifications: [] as Certification[],
    awards: [] as Award[],
    testimonials: [] as Testimonial[],
    termsAndConditions: '',
    privacyPolicy: ''
  };

  // Local image storage
  localLogoUrl: string | null = null;
  localCoverUrl: string | null = null;
  newSocialMediaLink: SocialMediaLink = { platform: '', url: '' };
  newTeamMember: TeamMember = { name: '', role: '' };
  newCertification: Certification = { name: '', provider: '', date: new Date() };
  newAward: Award = { title: '', date: new Date() };
  newTestimonial: Testimonial = { name: '', title: '', message: '' };
  availablePlatforms = ['Instagram', 'Facebook', 'LinkedIn', 'Twitter', 'YouTube'];

  // Add computed properties for booking counts
  get confirmedBookingsCount(): number {
    if (!this.bookings || !Array.isArray(this.bookings)) return 0;
    return this.bookings.filter(b => b.status === 'Accepted').length;
  }

  get pendingBookingsCount(): number {
    if (!this.bookings || !Array.isArray(this.bookings)) return 0;
    return this.bookings.filter(b => b.status === 'Pending').length;
  }

  get cancelledBookingsCount(): number {
    if (!this.bookings || !Array.isArray(this.bookings)) return 0;
    return this.bookings.filter(b => b.status === 'Rejected' || b.status === 'Cancelled').length;
  }

  canShowChat(booking: any): boolean {
    return booking.status !== 'Rejected' && booking.status !== 'Cancelled';
  }

  readonly availableTabs: TabType[] = ['profile', 'bookings', 'payments'];

  constructor(
    private agencyProfileService: AgencyProfileService,
    private bookingService: BookingService,
    private chatService: ChatService,
    private authService: AuthService,
    private fileUploadService: FileUploadService,
    private router: Router,
    private location: Location
  ) {}

  async ngOnInit() {
    try {
      this.user = await this.authService.getCurrentUser();
      await this.loadProfile();
      await this.loadBookings();
      await this.loadPayments();
      this.setupChatNotifications();
    } catch (error) {
      console.error('Error initializing dashboard:', error);
    } finally {
      this.isLoading = false;
    }
  }

  goBack() {
    this.location.back();
  }

  async toggleOnlineStatus() {
    try {
      const newStatus = !this.profile?.isOnline;
      await firstValueFrom(this.agencyProfileService.updateOnlineStatus(newStatus));
      this.profile.isOnline = newStatus;
    } catch (error) {
      console.error('Error toggling online status:', error);
    }
  }

  private async loadProfile() {
    try {
      this.profile = await firstValueFrom(this.agencyProfileService.getMyAgencyProfile());
      if (this.profile) {
        this.profileForm = { ...this.profile };
      }
    } catch (error: any) {
      console.error('Error loading profile:', error);
      // Check if the error is a 404 (profile not found)
      if (error.status === 404) {
        this.profile = null;
        this.showProfileForm = true;
      }
    }
  }

  private async loadBookings() {
    try {
      const response = await firstValueFrom(this.bookingService.getAgencyBookings());

      if (response && Array.isArray(response)) {
        this.bookings = response;
      } else if (response && response.data && Array.isArray(response.data)) {
        this.bookings = response.data;
      } else {
        console.warn('Unexpected bookings response format:', response);
        this.bookings = [];
      }

      // Apply initial filter
      this.applyBookingFilter();

    } catch (error) {
      console.error('Error loading bookings:', error);
      this.bookings = [];
    }
  }

  private async loadPayments() {
    try {
      this.isLoadingPayments = true;
      // Replace with actual API call when available
      // const response = await firstValueFrom(this.paymentService.getAgencyPayments());
      // this.payments = response;

      // Temporary mock data
      this.payments = [];
      this.filteredPayments = [];
      this.totalEarnings = 0;
      this.completedPayments = 0;
      this.pendingPayments = 0;
      this.failedPayments = 0;

      this.applyPaymentFilter();
    } catch (error) {
      console.error('Error loading payments:', error);
      this.payments = [];
    } finally {
      this.isLoadingPayments = false;
    }
  }

  private setupChatNotifications() {
    this.chatService.getUnreadMessagesCount().subscribe(count => {
      this.unreadMessages = count;
    });
  }

  switchTab(tab: TabType): void {
    this.activeTab = tab;
  }

  async updateLastActive() {
    try {
      await firstValueFrom(this.agencyProfileService.updateLastActive());
    } catch (error) {
      console.error('Error updating last active:', error);
    }
  }

  async acceptBooking(id: number) {
    try {
      await firstValueFrom(this.bookingService.acceptBooking(id));
      await this.loadBookings();
    } catch (error) {
      console.error('Error accepting booking:', error);
    }
  }

  async rejectBooking(id: number) {
    try {
      await firstValueFrom(this.bookingService.rejectBooking(id, 'Rejected by agency'));
      await this.loadBookings();
    } catch (error) {
      console.error('Error rejecting booking:', error);
    }
  }

  async viewChat(id: number) {
    this.selectedBooking = this.bookings.find(b => b.id === id);
    this.chatService.selectedBooking = this.selectedBooking;
    this.activeTab = 'bookings';
    await this.loadChatMessages(id);
  }

  async selectChat(id: number) {
    this.selectedBooking = this.bookings.find(b => b.id === id);
    await this.loadChatMessages(id);
  }

  private async loadChatMessages(id: number) {
    try {
      this.chatMessages = await firstValueFrom(this.bookingService.getChatMessages(id));
      await this.chatService.joinChat(id);
    } catch (error) {
      console.error('Error loading chat messages:', error);
    }
  }

  async sendMessage() {
    console.log('Attempting to send message:', {
      message: this.newMessage,
      selectedBooking: this.selectedBooking
    });

    if (!this.newMessage.trim() || !this.selectedBooking) {
      console.error('No message or booking selected');
      return;
    }

    try {
      await this.chatService.sendMessage(this.selectedBooking.id, this.newMessage);
      console.log('Message sent successfully');
      this.newMessage = '';
      await this.loadChatMessages(this.selectedBooking.id);
    } catch (error) {
      console.error('Error sending message:', error);
    }
  }

  async submitProfile() {
    try {
      // Upload images if they have changed
      if (this.localLogoUrl && this.localLogoUrl !== this.profileForm.logoUrl) {
        const logoFile = await this.dataURLtoFile(this.localLogoUrl, 'logo.png');
        const uploadResponse = await firstValueFrom(
          this.fileUploadService.uploadFile(logoFile, 'agency-logos')
        );
        this.profileForm.logoUrl = uploadResponse.url;
      }

      if (this.localCoverUrl && this.localCoverUrl !== this.profileForm.coverImageUrl) {
        const coverFile = await this.dataURLtoFile(this.localCoverUrl, 'cover.png');
        const uploadResponse = await firstValueFrom(
          this.fileUploadService.uploadFile(coverFile, 'agency-covers')
        );
        this.profileForm.coverImageUrl = uploadResponse.url;
      }

      if (this.profile) {
        await firstValueFrom(this.agencyProfileService.updateAgencyProfile(this.profile.id, this.profileForm));
      } else {
        await firstValueFrom(this.agencyProfileService.createAgencyProfile(this.profileForm));
      }
      await this.loadProfile();
      this.showProfileForm = false;
    } catch (error) {
      console.error('Error submitting profile:', error);
    }
  }

  // Image preview methods
  onLogoFileChange(event: any) {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.localLogoUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  onCoverFileChange(event: any) {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.localCoverUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  // Helper method to convert data URL to File
  private dataURLtoFile(dataurl: string, filename: string): Promise<File> {
    return fetch(dataurl)
      .then(res => res.blob())
      .then(blob => new File([blob], filename, { type: 'image/png' }));
  }

  // Social media methods
  addSocialMediaLink() {
    if (this.newSocialMediaLink.platform && this.newSocialMediaLink.url) {
      this.profileForm.socialMediaLinks.push({ ...this.newSocialMediaLink });
      this.newSocialMediaLink = { platform: '', url: '' };
    }
  }

  removeSocialMediaLink(index: number) {
    this.profileForm.socialMediaLinks.splice(index, 1);
  }

  getSocialMediaIcon(platform: string): string {
    switch (platform.toLowerCase()) {
      case 'instagram':
        return 'M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2.163c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z';
      case 'facebook':
        return 'M18 2h-3a5 5 0 00-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 011-1h3z';
      case 'linkedin':
        return 'M16 8a6 6 0 016 6v7h-4v-7a2 2 0 00-2-2 2 2 0 00-2 2v7h-4v-7a6 6 0 016-6zM2 9h4v12H2z';
      case 'twitter':
        return 'M23 3a10.9 10.9 0 01-3.14 1.53 4.48 4.48 0 00-7.86 3v1A10.66 10.66 0 013 4s-4 9 5 13a11.64 11.64 0 01-7 2c9 5 20 0 20-11.5a4.5 4.5 0 00-.08-.83A7.72 7.72 0 0023 3z';
      case 'youtube':
        return 'M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z';
      default:
        return '';
    }
  }

  addTeamMember() {
    if (this.newTeamMember.name && this.newTeamMember.role) {
      this.profileForm.teamMembers.push({ ...this.newTeamMember });
      this.newTeamMember = { name: '', role: '' };
    }
  }

  removeTeamMember(index: number) {
    this.profileForm.teamMembers.splice(index, 1);
  }

  addCertification() {
    if (this.newCertification.name && this.newCertification.provider) {
      this.profileForm.certifications.push({ ...this.newCertification });
      this.newCertification = { name: '', provider: '', date: new Date() };
    }
  }

  removeCertification(index: number) {
    this.profileForm.certifications.splice(index, 1);
  }

  addAward() {
    if (this.newAward.title) {
      this.profileForm.awards.push({ ...this.newAward });
      this.newAward = { title: '', date: new Date() };
    }
  }

  removeAward(index: number) {
    this.profileForm.awards.splice(index, 1);
  }

  addTestimonial() {
    if (this.newTestimonial.name && this.newTestimonial.message) {
      this.profileForm.testimonials.push({ ...this.newTestimonial });
      this.newTestimonial = { name: '', title: '', message: '' };
    }
  }

  removeTestimonial(index: number) {
    this.profileForm.testimonials.splice(index, 1);
  }

  navigateToCreateProfile() {
    this.router.navigate(['/agency/create-profile']);
  }

  navigateToEditProfile() {
    if (this.profile && this.profile.id) {
      this.router.navigate(['/agency/edit-profile', this.profile.id]);
    }
  }

  // Add new methods for booking filtering
  setBookingFilter(filter: 'all' | 'confirmed' | 'pending' | 'cancelled'): void {
    this.bookingFilter = filter;
    this.applyBookingFilter();
  }

  applyBookingFilter(): void {
    if (!this.bookings || !Array.isArray(this.bookings)) {
      this.filteredBookings = [];
      return;
    }

    switch (this.bookingFilter) {
      case 'confirmed':
        this.filteredBookings = this.bookings.filter(b => b.status === 'Accepted');
        break;
      case 'pending':
        this.filteredBookings = this.bookings.filter(b => b.status === 'Pending');
        break;
      case 'cancelled':
        this.filteredBookings = this.bookings.filter(b => b.status === 'Rejected' || b.status === 'Cancelled');
        break;
      default:
        this.filteredBookings = [...this.bookings];
    }
  }

  getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'accepted':
        return 'bg-green-100 text-green-800';
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'cancelled':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusDotColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'accepted':
        return 'bg-green-500';
      case 'pending':
        return 'bg-yellow-500';
      case 'cancelled':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  }

  onStatusChange(status: string): void {
    this.selectedStatus = status;
    this.applyPaymentFilter();
  }

  applyPaymentFilter(): void {
    if (!this.payments || !Array.isArray(this.payments)) {
      this.filteredPayments = [];
      return;
    }

    if (this.selectedStatus === 'all') {
      this.filteredPayments = [...this.payments];
    } else {
      this.filteredPayments = this.payments.filter(p => p.status === this.selectedStatus);
    }
  }

  viewDetails(payment: any): void {
    console.log('View payment details:', payment);
    // Implement payment details view logic
  }

  processPayment(payment: any): void {
    console.log('Process payment:', payment);
    // Implement payment processing logic
  }

  refundPayment(payment: any): void {
    console.log('Refund payment:', payment);
    // Implement payment refund logic
  }
}
