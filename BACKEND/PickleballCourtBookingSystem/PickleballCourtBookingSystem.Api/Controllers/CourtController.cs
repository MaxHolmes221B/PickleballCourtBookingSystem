﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickleballCourtBookingSystem.Api.Models;
using PickleballCourtBookingSystem.Core.DTOs;
using PickleballCourtBookingSystem.Core.Entities;
using PickleballCourtBookingSystem.Core.Interfaces.Infrastructure;
using PickleballCourtBookingSystem.Core.Interfaces.Services;
using PickleballCourtBookingSystem.Core.Services;
using PickleballCourtBookingSystem.Infrastructure.Repository;

namespace PickleballCourtBookingSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtController : ControllerBase
    {
        private readonly ICourtService _courtService;
        public CourtController(ICourtService courtService)
        {
            _courtService = courtService;
        }

        [HttpGet]
        public IActionResult GetAllCourt()
        {
            var result = _courtService.GetAllService();
            return StatusCode(result.StatusCode, result);
        }
        
        [HttpGet("{id}")]
        public IActionResult GetCourtById(Guid id)
        {
            var result = _courtService.GetByIdService(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public IActionResult PostCourt([FromBody] Court court)
        {
            court.Id = Guid.NewGuid();
            var result = _courtService.InsertService(court);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        public IActionResult UpdateCourt([FromBody] Court court, Guid id)
        {
            var result = _courtService.UpdateService(court, id);
            return StatusCode(result.StatusCode, result);
        }
        
        [HttpDelete("{id}")]
        public IActionResult DeleteCourt(Guid id)
        {
            var result = _courtService.DeleteService(id);
            return StatusCode(result.StatusCode, result);
        }
        
        [HttpGet("get-court-by-courtCluster/{courtClusterId}")]
        public IActionResult GetCourtsOfACourtCluster(Guid courtClusterid)
        {
            var result = _courtService.GetCourtsByCourtClusterId(courtClusterid);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("dummy")]
        public IActionResult PostListCourtCluster([FromBody] List<Court> courts)
        {
            Dictionary<string, ServiceResult> failedRecords = new Dictionary<string, ServiceResult>();
            foreach (var court in courts)
            {
                var result = _courtService.InsertService(court);
                if (!result.Success)
                {
                    failedRecords.Add(court.Id.ToString(), result);
                }
            }
            return StatusCode(201,
                new
                {
                    Result = $"insert thành công {courts.Count - failedRecords.Count}/{courts.Count} bản ghi",
                    Failed = failedRecords
                });
        }
    }
}
