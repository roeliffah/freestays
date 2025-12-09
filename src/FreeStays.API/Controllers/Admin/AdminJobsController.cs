using FreeStays.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

/// <summary>
/// Background job'ları yönetme endpoint'leri
/// </summary>
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/admin/jobs")]
[ApiController]
[Produces("application/json")]
public class AdminJobsController : ControllerBase
{
    private readonly SunHotelsStaticDataSyncJob _syncJob;
    private readonly ILogger<AdminJobsController> _logger;

    public AdminJobsController(
        SunHotelsStaticDataSyncJob syncJob,
        ILogger<AdminJobsController> logger)
    {
        _syncJob = syncJob;
        _logger = logger;
    }

    /// <summary>
    /// SunHotels tüm statik verileri senkronize et (manuel tetikleme)
    /// </summary>
    [HttpPost("sunhotels/sync-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncAllSunHotelsData()
    {
        try
        {
            _logger.LogInformation("Manual sync started for all SunHotels static data by {User}", User.Identity?.Name);
            
            await _syncJob.SyncAllStaticDataAsync();
            
            return Ok(new
            {
                success = true,
                message = "All SunHotels static data synchronized successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual SunHotels full sync");
            return StatusCode(500, new
            {
                success = false,
                message = "Error during synchronization",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// SunHotels temel verileri senkronize et (hızlı)
    /// </summary>
    [HttpPost("sunhotels/sync-basic")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncBasicSunHotelsData()
    {
        try
        {
            _logger.LogInformation("Manual basic sync started for SunHotels by {User}", User.Identity?.Name);
            
            await _syncJob.SyncBasicDataAsync();
            
            return Ok(new
            {
                success = true,
                message = "SunHotels basic data synchronized successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual SunHotels basic sync");
            return StatusCode(500, new
            {
                success = false,
                message = "Error during synchronization",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Hangfire ile arka plan job'u olarak senkronizasyonu tetikle
    /// </summary>
    [HttpPost("sunhotels/enqueue-sync-all")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult EnqueueFullSync()
    {
        var jobId = BackgroundJob.Enqueue<SunHotelsStaticDataSyncJob>(
            job => job.SyncAllStaticDataAsync());

        _logger.LogInformation("Full sync job enqueued with ID: {JobId} by {User}", jobId, User.Identity?.Name);

        return Accepted(new
        {
            success = true,
            message = "Full synchronization job has been enqueued",
            jobId = jobId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Hangfire ile arka plan job'u olarak temel senkronizasyonu tetikle
    /// </summary>
    [HttpPost("sunhotels/enqueue-sync-basic")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult EnqueueBasicSync()
    {
        var jobId = BackgroundJob.Enqueue<SunHotelsStaticDataSyncJob>(
            job => job.SyncBasicDataAsync());

        _logger.LogInformation("Basic sync job enqueued with ID: {JobId} by {User}", jobId, User.Identity?.Name);

        return Accepted(new
        {
            success = true,
            message = "Basic synchronization job has been enqueued",
            jobId = jobId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Aktif ve zamanlanmış job'ları listele
    /// </summary>
    [HttpGet("sunhotels/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetJobStatus()
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            
            // Processing jobs
            var processingJobs = monitoringApi.ProcessingJobs(0, 100)
                .Where(j => j.Value?.Job?.Type == typeof(SunHotelsStaticDataSyncJob))
                .Select(j => new
                {
                    jobId = j.Key,
                    method = j.Value.Job.Method.Name,
                    startedAt = j.Value.StartedAt
                })
                .ToList();

            // Scheduled jobs
            var scheduledJobs = monitoringApi.ScheduledJobs(0, 100)
                .Where(j => j.Value?.Job?.Type == typeof(SunHotelsStaticDataSyncJob))
                .Select(j => new
                {
                    jobId = j.Key,
                    method = j.Value.Job.Method.Name,
                    scheduleAt = j.Value.ScheduledAt
                })
                .ToList();

            // Enqueued jobs
            var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 100)
                .Where(j => j.Value?.Job?.Type == typeof(SunHotelsStaticDataSyncJob))
                .Select(j => new
                {
                    jobId = j.Key,
                    method = j.Value.Job.Method.Name,
                    enqueuedAt = j.Value.EnqueuedAt
                })
                .ToList();

            return Ok(new
            {
                message = "Job status retrieved successfully",
                processingJobs = processingJobs,
                scheduledJobs = scheduledJobs,
                enqueuedJobs = enqueuedJobs,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status");
            return Ok(new
            {
                message = "Unable to retrieve full job status",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Recurring job'ı manuel tetikle
    /// </summary>
    [HttpPost("recurring/{jobId}/trigger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult TriggerRecurringJob(string jobId)
    {
        try
        {
            // Hangfire 1.8+ için TriggerJob kullan
            #pragma warning disable CS0618
            RecurringJob.Trigger(jobId);
            #pragma warning restore CS0618
            
            _logger.LogInformation("Recurring job {JobId} triggered by {User}", jobId, User.Identity?.Name);

            return Ok(new
            {
                success = true,
                message = $"Recurring job '{jobId}' has been triggered",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering recurring job {JobId}", jobId);
            return NotFound(new
            {
                success = false,
                message = $"Recurring job '{jobId}' not found or error occurred",
                error = ex.Message
            });
        }
    }
}
