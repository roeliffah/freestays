using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class EmailSettingRepository : Repository<EmailSetting>, IEmailSettingRepository
{
    public EmailSettingRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<EmailSetting?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailSettings
            .FirstOrDefaultAsync(x => x.IsDefault && x.IsActive, cancellationToken);
    }

    public async Task<EmailSetting?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailSettings
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            ?? await GetDefaultAsync(cancellationToken);
    }
}
