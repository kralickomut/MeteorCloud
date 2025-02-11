namespace UserService.Application.TransferObjects;

public class UserModel
{
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; } = null;
}