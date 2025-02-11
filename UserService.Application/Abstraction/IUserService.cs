using UserService.Domain.Models;

namespace UserService.Application.Abstraction;

public interface IUserService
{
    Task<User> RegisterUser(User user);
    Task<List<User>> GetAllUsers();
    Task<User?> GetUserById(int id);
    Task<User?> GetUserByEmail(string email);
    
    Task<User?> GetUserByUsername(string username);
    
}