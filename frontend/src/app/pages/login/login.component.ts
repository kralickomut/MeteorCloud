import { Component } from '@angular/core';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  model = {
    name: '',
    password: ''
  };

  ngOnInit() {
    this.setWelcomeText()
  }

  welcomeText: string = '';

  welcomeTexts = [
    'Ready to dive back in and get things done?',
    'Pick up right where you left off — your workspace is waiting!',
    'Good to see you again!',
    'Your files, your team, your flow — all in one place.'
  ];

  setWelcomeText() {
    let index: number = Math.floor(Math.random() * this.welcomeTexts.length);
    this.welcomeText = this.welcomeTexts[index];
  }

  onLogin() {
    console.log('Logging in:', this.model);
  }
}
