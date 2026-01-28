using System.Threading;
using System.Threading.Tasks;

namespace FreeStays.Application.Common.Interfaces;

/// <summary>
/// Admin tarafından seçilen popüler destinasyonlar için cache ısınlatma servisi.
/// Seçim yapıldığında SunHotels statik verileri arka planda DB cache tablolarına yazılır.
/// </summary>
public interface IPopularDestinationWarmupService
{
    /// <summary>
    /// Belirli bir SunHotels destinasyonu için otel ve oda statik verilerini arka planda ısınlat.
    /// </summary>
    Task WarmDestinationAsync(string destinationId, CancellationToken cancellationToken = default);
}