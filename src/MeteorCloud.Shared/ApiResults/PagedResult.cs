namespace MeteorCloud.Shared.ApiResults;

public class PagedResult<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}