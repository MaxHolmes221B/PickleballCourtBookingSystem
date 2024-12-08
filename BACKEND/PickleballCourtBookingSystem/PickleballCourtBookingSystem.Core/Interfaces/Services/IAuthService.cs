using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.DTOs;
using PickleballCourtBookingSystem.Core.Entities;

namespace PickleballCourtBookingSystem.Core.Interfaces.Services;

public interface IAuthService : IBaseService<User>
{
    public ServiceResult Register(string username, string password);
    public ServiceResult Login(string username, string password);
}