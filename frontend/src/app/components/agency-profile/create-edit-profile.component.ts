import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AgencyProfileService } from '../../services/agency-profile.service';
import { FileUploadService } from '../../services/file-upload.service';
import { firstValueFrom } from 'rxjs';

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
  date: string | Date;
}

interface Award {
  title: string;
  date: string | Date;
}

interface Testimonial {
  name: string;
  title: string;
  message: string;
}

@Component({
  selector: 'app-create-edit-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-edit-profile.component.html',
  styleUrls: ['./create-edit-profile.component.css']
})
export class CreateEditProfileComponent implements OnInit {
  isLoading = false;
  isEdit = false;
  errorMessage = '';
  successMessage = '';
  currentStep = 1;
  totalSteps = 4;
  Math = Math;

  profile = {
    website: '',
    email: '',
    logoUrl: '',
    coverImageUrl: '',
    officeHours: '',
    languages: [] as string[],
    specializations: '',
    socialMediaLinks: [] as SocialMediaLink[],
    teamMembers: [] as TeamMember[],
    certifications: [] as Certification[],
    awards: [] as Award[],
    testimonials: [] as Testimonial[],
    termsAndConditions: '',
    privacyPolicy: ''
  };

  // Form helpers
  newLanguage = '';
  newSocialMediaLink: SocialMediaLink = { platform: '', url: '' };
  newTeamMember: TeamMember = { name: '', role: '' };
  newCertification: Certification = { name: '', provider: '', date: new Date() };
  newAward: Award = { title: '', date: new Date() };
  newTestimonial: Testimonial = { name: '', title: '', message: '' };

  // Image preview
  logoPreview: string | null = null;
  coverPreview: string | null = null;

  availablePlatforms = ['Instagram', 'Facebook', 'LinkedIn', 'Twitter', 'YouTube'];
  commonLanguages = ['English', 'Spanish', 'French', 'German', 'Chinese', 'Japanese', 'Arabic'];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private agencyProfileService: AgencyProfileService,
    private fileUploadService: FileUploadService
  ) {}

  async ngOnInit() {
    this.route.params.subscribe(async params => {
      if (params['id']) {
        this.isEdit = true;
        await this.loadProfile(params['id']);
      }
    });
  }

  private async loadProfile(id: number) {
    try {
      this.isLoading = true;
      const profile = await firstValueFrom(this.agencyProfileService.getAgencyProfile(id));

      // Parse JSON strings into arrays
      this.profile = {
        ...profile,
        languages: this.parseJsonField(profile.languages, []),
        socialMediaLinks: this.parseJsonField(profile.socialMediaLinks, []),
        teamMembers: this.parseJsonField(profile.teamMembers, []),
        certifications: this.parseJsonField(profile.certifications, []),
        awards: this.parseJsonField(profile.awards, []),
        testimonials: this.parseJsonField(profile.testimonials, [])
      };

      // Ensure all arrays are properly initialized
      if (!Array.isArray(this.profile.languages)) {
        this.profile.languages = [];
      }
      if (!Array.isArray(this.profile.socialMediaLinks)) {
        this.profile.socialMediaLinks = [];
      }
      if (!Array.isArray(this.profile.teamMembers)) {
        this.profile.teamMembers = [];
      }
      if (!Array.isArray(this.profile.certifications)) {
        this.profile.certifications = [];
      }
      if (!Array.isArray(this.profile.awards)) {
        this.profile.awards = [];
      }
      if (!Array.isArray(this.profile.testimonials)) {
        this.profile.testimonials = [];
      }

      this.logoPreview = profile.logoUrl;
      this.coverPreview = profile.coverImageUrl;
    } catch (error) {
      this.errorMessage = 'Failed to load profile';
      console.error('Error loading profile:', error);
    } finally {
      this.isLoading = false;
    }
  }

  // Helper method to parse JSON strings
  private parseJsonField(field: any, defaultValue: any): any {
    if (!field) return defaultValue;

    try {
      // Check if it's already an array
      if (Array.isArray(field)) return field;

      // Try to parse as JSON
      return JSON.parse(field);
    } catch (e) {
      console.error('Error parsing JSON field:', e);
      return defaultValue;
    }
  }

  // File upload handlers
  async onLogoUpload(event: any) {
    const file = event.target.files[0];
    if (file) {
      try {
        const reader = new FileReader();
        reader.onload = (e: any) => {
          this.logoPreview = e.target.result;
        };
        reader.readAsDataURL(file);

        const response = await firstValueFrom(
          this.fileUploadService.uploadFile(file, 'agency-logos')
        );
        this.profile.logoUrl = response.url;
        this.successMessage = 'Logo uploaded successfully';
      } catch (error) {
        this.errorMessage = 'Failed to upload logo';
        console.error('Error uploading logo:', error);
      }
    }
  }

  async onCoverUpload(event: any) {
    const file = event.target.files[0];
    if (file) {
      try {
        const reader = new FileReader();
        reader.onload = (e: any) => {
          this.coverPreview = e.target.result;
        };
        reader.readAsDataURL(file);

        const response = await firstValueFrom(
          this.fileUploadService.uploadFile(file, 'agency-covers')
        );
        this.profile.coverImageUrl = response.url;
        this.successMessage = 'Cover image uploaded successfully';
      } catch (error) {
        this.errorMessage = 'Failed to upload cover image';
        console.error('Error uploading cover image:', error);
      }
    }
  }

  // Step navigation
  nextStep() {
    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  // Form helpers
  addLanguage() {
    if (this.newLanguage && !this.profile.languages.includes(this.newLanguage)) {
      this.profile.languages.push(this.newLanguage);
      this.newLanguage = '';
    }
  }

  removeLanguage(language: string) {
    this.profile.languages = this.profile.languages.filter(l => l !== language);
  }

  addSocialMediaLink() {
    if (this.newSocialMediaLink.platform && this.newSocialMediaLink.url) {
      this.profile.socialMediaLinks.push({ ...this.newSocialMediaLink });
      this.newSocialMediaLink = { platform: '', url: '' };
    }
  }

  removeSocialMediaLink(index: number) {
    this.profile.socialMediaLinks.splice(index, 1);
  }

  addTeamMember() {
    if (this.newTeamMember.name && this.newTeamMember.role) {
      this.profile.teamMembers.push({ ...this.newTeamMember });
      this.newTeamMember = { name: '', role: '' };
    }
  }

  removeTeamMember(index: number) {
    this.profile.teamMembers.splice(index, 1);
  }

  addCertification() {
    if (this.newCertification.name && this.newCertification.provider) {
      this.profile.certifications.push({ ...this.newCertification });
      this.newCertification = { name: '', provider: '', date: new Date() };
    }
  }

  removeCertification(index: number) {
    this.profile.certifications.splice(index, 1);
  }

  addAward() {
    if (this.newAward.title) {
      this.profile.awards.push({ ...this.newAward });
      this.newAward = { title: '', date: new Date() };
    }
  }

  removeAward(index: number) {
    this.profile.awards.splice(index, 1);
  }

  addTestimonial() {
    if (this.newTestimonial.name && this.newTestimonial.message) {
      this.profile.testimonials.push({ ...this.newTestimonial });
      this.newTestimonial = { name: '', title: '', message: '' };
    }
  }

  removeTestimonial(index: number) {
    this.profile.testimonials.splice(index, 1);
  }

  // Form submission
  async onSubmit() {
    try {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      // Validate required fields
      if (!this.profile.website || !this.profile.email) {
        this.errorMessage = 'Website and email are required fields';
        this.isLoading = false;
        return;
      }

      // Create a deep copy of the profile to avoid modifying the original
      const profileToSubmit = JSON.parse(JSON.stringify(this.profile));

      // Standardize date formats for certifications and awards
      if (profileToSubmit.certifications && profileToSubmit.certifications.length > 0) {
        profileToSubmit.certifications = profileToSubmit.certifications.map((cert: any) => ({
          ...cert,
          date: new Date(cert.date).toISOString()
        }));
      }

      if (profileToSubmit.awards && profileToSubmit.awards.length > 0) {
        profileToSubmit.awards = profileToSubmit.awards.map((award: any) => ({
          ...award,
          date: new Date(award.date).toISOString()
        }));
      }

      // Ensure all arrays are properly initialized
      if (!Array.isArray(profileToSubmit.languages)) {
        profileToSubmit.languages = [];
      }
      if (!Array.isArray(profileToSubmit.socialMediaLinks)) {
        profileToSubmit.socialMediaLinks = [];
      }
      if (!Array.isArray(profileToSubmit.teamMembers)) {
        profileToSubmit.teamMembers = [];
      }
      if (!Array.isArray(profileToSubmit.certifications)) {
        profileToSubmit.certifications = [];
      }
      if (!Array.isArray(profileToSubmit.awards)) {
        profileToSubmit.awards = [];
      }
      if (!Array.isArray(profileToSubmit.testimonials)) {
        profileToSubmit.testimonials = [];
      }

      if (this.isEdit) {
        await firstValueFrom(this.agencyProfileService.updateAgencyProfile(
          parseInt(this.route.snapshot.params['id']),
          profileToSubmit
        ));
        this.successMessage = 'Profile updated successfully';
      } else {
        await firstValueFrom(this.agencyProfileService.createAgencyProfile(profileToSubmit));
        this.successMessage = 'Profile created successfully';
      }

      setTimeout(() => {
        this.router.navigate(['/agency-dashboard']);
      }, 1500);
    } catch (error: any) {
      console.error('Error saving profile:', error);
      if (error.error?.errors) {
        this.errorMessage = Object.values(error.error.errors).flat().join(' ');
      } else if (error.error?.message) {
        this.errorMessage = error.error.message;
      } else {
        this.errorMessage = 'Failed to save profile. Please try again.';
      }
    } finally {
      this.isLoading = false;
    }
  }
  goBack(): void {
    this.router.navigate(['/agency-dashboard']);
  }
}
