namespace MetadataService.Models.Tree;

public class FolderNode
{
            public string Name { get; set; }
            public List<FolderNode> Folders { get; set; } = new();
            public List<FileNode> Files { get; set; } = new();

            public DateTime? UploadedAt { get; set; } // new
            public string? UploadedByName { get; set; } // new
    

    public FolderNode(string name)
    {
        Name = name;
    }
}