import { Component, signal, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';
import { Cam } from './cam/cam';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AsyncPipe, Cam],
  templateUrl: './app.html',
  styleUrl: './app.less'
})

export class App implements OnInit{
  protected readonly title = signal('camerafeed-client');
  public readonly origin = window.location.origin;
  constructor(public auth: AuthService) {}

  ngOnInit() {
    this.auth.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        //this.auth.getAccessTokenSilently().subscribe(token => console.log('Access Token:', token));
      }
    });
  }
}
