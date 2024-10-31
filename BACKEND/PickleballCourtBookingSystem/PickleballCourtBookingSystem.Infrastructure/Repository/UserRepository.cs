﻿using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.Interfaces.DBContext;
using PickleballCourtBookingSystem.Core.Interfaces.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleballCourtBookingSystem.Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IDbContext dbContext) : base(dbContext)
        {

        }
    }
}
