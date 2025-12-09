using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Commands;

public record UpdateSeoSettingCommand : IRequest<SeoSettingDto>
{
    public string Locale { get; init; } = string.Empty;
    public string PageType { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string? OgImage { get; init; }
}

public class UpdateSeoSettingCommandHandler : IRequestHandler<UpdateSeoSettingCommand, SeoSettingDto>
{
    private readonly ISeoSettingRepository _seoSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSeoSettingCommandHandler(ISeoSettingRepository seoSettingRepository, IUnitOfWork unitOfWork)
    {
        _seoSettingRepository = seoSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SeoSettingDto> Handle(UpdateSeoSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _seoSettingRepository.GetByLocaleAndPageTypeAsync(request.Locale, request.PageType, cancellationToken);

        if (setting == null)
        {
            setting = new SeoSetting
            {
                Id = Guid.NewGuid(),
                Locale = request.Locale,
                PageType = request.PageType,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                MetaKeywords = request.MetaKeywords,
                OgImage = request.OgImage
            };
            await _seoSettingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.MetaTitle = request.MetaTitle;
            setting.MetaDescription = request.MetaDescription;
            setting.MetaKeywords = request.MetaKeywords;
            setting.OgImage = request.OgImage;
            setting.UpdatedAt = DateTime.UtcNow;
            await _seoSettingRepository.UpdateAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SeoSettingDto
        {
            Id = setting.Id,
            Locale = setting.Locale,
            PageType = setting.PageType,
            MetaTitle = setting.MetaTitle,
            MetaDescription = setting.MetaDescription,
            MetaKeywords = setting.MetaKeywords,
            OgImage = setting.OgImage
        };
    }
}
