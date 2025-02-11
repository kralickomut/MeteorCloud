using UserService.Domain.Models;

namespace UserService.Application.Abstraction;

public interface IUserRepository
{
    Task<User?> GetUserById(int id);
    Task<User?> GetUserByEmail(string email);
    Task<User?> GetUserByUsername(string username);
    Task<IEnumerable<User>> GetAllUsers();
    Task<User> CreateUser(User user);
    Task<User> UpdateUser(User user);
    Task<User> DeleteUser(User user);
}