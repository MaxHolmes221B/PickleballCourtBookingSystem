﻿using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.Entities;

namespace PickleballCourtBookingSystem.Core.Interfaces.Infrastructure;

public interface ICourtRepository : IBaseRepository<Court>
{
    public IEnumerable<Court> GetCourtsByCourtClusterId(Guid courtClusterId);
}