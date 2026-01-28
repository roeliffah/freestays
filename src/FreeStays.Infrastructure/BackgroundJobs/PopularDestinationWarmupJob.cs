using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FreeStays.Infrastructure.BackgroundJobs;

/// <summary>
/// Admin tarafından aktif/popüler destinasyonlar için nightly cache warmup job.
/// Aktif featured destinasyonları bulur ve her biri için otel/oda statik verilerini DB cache'e ısınlatır.
/// </summary>
public class PopularDestinationWarmupJob
{
    private readonly IFeaturedDestinationRepository _featuredRepo;
    private readonly IPopularDestinationWarmupService _warmupService;
    private readonly ILogger<PopularDestinationWarmupJob> _logger;

    public PopularDestinationWarmupJob(
        IFeaturedDestinationRepository featuredRepo,
        IPopularDestinationWarmupService warmupService,
        ILogger<PopularDestinationWarmupJob> logger)
    {
        _featuredRepo = featuredRepo;
        _warmupService = warmupService;
        _logger = logger;
    }

    /// <summary>
    /// Aktif featured destinasyonları için warmup başlatır.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task WarmActiveFeaturedDestinationsAsync(int maxCount = 50, Season? season = null)
    {
        _logger.LogInformation("Starting popular destination warmup (max {MaxCount}, season={Season})", maxCount, season?.ToString() ?? "null");

        try
        {
            var featured = await _featuredRepo.GetActiveAsync(count: maxCount, season: season);
            if (featured.Count == 0)
            {
                _logger.LogInformation("No active featured destinations found for warmup.");
                return;
            }

            foreach (var f in featured)
            {
                try
                {
                    await _warmupService.WarmDestinationAsync(f.DestinationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Warmup enqueue failed for destination {DestinationId}", f.DestinationId);
                }
            }

            _logger.LogInformation("Warmup enqueued for {Count} featured destinations", featured.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during popular destination warmup");
            throw;
        }
    }
}
