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
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';

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
    path: 'home',
    component: HomeComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'bookings',
    component: ProfileComponent,
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
    path: 'faq',
    component: FAQComponent,
    title: 'FAQ - TravelWise'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
