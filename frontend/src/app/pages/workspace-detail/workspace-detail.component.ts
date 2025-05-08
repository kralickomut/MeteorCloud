import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { WorkspaceService } from '../../services/workspace.service';
import { Workspace } from '../../models/WorkspaceFile';
import {UserService} from "../../services/user.service";

@Component({
  selector: 'app-workspace-detail',
  templateUrl: './workspace-detail.component.html',
  styleUrls: ['./workspace-detail.component.scss']
})
export class WorkspaceDetailComponent implements OnInit {
  workspaceId!: number;
  workspace?: Workspace;

  userId!: number;
  userRoleInWorkspace?: number;

  constructor(
    private route: ActivatedRoute,
    private workspaceService: WorkspaceService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.userService.user$.subscribe((user) => {
      if (user) {
        this.userId = user.id;
        this.route.paramMap.subscribe(params => {
          const id = Number(params.get('id'));
          if (!isNaN(id)) {
            this.workspaceId = id;
            this.fetchWorkspace();
          }
        });
      }
    });
  }

  fetchWorkspace(): void {
    this.workspaceService.getWorkspaceById(this.workspaceId).subscribe({
      next: (res) => {
        if (res.success) {
          this.workspace = res.data;

          const userEntry = this.workspace?.users?.find(u => u.userId === this.userId);
          this.userRoleInWorkspace = userEntry?.role;
        }
      }
    });
  }

  canInvite(): boolean {
    return this.userRoleInWorkspace === 1 || this.userRoleInWorkspace === 2;
  }
}
