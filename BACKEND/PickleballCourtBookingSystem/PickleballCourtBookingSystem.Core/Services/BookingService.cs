﻿using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.DTOs;
using PickleballCourtBookingSystem.Core.Entities;
using PickleballCourtBookingSystem.Core.Interfaces.Infrastructure;
using PickleballCourtBookingSystem.Core.Interfaces.Services;
using PickleballCourtBookingSystem.Core.PEnum;

namespace PickleballCourtBookingSystem.Core.Services;

public class BookingService : BaseService<Booking>, IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoleService _roleService;
    private readonly ICourtTimeSlotService _courtTimeSlotService;
    private readonly ICourtService _courtService;
    private readonly ICourtClusterService _courtClusterService;
    private readonly ICourtTimeBookingService _courtTimeBookingService;
    private readonly ICustomerService _customerService;
    private readonly ICourtOwnerService _courtOwnerService;
    private readonly ITimeService _timeService;
    private readonly ICancellationService _cancellationService;
    private readonly IUserService _userService;
    public BookingService(IBookingRepository repository,
        IRoleService roleService,
        ICourtTimeSlotService courtTimeSlotService, ICourtTimeBookingService courtTimeBookingService,
        ICourtService courtService, ICustomerService customerService, IBookingRepository bookingRepository,
        ITimeService timeService, ICancellationService cancellationService,
        ICourtOwnerService courtOwnerService, ICourtClusterService courtClusterService, IUserService userService) : base(repository)
    {
        _roleService = roleService;
        _courtTimeSlotService = courtTimeSlotService;
        _courtTimeBookingService = courtTimeBookingService;
        _courtService = courtService;
        _customerService = customerService;
        _bookingRepository = bookingRepository;
        _timeService = timeService;
        _cancellationService = cancellationService;
        _courtOwnerService = courtOwnerService;
        _courtClusterService = courtClusterService;
        _userService = userService;
    }

    public ServiceResult AddBooking(Guid userId, List<Guid> courtTimeSlotIds, Guid courtId)
    {
        try
        {
            if (userId == Guid.Empty || courtTimeSlotIds.Count == 0 || courtId == Guid.Empty ||
                courtTimeSlotIds.Contains(Guid.Empty))
            {
                return CreateServiceResult(false, StatusCode: 400, UserMsg: "Request error", DevMsg: "Request error");
            }

            var roleServiceResult = _roleService.GetUserRoleByUserId(userId);
            if (!roleServiceResult.Success)
            {
                return roleServiceResult;
            }


            var courtServiceResult = _courtService.GetByIdService(courtId);
            if (!courtServiceResult.Success)
            {
                return courtServiceResult;
            }

            var court = (Court)courtServiceResult.Data!;
            var amount = 0.00;
            foreach (var courtTimeSlotId in courtTimeSlotIds)
            {
                var courtTimeSlot = (CourtTimeSlot)_courtTimeSlotService.GetByIdService(courtTimeSlotId).Data!;
                if (courtTimeSlot.CourtId != court.Id)
                {
                    return CreateServiceResult(false, StatusCode: 400, UserMsg: "Request error",
                        DevMsg: "Request error");
                }

                if (courtTimeSlot.IsAvailable == 0)
                {
                    return CreateServiceResult(false, StatusCode: 400, UserMsg: "Lich san da bi dang ky",
                        DevMsg: "Lich san da bi dang ky");
                }

                if (courtTimeSlot.Date != null && courtTimeSlot.Time != null &&
                    courtTimeSlot.Date.Value + courtTimeSlot.Time.Value < _timeService.GetCurrentTime())
                {
                    return CreateServiceResult(false, StatusCode: 400,
                        UserMsg: "Ban dang ky san vao thoi gian trong qua khu",
                        DevMsg: "Thoi gian dang ky san o qua khu");
                }

                if (courtTimeSlot.Price.HasValue)
                {
                    amount += courtTimeSlot.Price.Value;
                }
            }


            var customerServiceResult = _customerService.GetCustomerByUserIdService(userId);
            if (!customerServiceResult.Success)
            {
                return customerServiceResult;
            }

            var courtClusterServiceResult = _courtClusterService.GetByIdService(court.CourtClusterId!.Value);
            if (!courtClusterServiceResult.Success)
            {
                return courtClusterServiceResult;
            }

            var customer = (Customer)customerServiceResult.Data!;
            var code = _bookingRepository.FindLargestValueEndsWithNumberInColumn(nameof(Booking.Code));

            code = code == null ? "BK0001" : "BK" + (int.Parse(code.Substring(2)) + 1).ToString("D4");

            var courtCluster = (CourtCluster)courtClusterServiceResult.Data!;
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                Code = code,
                CourtId = court.Id,
                CustomerId = customer.Id,
                CourtClusterId = court.CourtClusterId,
                CourtOwnerId = courtCluster.CourtOwnerId,
                TimeBooking = _timeService.GetCurrentTime(),
                Status = (int)BookingStatusEnum.Pending,
                PaymentStatus = 0,
                Amount = amount
            };
            var listCourtTimeBooking = new List<CourtTimeBooking>();
            var resultAddBooking = _bookingRepository.Insert(booking);
            if (resultAddBooking == 0)
            {
                return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to add booking",
                    DevMsg: "Failed to add booking");
            }

            foreach (var courtTimeSlotId in courtTimeSlotIds)
            {
                var courtTimeBooking = new CourtTimeBooking
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    CourtTimeSlotId = courtTimeSlotId
                };
                var courtTimeSlot = new CourtTimeSlot
                {
                    IsAvailable = 0
                };
                var resultUpdateCourtTimeSlot =
                    _courtTimeSlotService.UpdatePartialService(courtTimeSlot, courtTimeSlotId);
                if (!resultUpdateCourtTimeSlot.Success)
                {
                    return resultUpdateCourtTimeSlot;
                }
                listCourtTimeBooking.Add(courtTimeBooking);
            }

            var resultAddCourtTimeBooking = _courtTimeBookingService.InsertManyService(listCourtTimeBooking);
            if (resultAddCourtTimeBooking.Success)
            {
                return CreateServiceResult(Success: true, StatusCode: 201, UserMsg: "Tao booking thanh cong",
                    DevMsg: "Success to add booking");

            }

            var resultRemoveBooking = _bookingRepository.Delete(booking.Id.Value);
            return resultAddCourtTimeBooking;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to add booking", DevMsg: e.Message);
        }
    }

    public ServiceResult CourtOwnerConfirmBooking(Guid userId, Guid bookingId)
    {
        var booking = _bookingRepository.GetById(bookingId);
        if (booking == null)
        {
            return CreateServiceResult(Success: false, StatusCode: 404, UserMsg: "Booking not found", DevMsg: "Booking not found");
        }

        var resultGetCourtOwner = _courtOwnerService.GetCourtOwnerByUserIdService(userId);
        if (!resultGetCourtOwner.Success)
        {
            return resultGetCourtOwner;
        }
        var courtOwner = (CourtOwner)resultGetCourtOwner.Data!;

        var resultGetCourtCluster = _courtClusterService.GetByIdService(booking.CourtClusterId!.Value);
        if (!resultGetCourtCluster.Success)
        {
            return resultGetCourtCluster;
        }
        var courtCluster = (CourtCluster)resultGetCourtCluster.Data!;
        if (courtOwner.Id != courtCluster.CourtOwnerId)
        {
            return CreateServiceResult(Success: false, StatusCode: 403, UserMsg: "Ban khong phai la chu san trong Booking", DevMsg: "Id chu san khong phai cua chu san trong Booking");
        }
        if (booking.Status != BookingStatusEnum.Pending)
        {
            return CreateServiceResult(Success: false, StatusCode: 400, UserMsg: "Booking khong o trang thai dang dat", DevMsg: "Booking khong o trang thai dang dat");
        }
        booking.Status = BookingStatusEnum.CourtOwnerConfirmed;
        var result = _bookingRepository.Update(booking, bookingId);
        if (result != 0)
        {
            return CreateServiceResult(Success: true, StatusCode: 200, UserMsg: "Xac nhan san thanh cong");
        }
        return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to update booking", DevMsg: "Failed to update booking");
    }

    public ServiceResult CustomerConfirmBooking(Guid userId, Guid bookingId)
    {
        var booking = _bookingRepository.GetById(bookingId);
        if (booking == null)
        {
            return CreateServiceResult(Success: false, StatusCode: 404, UserMsg: "Booking not found", DevMsg: "Booking not found");
        }

        var resultGetCustomer = _customerService.GetCustomerByUserIdService(userId);
        if (!resultGetCustomer.Success)
        {
            return resultGetCustomer;
        }
        var customer = (Customer)resultGetCustomer.Data!;
        if (booking.CustomerId != customer.Id)
        {
            return CreateServiceResult(Success: false, StatusCode: 403, UserMsg: "Ban khong co quyen xac nhan", DevMsg: "Booking khong phai cua customer");
        }

        if (booking.Status != BookingStatusEnum.CourtOwnerConfirmed)
        {
            return CreateServiceResult(Success: false, StatusCode: 400, UserMsg: "Booking khong o trang thai duoc chu san xac nhan", DevMsg: "Booking khong o trang thai duoc chu san xac nhan");
        }
        booking.Status = BookingStatusEnum.Completed;
        var result = _bookingRepository.Update(booking, bookingId);
        if (result != 0)
        {
            return CreateServiceResult(Success: true, StatusCode: 200, UserMsg: "Xac nhan hoan thanh dat san thanh cong");
        }
        return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to update booking", DevMsg: "Failed to update booking");
    }

    public ServiceResult CancelBooking(Guid userId, Guid bookingId, string? reason)
    {
        try
        {
            var booking = _bookingRepository.GetById(bookingId);
            if (booking == null)
            {
                return CreateServiceResult(Success: false, StatusCode: 404, UserMsg: "Booking not found", DevMsg: "Booking not found");
            }

            var resultGetCustomer = _customerService.GetCustomerByUserIdService(userId);
            if (!resultGetCustomer.Success)
            {
                return resultGetCustomer;
            }
            var customer = (Customer)resultGetCustomer.Data!;
            if (booking.CustomerId != customer.Id)
            {
                return CreateServiceResult(Success: false, StatusCode: 403, UserMsg: "Ban khong co quyen huy san", DevMsg: "Booking khong phai cua customer");
            }

            if (booking.Status != (int)BookingStatusEnum.Pending)
            {
                return CreateServiceResult(Success: false, StatusCode: 400, UserMsg: "Booking khong o trang thai dang dat", DevMsg: "Booking khong o trang thai dang dat");
            }

            var cancelBooking = new Cancellation
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Reason = reason,
                TimeCancel = _timeService.GetCurrentTime()
            };

            var resultCreateCancelBooking = _cancellationService.InsertService(cancelBooking);
            if (!resultCreateCancelBooking.Success)
            {
                return resultCreateCancelBooking;
            }
            booking.Status = BookingStatusEnum.Canceled;
            var resultUpdateBooking = _bookingRepository.Update(booking, booking.Id!.Value);
            if (resultUpdateBooking != 0)
            {
                return CreateServiceResult(Success: true, StatusCode: 200, UserMsg: "Huy dat san thanh cong", DevMsg: "Huy dat san thanh cong");

            }
            return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to update booking", DevMsg: "Error when update status booking");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to cancel booking", DevMsg: e.Message);
        }
    }

    public ServiceResult GetBookingByStatusService(Guid userId, BookingStatusEnum status)
    {
        try
        {
            if (_userService.GetByIdService(userId).Data is not User user)
            {
                return CreateServiceResult(Success: false, StatusCode: 404, UserMsg: "User not found", DevMsg: "User not found");
            }
            if (user.RoleId == (int)RoleEnum.Customer)
            {
                Customer customer = _customerService.GetCustomerByUserIdService(userId).Data as Customer;
                Dictionary<string, object> conditions = new Dictionary<string, object>
                {
                    {nameof(Booking.CustomerId), customer.Id}
                };
                //Nếu chỉ muốn lấy ra booking ở trạng thái nào đó
                if (status != BookingStatusEnum.All)
                {
                    conditions.Add(nameof(Booking.Status), (int)status);
                }
                Dictionary<string, string> order = new Dictionary<string, string>
                {
                    {nameof(Booking.TimeBooking), "desc"}
                };
                var res = _bookingRepository.GetByMultipleConditions(conditions, order);
                return CreateServiceResult(Success: true, StatusCode: 200, Data: res);
            }
            else if (user.RoleId == (int)RoleEnum.CourtOwner)
            {
                CourtOwner courtOwner = _courtOwnerService.GetCourtOwnerByUserIdService(userId).Data as CourtOwner;
                Dictionary<string, object> conditions = new Dictionary<string, object>
                {
                    {nameof(Booking.CourtOwnerId), courtOwner.Id},
                };
                //Nếu chỉ muốn lấy ra booking ở trạng thái nào đó
                if (status != BookingStatusEnum.All)
                {
                    conditions.Add(nameof(Booking.Status), (int)status);
                }
                Dictionary<string, string> order = new Dictionary<string, string>
                {
                    {nameof(Booking.TimeBooking), "desc"}
                };
                var res = _bookingRepository.GetByMultipleConditions(conditions, order);
                return CreateServiceResult(Success: true, StatusCode: 200, Data: res);
            }
            return CreateServiceResult(Success: false, StatusCode: 400, UserMsg: "Role not found", DevMsg: "Role not found");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to get booking", DevMsg: e.Message);
        }
    }

    public ServiceResult GetCourtTimeSlotBookingIdService(Guid bookingId)
    {
        try
        {
            var conditions = new Dictionary<string, object>
            {
                {nameof(CourtTimeBooking.BookingId), bookingId}
            };
            var courtTimeBooingRes = _courtTimeBookingService.GetByMultipleConditionsService(conditions);
            List<CourtTimeBooking> courtTimeBookings = courtTimeBooingRes.Data as List<CourtTimeBooking>;
            var courtTimeSlots = new List<CourtTimeSlot>();
            foreach (var courtTimeBooking in courtTimeBookings)
            {
                if (_courtTimeSlotService.GetByIdService(courtTimeBooking.CourtTimeSlotId.Value).Data is CourtTimeSlot courtTimeSlot)
                {
                    courtTimeSlots.Add(courtTimeSlot);
                }
            }
            return CreateServiceResult(Success: true, StatusCode: 200, Data: courtTimeSlots);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return CreateServiceResult(Success: false, StatusCode: 500, UserMsg: "Failed to get court time booking", DevMsg: e.Message);
        }
    }
}