import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  animations: [
    trigger('fadeUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('700ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'translateY(-20px)' }))
      ])
    ]),
    trigger('fadeInCards', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('700ms ease-out', style({ opacity: 1 }))
      ])
    ]),
    trigger('fadeInDelay', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('{{delay}}ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ], { params: { delay: 700 } })
    ])
  ]
})
export class HomeComponent {

  constructor(private router: Router) {}

  navigateToWorkspace(id: string) {
    this.router.navigate(['/workspaces', id]);
  }


  placeholderText = '';
  searchSuggestions = ['marketing brief', 'design system', 'roadmap 2025', 'team folders'];
  currentPhraseIndex = 0;
  charIndex = 0;
  isDeleting = false;
  typingSpeed = 100;
  pauseBetween = 1500;

  showRecents = false;

  recentWorkspaces = [
    {
      id: '1',
      name: 'Workspace',
      size: '13MB',
      fileCount: 87,
      owner: 'František Hromek',
      date: '21/03/2025',
      time: '16:09'
    },
    {
      id: '2',
      name: 'Workspace',
      size: '13MB',
      fileCount: 87,
      owner: 'František Hromek',
      date: '21/03/2025',
      time: '16:09'
    },
    {
      id: '3',
      name: 'Workspace',
      size: '13MB',
      fileCount: 87,
      owner: 'František Hromek',
      date: '21/03/2025',
      time: '16:09'
    }
  ];

  ngOnInit() {
    this.startTyping();
  }

  toggleRecents() {
    this.showRecents = !this.showRecents;
  }

  startTyping() {
    const currentPhrase = this.searchSuggestions[this.currentPhraseIndex];

    if (this.isDeleting) {
      this.placeholderText = currentPhrase.substring(0, this.charIndex--) + '|';
    } else {
      this.placeholderText = currentPhrase.substring(0, this.charIndex++) + '|';
    }

    if (!this.isDeleting && this.charIndex === currentPhrase.length + 1) {
      this.isDeleting = true;
      setTimeout(() => this.startTyping(), this.pauseBetween);
    } else if (this.isDeleting && this.charIndex === 0) {
      this.isDeleting = false;
      this.currentPhraseIndex = (this.currentPhraseIndex + 1) % this.searchSuggestions.length;
      setTimeout(() => this.startTyping(), 200);
    } else {
      setTimeout(() => this.startTyping(), this.isDeleting ? 50 : this.typingSpeed);
    }
  }
}
