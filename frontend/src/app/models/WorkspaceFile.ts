export interface WorkspaceFile {
    name: string;
    addedBy: string;
    date: string; // used for the table display
    createdAt: Date; // actual timestamp
    editedAt?: Date;
    type: string;
    size: string;
    resolution?: string;
    colorSpace?: string;
    pages?: number;
    status: 'Active' | 'Archived' | 'Draft';
}
export interface Workspace {
    id: number;
    ownerId: number;
    name: string;
    ownerName: string;
    status: string; // e.g., "Active", "Archived", etc.
    sizeInGB: number; // total size in gigabytes
    totalFiles: number;
    description?: string;
    createdOn: string; // ISO string
    lastUploadOn?: string; // ISO string or null
    users?: WorkspaceUser[]; // Optional, if included
}

export interface WorkspaceUser {
    id: number;
    workspaceId: number;
    userId: number;
    role: Role; // Enum
}

export enum Role {
    Owner = 1,
    Manager = 2,
    Guest = 3
}


export interface InviteToWorkspace {
  email: string;
  workspaceId: number;
  invitedByUserId: number
}
