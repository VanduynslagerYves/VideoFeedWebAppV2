
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideAuth0, AuthHttpInterceptor } from '@auth0/auth0-angular';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    provideAuth0({
      domain: 'dev-i4c6oxzfwdlecakx.eu.auth0.com',
      clientId: 'WQFbWudM8d0yoSWlUxeQSvp6qyg5sOhn',
      // no client secret is needed here, that's only for backend applications
      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: 'https://localhost:7244',
        scope: 'openid profile email'
      },
    }),
  ]
};
