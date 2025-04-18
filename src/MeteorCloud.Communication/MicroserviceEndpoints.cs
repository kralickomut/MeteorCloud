namespace MeteorCloud.Communication;

public static class MicroserviceEndpoints
{


    public class UserService
    {
        const string BaseUrl = "http://user_service:5295";
        
        public static string GetUserById(int id) => BaseUrl + $"/api/users/{id}";
        public static string GetUserByEmail(string email) => BaseUrl + $"/api/users/email/{email}";
    }
    
    public class WorkspaceService
    {
        const string BaseUrl = "http://workspace_service:5297";
        
        public static string GetWorkspaceById(int id) => BaseUrl + $"/api/workspace/{id}";
    }
    
    public const string AuthService = "http://auth_service:5296";
    public const string FileService = "http://file_service:5298";
}