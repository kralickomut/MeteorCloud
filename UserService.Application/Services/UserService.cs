using UserService.Application.Abstraction;
using UserService.Application.TransferObjects;
using UserService.Domain.Models;
// ReSharper disable All

namespace UserService.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> RegisterUser(UserRegistrationModel request)
    {
        if (request.Password != request.PasswordAgain)
        {
            throw new Exception("Passwords do not match");
        }

        if (await _userRepository.GetUserByEmail(request.Email) != null)
        {
            throw new Exception("User with this email already exists");
        }

        if (await _userRepository.GetUserByUsername(request.Username) != null)
        {
            throw new Exception("User with this username already exists");
        }
        
        return result;
    }

    public async Task<List<User>> GetAllUsers()
    {
        var result = await _userRepository.GetAllUsers();
        
        return result.ToList();
    }

    public async Task<User?> GetUserById(int id)
    {
        return await _userRepository.GetUserById(id);
    }
    
    public async Task<User?> GetUserByEmail(string email)
    {
        var result = await _userRepository.GetUserByEmail(email);
        
        return result;
    }

    public async Task<User?> GetUserByUsername(string username)
    {
        var result = await _userRepository.GetUserByUsername(username);
        
        return result;
    }
}