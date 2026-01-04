using FreeStays.Domain.Entities;
using FreeStays.Infrastructure.Persistence.Context;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers.Admin;

[Route("api/v1/admin/hangfire")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class HangfireManagementController : BaseApiController
{
    private readonly FreeStaysDbContext _dbContext;
    private readonly ILogger<HangfireManagementController> _logger;

    public HangfireManagementController(
        FreeStaysDbContext dbContext,
        ILogger<HangfireManagementController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Tüm recurring job'ları listeler
    /// </summary>
    [HttpGet("recurring-jobs")]
    public IActionResult GetRecurringJobs()
    {
        var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        var jobs = recurringJobs.Select(MapRecurringJob).ToList();

        return Ok(jobs);
    }

    /// <summary>
    /// Belirli bir recurring job'ın detaylarını getirir
    /// </summary>
    [HttpGet("recurring-jobs/{jobId}")]
    public IActionResult GetRecurringJobById(string jobId)
    {
        try
        {
            var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound(new { error = $"Job with id '{jobId}' not found" });
            }

            return Ok(MapRecurringJob(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recurring job: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir recurring job'ı tetikler (manuel başlatma)
    /// </summary>
    [HttpPost("recurring-jobs/{jobId}/trigger")]
    public IActionResult TriggerRecurringJob(string jobId)
    {
        try
        {
            RecurringJob.TriggerJob(jobId);
            _logger.LogInformation("Recurring job triggered: {JobId}", jobId);

            return Ok(new { message = "Job triggered successfully", jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger job: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Recurring job'ı siler (durdurur)
    /// </summary>
    [HttpDelete("recurring-jobs/{jobId}")]
    public IActionResult RemoveRecurringJob(string jobId)
    {
        try
        {
            RecurringJob.RemoveIfExists(jobId);
            _logger.LogInformation("Recurring job removed: {JobId}", jobId);

            return Ok(new { message = "Job removed successfully", jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove job: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Recurring job'ın cron expression'ını günceller
    /// </summary>
    [HttpPut("recurring-jobs/{jobId}/schedule")]
    public IActionResult UpdateJobSchedule(string jobId, [FromBody] UpdateScheduleRequest request)
    {
        try
        {
            // Job'ı yeniden kaydet - bu mevcut job'ı günceller
            if (jobId == "sunhotels-static-data-sync")
            {
                RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.SunHotelsStaticDataSyncJob>(
                    jobId,
                    job => job.SyncAllStaticDataAsync(),
                    request.CronExpression,
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone ?? "Europe/Istanbul")
                    }
                );
            }
            else if (jobId == "sunhotels-basic-data-sync")
            {
                RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.SunHotelsStaticDataSyncJob>(
                    jobId,
                    job => job.SyncBasicDataAsync(),
                    request.CronExpression,
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone ?? "Europe/Istanbul")
                    }
                );
            }
            else
            {
                return BadRequest(new { error = "Unknown job ID" });
            }

            _logger.LogInformation("Job schedule updated: {JobId} -> {Cron}", jobId, request.CronExpression);

            return Ok(new { message = "Schedule updated successfully", jobId, cron = request.CronExpression });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job schedule: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Çalışan (processing) job'ları listeler
    /// </summary>
    [HttpGet("processing-jobs")]
    public IActionResult GetProcessingJobs()
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var processingJobs = monitoringApi.ProcessingJobs(0, 100);

        var jobs = processingJobs.Select(job => new
        {
            jobId = job.Key,
            serverId = job.Value.ServerId,
            startedAt = job.Value.StartedAt,
            job = job.Value.Job?.Method?.Name,
            args = job.Value.Job?.Args
        }).ToList();

        return Ok(jobs);
    }

    /// <summary>
    /// Belirli bir job'ı iptal eder (çalışıyorsa)
    /// </summary>
    [HttpDelete("jobs/{jobId}")]
    public IActionResult DeleteJob(string jobId)
    {
        try
        {
            BackgroundJob.Delete(jobId);
            _logger.LogInformation("Job deleted: {JobId}", jobId);

            return Ok(new { message = "Job deleted successfully", jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete job: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Queue istatistiklerini getirir
    /// </summary>
    [HttpGet("queue/stats")]
    public IActionResult GetQueueStats()
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var stats = monitoringApi.GetStatistics();

        return Ok(new
        {
            enqueued = stats.Enqueued,
            failed = stats.Failed,
            processing = stats.Processing,
            scheduled = stats.Scheduled,
            succeeded = stats.Succeeded,
            deleted = stats.Deleted,
            recurring = stats.Recurring,
            servers = stats.Servers,
            queues = monitoringApi.Queues().Select(q => new
            {
                name = q.Name,
                length = q.Length
            })
        });
    }

    /// <summary>
    /// Tüm başarısız job'ları siler
    /// </summary>
    [HttpDelete("queue/failed")]
    public IActionResult ClearFailedJobs()
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var failedJobs = monitoringApi.FailedJobs(0, int.MaxValue);

            int deletedCount = 0;
            foreach (var job in failedJobs)
            {
                BackgroundJob.Delete(job.Key);
                deletedCount++;
            }

            _logger.LogInformation("Cleared {Count} failed jobs", deletedCount);

            return Ok(new { message = $"Cleared {deletedCount} failed jobs", count = deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear failed jobs");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Tüm processing job'ları iptal eder
    /// </summary>
    [HttpDelete("queue/processing")]
    public IActionResult ClearProcessingJobs()
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue);

            int deletedCount = 0;
            foreach (var job in processingJobs)
            {
                BackgroundJob.Delete(job.Key);
                deletedCount++;
            }

            _logger.LogInformation("Cleared {Count} processing jobs", deletedCount);

            return Ok(new { message = $"Cleared {deletedCount} processing jobs", count = deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear processing jobs");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Database'deki job history'yi listeler
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetJobHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? jobType = null,
        [FromQuery] string? status = null)
    {
        var query = _dbContext.JobHistories.AsQueryable();

        if (!string.IsNullOrEmpty(jobType))
        {
            query = query.Where(j => j.JobType == jobType);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<JobStatus>(status, out var jobStatus))
        {
            query = query.Where(j => j.Status == jobStatus);
        }

        var total = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id,
                j.JobType,
                status = j.Status.ToString(),
                j.StartTime,
                j.EndTime,
                errorMessage = j.Message,
                duration = j.EndTime.HasValue ? (TimeSpan?)(j.EndTime.Value - j.StartTime) : null
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            jobs
        });
    }

    /// <summary>
    /// Database'deki stuck/running job'ları temizler
    /// </summary>
    [HttpPost("history/cleanup-stuck")]
    public async Task<IActionResult> CleanupStuckJobs([FromQuery] int olderThanMinutes = 30)
    {
        try
        {
            var stuckJobs = await _dbContext.JobHistories
                .Where(j => j.Status == JobStatus.Running &&
                            j.StartTime < DateTime.UtcNow.AddMinutes(-olderThanMinutes))
                .ToListAsync();

            foreach (var job in stuckJobs)
            {
                job.Status = JobStatus.Failed;
                job.EndTime = DateTime.UtcNow;
                job.Message = $"Auto-cleaned - exceeded timeout ({olderThanMinutes} minutes)";
                job.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Cleaned {Count} stuck jobs older than {Minutes} minutes",
                stuckJobs.Count, olderThanMinutes);

            return Ok(new
            {
                message = $"Cleaned {stuckJobs.Count} stuck jobs",
                count = stuckJobs.Count,
                jobs = stuckJobs.Select(j => new { j.Id, j.JobType, j.StartTime })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup stuck jobs");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cron expression helper - yaygın kullanılan pattern'ler
    /// </summary>
    [HttpGet("cron-presets")]
    public IActionResult GetCronPresets()
    {
        return Ok(new[]
        {
            new { name = "Her 5 dakikada", cron = "*/5 * * * *" },
            new { name = "Her 15 dakikada", cron = "*/15 * * * *" },
            new { name = "Her 30 dakikada", cron = "*/30 * * * *" },
            new { name = "Her saat başı", cron = "0 * * * *" },
            new { name = "Her 2 saatte", cron = "0 */2 * * *" },
            new { name = "Her 3 saatte", cron = "0 */3 * * *" },
            new { name = "Her 6 saatte", cron = "0 */6 * * *" },
            new { name = "Her 12 saatte", cron = "0 */12 * * *" },
            new { name = "Günde 1 kez (gece 00:00)", cron = "0 0 * * *" },
            new { name = "Günde 1 kez (sabah 03:00)", cron = "0 3 * * *" },
            new { name = "Günde 1 kez (sabah 06:00)", cron = "0 6 * * *" },
            new { name = "Haftada 1 kez (Pazar 00:00)", cron = "0 0 * * 0" },
            new { name = "Ayda 1 kez (1. gün 00:00)", cron = "0 0 1 * *" }
        });
    }

    /// <summary>
    /// Hangfire server istatistikleri
    /// </summary>
    [HttpGet("servers")]
    public IActionResult GetServers()
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var servers = monitoringApi.Servers();

        return Ok(servers.Select(s => new
        {
            name = s.Name,
            workersCount = s.WorkersCount,
            queues = s.Queues,
            startedAt = s.StartedAt,
            heartbeat = s.Heartbeat
        }));
    }

    private static object MapRecurringJob(RecurringJobDto job)
    {
        return new
        {
            id = job.Id,
            cron = job.Cron,
            nextExecution = job.NextExecution,
            lastExecution = job.LastExecution,
            lastJobId = job.LastJobId,
            lastJobState = job.LastJobState,
            createdAt = job.CreatedAt,
            removed = job.Removed,
            job = job.Job?.Method?.Name,
            error = job.Error
        };
    }
}

public class UpdateScheduleRequest
{
    public string CronExpression { get; set; } = string.Empty;
    public string? TimeZone { get; set; }
}
