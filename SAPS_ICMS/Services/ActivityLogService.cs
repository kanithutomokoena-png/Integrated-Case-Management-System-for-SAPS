using Microsoft.EntityFrameworkCore;
using SAPS_ICMS.Data;
using SAPS_ICMS.Models;
using System.Security.Claims;

namespace SAPS_ICMS.Services
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string action, string? entityType = null, int? entityId = null, string? details = null);
        Task<List<ActivityLog>> GetRecentLogsAsync(int count = 100);
        Task<List<ActivityLog>> GetLogsForUserAsync(string userId, DateTime? from = null, DateTime? to = null);
        Task<List<ActivityLog>> GetLogsForEntityAsync(string entityType, int entityId);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(string action, string? entityType = null, int? entityId = null, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = httpContext.User.Identity?.Name;
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

                var log = new ActivityLog
                {
                    UserId = userId ?? "Anonymous",
                    UserName = userName,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.Now
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - activity logging shouldn't break the app
                Console.WriteLine($"Error logging activity: {ex.Message}");
            }
        }

        public async Task<List<ActivityLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _context.ActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetLogsForUserAsync(string userId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ActivityLogs.Where(l => l.UserId == userId);

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value);

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        public async Task<List<ActivityLog>> GetLogsForEntityAsync(string entityType, int entityId)
        {
            return await _context.ActivityLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
