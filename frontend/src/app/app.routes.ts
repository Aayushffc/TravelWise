import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { HomeComponent } from './components/home/home.component';
import { AuthCallbackComponent } from './components/auth/auth-callback.component';
import { VerifyEmailComponent } from './components/verify-email/verify-email.component';
import { LocationDetailsComponent } from './components/location-details/location-details.component';
import { FAQComponent } from './components/faq/faq.component';
import { ProfileComponent } from './components/profile/profile.component';
import { AuthGuard } from './guards/auth.guard';
import { AdminGuard } from './guards/admin.guard';
import { AgencyGuard } from './guards/agency.guard';
import { AdminDashboardComponent } from './components/admin/admin-dashboard.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { AgencyApplicationComponent } from './components/agency-application/agency-application.component';
import { AdminAgencyApplicationsComponent } from './components/admin-agency-applications/admin-agency-applications.component';
import { ManageLocationsComponent } from './components/admin/manage-locations/manage-locations.component';
import { ManageDealsComponent } from './components/manage-deals/manage-deals.component';
import { AgencyDealDetailsComponent } from './components/agency-deal-details/agency-deal-details.component';
import { DealDetailsComponent } from './components/deal-details/deal-details.component';
import { SearchComponent } from './components/search/search.component';
import { AgencyDashboardComponent } from './components/agency-dashboard/agency-dashboard.component';
import { BookingComponent } from './components/booking/booking.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'forgot-password',
    component: ForgotPasswordComponent
  },
  {
    path: 'reset-password',
    component: ResetPasswordComponent
  },
  {
    path: 'admin',
    component: AdminDashboardComponent,
    canActivate: [AuthGuard, AdminGuard]
  },
  {
    path: 'admin/agency-applications',
    component: AdminAgencyApplicationsComponent,
    canActivate: [AuthGuard, AdminGuard]
  },
  {
    path: 'admin/locations',
    component: ManageLocationsComponent,
    canActivate: [AuthGuard, AdminGuard]
  },
  {
    path: 'agency/manage-deals',
    component: ManageDealsComponent,
    canActivate: [AuthGuard, AgencyGuard]
  },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'home/search',
    component: SearchComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'home/profile',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'agency-application',
    component: AgencyApplicationComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'bookings',
    component: BookingComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'quotations',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'wallet',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'passengers',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'auth/callback',
    component: AuthCallbackComponent
  },
  {
    path: 'verify-email',
    component: VerifyEmailComponent
  },
  {
    path: 'location/:id',
    component: LocationDetailsComponent
  },
  {
    path: 'deal/:id',
    component: DealDetailsComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'faq',
    component: FAQComponent,
    title: 'FAQ - TravelWise'
  },
  {
    path: 'agency/agency-deal-details/:id',
    component: AgencyDealDetailsComponent,
    canActivate: [AuthGuard, AgencyGuard]
  },
  {
    path: 'agency-dashboard',
    component: AgencyDashboardComponent,
    canActivate: [AuthGuard, AgencyGuard]
  },
  {
    path: '**',
    redirectTo: 'login',
    pathMatch: 'full'
  }
];
