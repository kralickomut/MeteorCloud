import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { WorkspaceService } from '../../services/workspace.service';
import { Workspace } from '../../models/WorkspaceFile';

@Component({
  selector: 'app-workspace-detail',
  templateUrl: './workspace-detail.component.html',
  styleUrls: ['./workspace-detail.component.scss']
})
export class WorkspaceDetailComponent implements OnInit {
  workspaceId!: number;
  workspace?: Workspace;

  constructor(
    private route: ActivatedRoute,
    private workspaceService: WorkspaceService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      if (!isNaN(id)) {
        this.workspaceId = id;
        this.fetchWorkspace();
      }
    });
  }

  fetchWorkspace(): void {
    this.workspaceService.getWorkspaceById(this.workspaceId).subscribe({
      next: (res) => {
        if (res.success) {
          this.workspace = res.data;
        }
      }
    });
  }
}
