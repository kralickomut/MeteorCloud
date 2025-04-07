import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {

  // Typing animation
  placeholderText = '';
  searchSuggestions = ['marketing brief', 'design system', 'roadmap 2025', 'team folders'];
  currentPhraseIndex = 0;
  charIndex = 0;
  isDeleting = false;
  typingSpeed = 100; // ms between letters
  pauseBetween = 1500; // wait after full word


  ngOnInit() {
    this.startTyping();
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
