
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenHoangPhuoc_AttendanceAPI.DTOs;
using NguyenHoangPhuoc_AttendanceAPI.Models;

namespace NguyenHoangPhuoc_AttendanceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IncidentAlertsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IncidentAlertsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateIncidentAlertRequest request)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.Latitude.HasValue != request.Longitude.HasValue)
            {
                return BadRequest(new
                {
                    message = "Latitude và Longitude phải được gửi cùng nhau."
                });
            }

            var alert = new IncidentAlert
            {
                ChildUserId = userId,
                IncidentType = request.IncidentType,
                SoundLevelDb = request.SoundLevelDb,
                MotionIntensity = request.MotionIntensity,
                AudioConfidence = request.AudioConfidence,
                RiskLevel = request.RiskLevel,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LocationAccuracyMeters =
                    request.LocationAccuracyMeters,
                Status = "New",
                DetectedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.IncidentAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = alert.Id },
                new
                {
                    alert.Id,
                    alert.IncidentType,
                    alert.RiskLevel,
                    alert.Latitude,
                    alert.Longitude,
                    alert.Status,
                    alert.CreatedAtUtc
                });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var alert = await _context.IncidentAlerts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.ChildUserId == userId);

            if (alert == null)
                return NotFound();

            return Ok(alert);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var alerts = await _context.IncidentAlerts
                .AsNoTracking()
                .Where(a => a.ChildUserId == userId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(100)
                .ToListAsync();

            return Ok(alerts);
        }
    }
}
