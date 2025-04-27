namespace FileService.Models;

public class DownloadResult
{
    public Stream Stream { get; set; }
    public string ContentType { get; set; }
    public string FileName { get; set; }
}