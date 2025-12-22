using System.Text.Json;
using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Commands;

public record ToggleEmailTemplateStatusCommand(Guid Id) : IRequest<EmailTemplateDto>;

public class ToggleEmailTemplateStatusCommandHandler : IRequestHandler<ToggleEmailTemplateStatusCommand, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleEmailTemplateStatusCommandHandler(IEmailTemplateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailTemplateDto> Handle(ToggleEmailTemplateStatusCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Id.ToString());
        }

        template.IsActive = !template.IsActive;

        await _repository.UpdateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EmailTemplateDto
        {
            Id = template.Id,
            Code = template.Code,
            Subject = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Subject) ?? new(),
            Body = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Body) ?? new(),
            Variables = JsonSerializer.Deserialize<List<string>>(template.Variables) ?? new(),
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt
        };
    }
}
