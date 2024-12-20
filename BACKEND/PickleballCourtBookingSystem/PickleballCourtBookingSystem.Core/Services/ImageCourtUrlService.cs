using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.Entities;
using PickleballCourtBookingSystem.Core.Interfaces.Infrastructure;
using PickleballCourtBookingSystem.Core.Interfaces.Services;

namespace PickleballCourtBookingSystem.Core.Services;

public class ImageCourtUrlService : BaseService<ImageCourtUrl>, IImageCourtUrlService
{
    public ImageCourtUrlService(IImageCourtUrlRepository repository) : base(repository)
    {
        
    }
}