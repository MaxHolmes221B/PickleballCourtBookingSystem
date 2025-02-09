﻿using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.Common;
using PickleballCourtBookingSystem.Core.CustomValidation;
using PickleballCourtBookingSystem.Core.DTOs;
using PickleballCourtBookingSystem.Core.DTOs.UserDTOs;
using PickleballCourtBookingSystem.Core.Entities;
using PickleballCourtBookingSystem.Core.Interfaces.Infrastructure;
using PickleballCourtBookingSystem.Core.Interfaces.Services;
using System.Data;

namespace PickleballCourtBookingSystem.Core.Services;

public class UserService : BaseService<User>, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAddressService _addressService;
    private readonly ICustomerService _customerService;
    private readonly ICourtOwnerService _courtOwnerService;
    public UserService(IUserRepository repository, IUserRepository userRepository, IAddressService addressService, ICustomerService customerService, ICourtOwnerService courtOwnerService) : base(repository)
    {
        _userRepository = userRepository;
        _addressService = addressService;
        _customerService = customerService;
        _courtOwnerService = courtOwnerService;
    }

    public ServiceResult ChangePassword(UpdatePasswordDTO updatePasswordDTO, Guid id)
    {
        try
        {
            var errorData = new Dictionary<string, string>();
            //Lấy user từ id
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 404,
                    UserMsg: "User not found",
                    DevMsg: "User not found"
                );
            }
            //Check password cũ
            if (!user.Password.Equals(updatePasswordDTO.CurrentPassword))
            {
                errorData.Add("CurrentPassword", "Mật khẩu cũ không đúng");
            }
            //Check password confirm
            if (!updatePasswordDTO.NewPassword.Equals(updatePasswordDTO.ConfirmNewPassword))
            {
                errorData.Add("ConfirmPassword", "Password confirm không trùng khớp");
            }

            //Trả về kết quả
            if (errorData.Any())
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 400,
                    UserMsg: "Validation errors occurred",
                    DevMsg: "Validation failed for one or more fields",
                    Data: errorData
                );
            }
            //Nếu thành công thì thực hiên update password
            var updateRes = base.UpdateSpecifiedColumnsService(new User { Password = updatePasswordDTO.NewPassword }, id, new List<string> { nameof(User.Password) });
            //Trả về kết quả thành công
            if (updateRes.Success)
            {
                return CommonMethods.CreateServiceResult(
                    Success: true,
                    StatusCode: 200,
                    UserMsg: "Password changed successfully",
                    DevMsg: "Password changed successfully"
                );
            }
            else
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 500,
                    UserMsg: "Failed to change password",
                    DevMsg: "Failed to change password"
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return CommonMethods.CreateServiceResult(
                Success: false,
                StatusCode: 500,
                UserMsg: "Failed to change password",
                DevMsg: e.Message
            );
        }
    }

    public ServiceResult Register(User user, Dictionary<string, string>? errorData = null)
    {
        try
        {
            // 1. Check các lỗi của user
            var userErrors = CheckRegisterUserErrors(user, errorData);

            // 2. Nếu có bất kì lỗi nào thì return lỗi
            if (userErrors.Any())
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 400,
                    UserMsg: "Validation errors occurred",
                    DevMsg: "Validation failed for one or more fields",
                    Data: errorData
                );
            }

            // 3. Success case: Register user
            user.Id = Guid.NewGuid();
            //Nếu user ok thì thêm address
            var newAddressId = Guid.NewGuid();
            _addressService.InsertService(
                new Address
                {
                    Id = newAddressId,
                    City = "",
                    District = "",
                    Street = "",
                    Ward = ""
                });

            //Gán newAddressId vào user
            user.AddressId = newAddressId;
            //Các thuộc tính khác của user
            user.IsActive = 1;
            var newCode = _userRepository.FindLargestValueEndsWithNumberInColumn(nameof(User.Code));
            user.Code = int.TryParse(newCode, out int parsedCode) ? parsedCode + 1 : 0;
            user.AvatarUrl = "";
            var insertRes = base.InsertService(user);
            if (insertRes.Success)
            {
                if (user.RoleId == (int)RoleEnum.Customer)
                {
                    _customerService.InsertService(new Customer { Id = Guid.NewGuid(), UserId = user.Id });
                }
                else if (user.RoleId == (int)RoleEnum.CourtOwner)
                {
                    _courtOwnerService.InsertService(new CourtOwner { Id = Guid.NewGuid(), UserId = user.Id });
                }
                return CommonMethods.CreateServiceResult(
                    Success: true,
                    StatusCode: 201,
                    UserMsg: "Account created successfully",
                    DevMsg: "Account created successfully"
                );
            }
            else
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 500,
                    UserMsg: "Failed to create account: " + insertRes.UserMsg,
                    DevMsg: "Failed to create account: " + insertRes.DevMsg
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CommonMethods.CreateServiceResult(
                Success: false,
                StatusCode: 500,
                UserMsg: "Failed to create account",
                DevMsg: e.Message
            );
        }
    }

    public ServiceResult UpdateService(UpdateUserDTO updateUserDTO, Guid id)
    {
        try
        {
            // 0. Tạo user từ dto
            var user = new User
            {
                Id = id,
                Name = updateUserDTO.Name,
                Email = updateUserDTO.Email,
                PhoneNumber = updateUserDTO.PhoneNumber,
            };

            // 1. Check các lỗi của user
            var userErrors = CheckUpdateUserErrors(user);

            foreach (var item in userErrors)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }

            // 2. Nếu có bất kì lỗi nào thì return lỗi
            if (userErrors.Any())
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 400,
                    UserMsg: "Validation errors occurred",
                    DevMsg: "Validation failed for one or more fields",
                    Data: userErrors
                );
            }

            // 3. Success case: Update user
            List<string> updateColumns = new List<string> { nameof(User.Name), nameof(User.Email), nameof(User.PhoneNumber) };
            var updateRes = base.UpdateSpecifiedColumnsService(user, id, updateColumns);

            if (updateRes.Success)
            {
                return CommonMethods.CreateServiceResult(
                    Success: true,
                    StatusCode: 200,
                    UserMsg: "Account updated successfully",
                    DevMsg: "Account updated successfully"
                );
            }
            else
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 500,
                    UserMsg: "Failed to update account: " + updateRes.UserMsg,
                    DevMsg: "Failed to update account: " + updateRes.DevMsg
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CommonMethods.CreateServiceResult(
                Success: false,
                StatusCode: 500,
                UserMsg: "Failed to update account",
                DevMsg: e.Message
            );
        }
    }

    private Dictionary<string, string> CheckRegisterUserErrors(User user, Dictionary<string, string>? errorData = null)
    {
        errorData ??= new Dictionary<string, string>();
        // 1. Check username valid
        if (!CustomValidationMethods.IsValidUsername(user.Username))
        {
            errorData.Add("Username", "Username không hợp lệ");
        }

        // 2. Check email valid
        if (!CustomValidationMethods.IsValidEmail(user.Email))
        {
            errorData.Add("Email", "Email không hợp lệ");
        }

        // 3. Check full name valid
        if (!CustomValidationMethods.IsValidFullName(user.Name))
        {
            errorData.Add("Name", "Tên không hợp lệ");
        }

        // 4. Check role valid
        // Kiểm tra nếu RoleId có giá trị trước khi sử dụng
        if (user.RoleId == null || !Enum.IsDefined(typeof(RoleEnum), user.RoleId))
        {
            errorData.Add("Role", "Role không hợp lệ");
        }
        else
        {
            if ((RoleEnum)user.RoleId != RoleEnum.Customer && (RoleEnum)user.RoleId != RoleEnum.CourtOwner)
            {
                errorData.Add("Role", "Role không hợp lệ (chỉ được là CourtOwner hoặc Customer)");
            }
        }

        List<string> columsCheck = new List<string> { nameof(User.Username), nameof(User.Email), nameof(User.PhoneNumber) };
        List<string> duplicatedColumns = _userRepository.HasDuplicateValuesInOtherRecords(user, columsCheck);
        if (duplicatedColumns.Any())
        {
            foreach (var column in duplicatedColumns)
            {
                errorData.Add(column, column + " đã tồn tại");
            }
        }
        return errorData;
    }

    private Dictionary<string, string> CheckUpdateUserErrors(User user, Dictionary<string, string>? errorData = null)
    {
        errorData ??= new Dictionary<string, string>();
        // 1. Check email valid
        if (!CustomValidationMethods.IsValidEmail(user.Email))
        {
            errorData.Add("Email", "Email không hợp lệ");
        }

        // 2. Check full name valid
        if (!CustomValidationMethods.IsValidFullName(user.Name))
        {
            errorData.Add("FullName", "Tên không hợp lệ");
        }
        List<string> columsCheck = new List<string> { nameof(User.Username), nameof(User.Email), nameof(User.PhoneNumber) };
        List<string> duplicatedColumns = _userRepository.HasDuplicateValuesInOtherRecords(user, columsCheck);
        if (duplicatedColumns.Any())
        {
            foreach (var column in duplicatedColumns)
            {
                errorData.Add(column, column + " đã tồn tại");
            }
        }
        return errorData;
    }

    public override ServiceResult InsertService(User entity)
    {
        try
        {
            if (entity.Id == null)
            {
                entity.Id = Guid.NewGuid();
            }
            if (entity.AddressId == null)
            {
                var address = new Address
                {
                    Id = Guid.NewGuid(),
                    City = "",
                    District = "",
                    Street = "",
                    Ward = ""
                };
                _addressService.InsertService(address);
                entity.AddressId = address.Id;
            }
            entity.IsActive = 1;
            var newCode = _userRepository.FindLargestValueEndsWithNumberInColumn(nameof(User.Code));
            entity.Code = int.TryParse(newCode, out int parsedCode) ? parsedCode + 1 : 0;
            if (string.IsNullOrEmpty(entity.AvatarUrl))
            {
                entity.AvatarUrl = "";
            }
            var insertRs = base.InsertService(entity);
            if (insertRs.Success)
            {
                if (entity.RoleId == (int)RoleEnum.Customer)
                {
                    _customerService.InsertService(new Customer { Id = Guid.NewGuid(), UserId = entity.Id });
                }
                else if (entity.RoleId == (int)RoleEnum.CourtOwner)
                {
                    _courtOwnerService.InsertService(new CourtOwner { Id = entity.Id, UserId = entity.Id });
                }
            }
            return insertRs;
        }
        catch (Exception e)
        {
            return CommonMethods.CreateServiceResult(
                Success: false,
                StatusCode: 500,
                UserMsg: "Failed to create account",
                DevMsg: e.Message
            );
        }
    }

    public ServiceResult GetPublicInfoService(Guid userId)
    {
        try
        {
            var user = _userRepository.GetById(userId);
            
            if (user == null)
            {
                return CreateServiceResult(Success: false, StatusCode: 404, UserMsg: "User not found", DevMsg: "User not found in database.");
            }
            
            var userResult = new CourtOwnerInfoDto()
            {
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return CreateServiceResult(Success: true, StatusCode: 200, Data: userResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Error", DevMsg: e.Message);
        }
    }
    
    public ServiceResult GetInfoByCustomerId(Guid customerId)
    {
        try
        {
            var customerServiceRs = _customerService.GetByIdService(customerId);
            Customer customer = customerServiceRs.Data as Customer;

            if (customer == null)
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 404,
                    UserMsg: "Customer not found",
                    DevMsg: "Customer not found"
                );
            }

            User user = _userRepository.GetById(customer.UserId.Value);

            if (user == null)
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 404,
                    UserMsg: "User not found",
                    DevMsg: "User not found"
                );
            }
            CustomerInfoDto userInfoDTO = new CustomerInfoDto()
            {
                Id = user.Id.Value,
                CustomerId = customer.Id.Value,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl
            };
            return CommonMethods.CreateServiceResult(
                Success: true,
                StatusCode: 200,
                Data: userInfoDTO
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CommonMethods.CreateServiceResult(
                Success: false,
                StatusCode: 500,
                UserMsg: "Failed to get user info",
                DevMsg: e.Message
            );
        }
    }
    
    public ServiceResult GetInfoByUserId(Guid userId)
    {
        try
        {
            User? user = _userRepository.GetById(userId);

            if (user == null)
            {
                return CommonMethods.CreateServiceResult(
                    Success: false,
                    StatusCode: 404,
                    UserMsg: "User not found",
                    DevMsg: "User not found"
                );
            }
            
            UserInfoDto userInfoDTO = new UserInfoDto()
            {
                Id = user.Id!.Value,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl
            };

            return CommonMethods.CreateServiceResult(
                Success: true,
                StatusCode: 200,
                Data: userInfoDTO
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CommonMethods.CreateServiceResult(
                Success: false,
                StatusCode: 500,
                UserMsg: "Failed to get user info",
                DevMsg: e.Message
            );
        }
    }

}