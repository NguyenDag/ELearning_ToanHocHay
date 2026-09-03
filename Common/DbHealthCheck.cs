using ELearning_ToanHocHay_Control.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ELearning_ToanHocHay_Control.Common
{
    /// <summary>P7 — readiness: can the API reach the database?</summary>
    public class DbHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;

        public DbHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var ok = await _context.Database.CanConnectAsync(cancellationToken);
                return ok
                    ? HealthCheckResult.Healthy("Database reachable")
                    : HealthCheckResult.Unhealthy("Database not reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed", ex);
            }
        }
    }
}
