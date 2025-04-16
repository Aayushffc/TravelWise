import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './services/auth.interceptor';
import { provideClientHydration } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { SocialAuthServiceConfig, GoogleLoginProvider } from '@abacritt/angularx-social-login';


export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor])
    ),
    provideClientHydration(),
    provideAnimations(),
    {
      provide: 'SocialAuthServiceConfig',
      useValue: {
        autoLogin: false,
        providers: [
          {
            id: GoogleLoginProvider.PROVIDER_ID,
            provider: new GoogleLoginProvider('334522416360-h3ipni5mvi5g9pnds92ts48j32k7stpr.apps.googleusercontent.com'),
          },
        ],
      } as SocialAuthServiceConfig,
    },
  ]
};
