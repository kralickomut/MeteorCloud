namespace MeteorCloud.Shared.ApiResults;

public class ApiResult<T>
{
    public T? Data { get; }
    public bool Success { get; }
    
    public string? Message { get; }

    public ApiResult(T? data, bool success = true, string? message = null)
    {
        Data = data;
        Success = success;
        Message = message;
    }
    
 
}