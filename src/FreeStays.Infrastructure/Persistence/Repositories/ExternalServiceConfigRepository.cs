using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class ExternalServiceConfigRepository : Repository<ExternalServiceConfig>, IExternalServiceConfigRepository
{
    public ExternalServiceConfigRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<ExternalServiceConfig?> GetByServiceNameAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await _context.ExternalServiceConfigs
            .FirstOrDefaultAsync(x => x.ServiceName == serviceName && x.IsActive, cancellationToken);
    }
}
