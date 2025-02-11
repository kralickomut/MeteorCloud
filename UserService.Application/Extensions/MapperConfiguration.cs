using AutoMapper;
using UserService.Application.TransferObjects;
using UserService.Domain.Models;

namespace UserService.Application.Extensions;

public class MapperConfiguration : Profile
{
    public MapperConfiguration()
    {
        CreateMap<User, UserModel>();
        CreateMap<UserModel, User>();
        
        CreateMap<User, UserRegistrationModel>();
        CreateMap<UserRegistrationModel, User>();
    }

}