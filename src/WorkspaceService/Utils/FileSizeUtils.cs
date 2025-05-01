namespace WorkspaceService.Utils;

public static class FileSizeUtils
{
    private const double BytesInGB = 1_000_000_000.0;

    public static double BytesToGB(long bytes)
    {
        return bytes / BytesInGB;
    }

    public static double SubtractSafe(double currentSizeInGb, long bytesToSubtract)
    {
        var sizeToSubtract = BytesToGB(bytesToSubtract);
        return Math.Max(0, currentSizeInGb - sizeToSubtract);
    }

    public static double AddSafe(double currentSizeInGb, long bytesToAdd)
    {
        return currentSizeInGb + BytesToGB(bytesToAdd);
    }
}