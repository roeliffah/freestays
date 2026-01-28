using FreeStays.Application.Common.Interfaces;
using FreeStays.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FreeStays.Infrastructure.Services;

/// <summary>
/// Popüler destinasyonlar için cache ısınlatma (DB warmup) servis implementasyonu.
/// Hangfire üzerinden anında job queue ederek DB cache tablolarını doldurur.
/// </summary>
public class PopularDestinationWarmupService : IPopularDestinationWarmupService
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<PopularDestinationWarmupService> _logger;

    public PopularDestinationWarmupService(
        IBackgroundJobClient backgroundJobs,
        ILogger<PopularDestinationWarmupService> logger)
    {
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public Task WarmDestinationAsync(string destinationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationId))
        {
            _logger.LogWarning("WarmDestinationAsync called with empty destinationId");
            return Task.CompletedTask;
        }

        // Enqueue a Hangfire job to sync hotels for this destination (language: en)
        var jobId = _backgroundJobs.Enqueue<SunHotelsStaticDataSyncJob>(job => job.SyncHotelsForDestinationAsync(destinationId, "en"));
        _logger.LogInformation("🔥 Warmup enqueued for destination {DestinationId}. Hangfire JobId: {JobId}", destinationId, jobId);

        return Task.CompletedTask;
    }
}
