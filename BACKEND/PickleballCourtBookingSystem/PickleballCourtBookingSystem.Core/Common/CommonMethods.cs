﻿using PickleballCourtBookingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleballCourtBookingSystem.Core.Common
{
    /// <summary>
    /// Class những method chung hay sử dụng
    /// </summary>
    public class CommonMethods
    {
        /// <summary>
        /// CuongLM (21/12/2024)
        /// Tạo 1 service result
        /// </summary>
        /// <param name="Success">Kết quả service</param>
        /// <param name="StatusCode">Status code</param>
        /// <param name="UserMsg">Message cho user</param>
        /// <param name="DevMsg">Message cho dev</param>
        /// <param name="Data">Data của service</param>
        /// <returns></returns>
        public static ServiceResult CreateServiceResult(bool Success, int StatusCode, string? UserMsg = null,
        string? DevMsg = null, object? Data = null)
        {
            ServiceResult result = new ServiceResult();
            result.Success = Success;
            result.UserMsg = UserMsg;
            result.DevMsg = DevMsg;
            result.StatusCode = StatusCode;
            result.Data = Data;
            return result;
        }
    }
}