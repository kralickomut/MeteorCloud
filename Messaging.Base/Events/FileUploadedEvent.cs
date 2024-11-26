namespace Messaging.Base.Events;

public class FileUploadedEvent
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime EventDate { get; private set; }

    public FileUploadedEvent(string fileName, long fileSize)
    {
        FileName = fileName;
        FileSize = fileSize;
        EventDate = DateTime.UtcNow;
    }
}