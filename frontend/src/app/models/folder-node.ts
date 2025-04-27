export interface FolderNode {
  name: string;
  folders: FolderNode[];
  files: {
    id: string;
    name: string;
    uploadedAt?: string;
    uploadedByName?: string;
    contentType?: string;
    size?: number; // only files have size
  }[];
  uploadedAt?: string;
  uploadedByName?: string;
}
