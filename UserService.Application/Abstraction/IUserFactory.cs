using UserService.Application.TransferObjects;
using UserService.Domain.Models;

namespace UserService.Application.Abstraction;

public interface IUserFactory
{
    Task<UserModel> CreateUser(UserRegistrationModel user);
}