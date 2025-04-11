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
