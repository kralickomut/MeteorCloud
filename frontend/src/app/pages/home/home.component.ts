import {Component, OnInit} from '@angular/core';
import { Router } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';
import {Workspace} from "../../models/WorkspaceFile";
import {UserService} from "../../services/user.service";
import {WorkspaceService} from "../../services/workspace.service";

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
export class HomeComponent implements OnInit{
  constructor(
    private router: Router,
    private workspaceService: WorkspaceService,
    private userService: UserService
  ) {}

  allWorkspaces: Workspace[] = [];
  filteredWorkspaces: Workspace[] = [];
  selectedWorkspace: Workspace | null = null;

  showRecents = false;
  recentWorkspaces: Workspace[] | undefined = [];
  loadingRecents = false;

  ngOnInit() {
    this.startTyping();
    this.userService.user$.subscribe(user => {
      if (user) {
        this.fetchRecentWorkspaces(user.id);
      }
    });
  }

  fetchRecentWorkspaces(userId: number): void {
    this.loadingRecents = true;
    this.workspaceService.getRecentWorkspaces(userId).subscribe({
      next: (res) => {
        this.recentWorkspaces = res.data;
        this.loadingRecents = false;
      },
      error: (err) => {
        console.error('Failed to fetch recent workspaces', err);
        this.loadingRecents = false;
      }
    });
  }

  filterWorkspaces(event: any): void {
    const query = event.query?.trim();
    if (!query) {
      this.filteredWorkspaces = [];
      return;
    }

    this.userService.user$.pipe().subscribe(user => {
      if (!user) return;

      this.workspaceService.searchWorkspaces(user.id, query).subscribe({
        next: (res) => {
          this.filteredWorkspaces = res.data ?? [];
        },
        error: (err) => {
          console.error('❌ Failed to search workspaces', err);
          this.filteredWorkspaces = [];
        }
      });
    });
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }

  navigateToWorkspace(id: number) {
    this.router.navigate(['/workspaces', id]);
  }


  placeholderText = '';
  searchSuggestions = ['marketing brief', 'design system', 'roadmap 2025', 'team folders'];
  currentPhraseIndex = 0;
  charIndex = 0;
  isDeleting = false;
  typingSpeed = 100;
  pauseBetween = 1500;


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
