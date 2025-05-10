namespace MeteorCloud.Communication;

public static class MicroserviceEndpoints
{
    private static string GetUrl(string service, string fallbackPort) =>
        Environment.GetEnvironmentVariable($"{service.ToUpper()}_URL") ?? $"http://{service}:{fallbackPort}";

    public static class UserService
    {
        private static string BaseUrl => GetUrl("user_service", "5295");

        public static string GetUserById(int id) => $"{BaseUrl}/api/users/{id}";
        public static string GetUserByEmail(string email) => $"{BaseUrl}/api/users/email/{email}";
        public static string GetUsersBulk() => $"{BaseUrl}/api/users/bulk";
    }

    public static class WorkspaceService
    {
        private static string BaseUrl => GetUrl("workspace_service", "5297");

        public static string GetWorkspaceById(int id) => $"{BaseUrl}/api/workspaces/{id}";
        public static string IsUserInWorkspace(int userId, int workspaceId) =>
            $"{BaseUrl}/api/workspaces/is-user-in-workspace/{userId}/{workspaceId}";
    }

    public static class AuditService
    {
        private static string BaseUrl => GetUrl("audit_service", "5300");

        public static string GetFileHistoryByWorkspaceId(int workspaceId) =>
            $"{BaseUrl}/api/audit/file-history/{workspaceId}";

        public static string GetRecentWorkspaceIds(int userId) =>
            $"{BaseUrl}/api/audit/recent-workspaces/{userId}";
    }

    public static string AuthService => GetUrl("auth_service", "5296");
    public static string FileService => GetUrl("file_service", "5298");
}