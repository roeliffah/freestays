using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/external-services")]
public class ExternalServicesController : BaseApiController
{
    private readonly IExternalServiceConfigRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExternalServicesController> _logger;

    public ExternalServicesController(
        IExternalServiceConfigRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ExternalServicesController> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all external service configurations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var services = await _repository.GetAllAsync();

        var result = services.Select(s => new
        {
            s.Id,
            s.ServiceName,
            s.BaseUrl,
            s.ApiKey,
            s.ApiSecret,
            s.Username,
            s.Password,
            s.AffiliateCode,
            s.IntegrationMode,
            s.IsActive,
            s.Settings,
            s.CreatedAt,
            s.UpdatedAt
        }).ToList();

        return Ok(new { items = result, total = result.Count });
    }

    /// <summary>
    /// Get external service configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var service = await _repository.GetByIdAsync(id);

        if (service == null)
        {
            return NotFound(new { message = "External service not found" });
        }

        var result = new
        {
            service.Id,
            service.ServiceName,
            service.BaseUrl,
            HasApiKey = !string.IsNullOrEmpty(service.ApiKey),
            HasApiSecret = !string.IsNullOrEmpty(service.ApiSecret),
            HasUsername = !string.IsNullOrEmpty(service.Username),
            HasPassword = !string.IsNullOrEmpty(service.Password),
            service.AffiliateCode,
            service.IntegrationMode,
            service.IsActive,
            service.Settings,
            service.CreatedAt,
            service.UpdatedAt
        };

        return Ok(result);
    }

    /// <summary>
    /// Get external service configuration by service name
    /// </summary>
    [HttpGet("by-name/{serviceName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByServiceName(string serviceName)
    {
        var service = await _repository.GetByServiceNameAsync(serviceName);

        if (service == null)
        {
            return NotFound(new { message = $"External service '{serviceName}' not found" });
        }

        var result = new
        {
            service.Id,
            service.ServiceName,
            service.BaseUrl,
            HasApiKey = !string.IsNullOrEmpty(service.ApiKey),
            HasApiSecret = !string.IsNullOrEmpty(service.ApiSecret),
            HasUsername = !string.IsNullOrEmpty(service.Username),
            HasPassword = !string.IsNullOrEmpty(service.Password),
            service.AffiliateCode,
            service.IntegrationMode,
            service.IsActive,
            service.Settings,
            service.CreatedAt,
            service.UpdatedAt
        };

        return Ok(result);
    }

    /// <summary>
    /// Create new external service configuration
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExternalServiceConfigRequest request)
    {
        var service = new ExternalServiceConfig
        {
            Id = Guid.NewGuid(),
            ServiceName = request.ServiceName,
            BaseUrl = request.BaseUrl,
            ApiKey = request.ApiKey,
            ApiSecret = request.ApiSecret,
            Username = request.Username,
            Password = request.Password,
            AffiliateCode = request.AffiliateCode,
            IntegrationMode = request.IntegrationMode,
            IsActive = request.IsActive,
            Settings = request.Settings,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("External service config created: {ServiceName} (ID: {Id})",
            service.ServiceName, service.Id);

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, new
        {
            service.Id,
            service.ServiceName,
            service.BaseUrl,
            service.IntegrationMode,
            service.IsActive
        });
    }

    /// <summary>
    /// Update external service configuration
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExternalServiceConfigRequest request)
    {
        var service = await _repository.GetByIdAsync(id);

        if (service == null)
        {
            return NotFound(new { message = "External service not found" });
        }

        if (!string.IsNullOrEmpty(request.BaseUrl))
            service.BaseUrl = request.BaseUrl;

        if (request.ApiKey != null)
            service.ApiKey = request.ApiKey;

        if (request.ApiSecret != null)
            service.ApiSecret = request.ApiSecret;

        if (request.Username != null)
            service.Username = request.Username;

        if (request.Password != null)
            service.Password = request.Password;

        if (request.AffiliateCode != null)
            service.AffiliateCode = request.AffiliateCode;

        if (request.IntegrationMode.HasValue)
            service.IntegrationMode = request.IntegrationMode.Value;

        if (request.IsActive.HasValue)
            service.IsActive = request.IsActive.Value;

        if (request.Settings != null)
            service.Settings = request.Settings;

        service.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("External service config updated: {ServiceName} (ID: {Id})",
            service.ServiceName, service.Id);

        return Ok(new
        {
            service.Id,
            service.ServiceName,
            service.BaseUrl,
            service.IntegrationMode,
            service.IsActive,
            message = "External service configuration updated successfully"
        });
    }

    /// <summary>
    /// Toggle external service active status
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var service = await _repository.GetByIdAsync(id);

        if (service == null)
        {
            return NotFound(new { message = "External service not found" });
        }

        service.IsActive = !service.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("External service {ServiceName} status changed to: {IsActive}",
            service.ServiceName, service.IsActive);

        return Ok(new
        {
            service.Id,
            service.ServiceName,
            service.IsActive,
            message = $"Service {(service.IsActive ? "activated" : "deactivated")} successfully"
        });
    }

    /// <summary>
    /// Delete external service configuration
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _repository.GetByIdAsync(id);

        if (service == null)
        {
            return NotFound(new { message = "External service not found" });
        }

        await _repository.DeleteAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning("External service config deleted: {ServiceName} (ID: {Id})",
            service.ServiceName, service.Id);

        return NoContent();
    }

    /// <summary>
    /// Test external service connection (placeholder for future implementation)
    /// </summary>
    [HttpPost("{id}/test-connection")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        var service = await _repository.GetByIdAsync(id);

        if (service == null)
        {
            return NotFound(new { message = "External service not found" });
        }

        // TODO: Implement actual connection test based on service type
        return Ok(new
        {
            service.ServiceName,
            status = "not_implemented",
            message = "Connection test not yet implemented for this service"
        });
    }
}

// Request DTOs
public record CreateExternalServiceConfigRequest(
    string ServiceName,
    string BaseUrl,
    string? ApiKey = null,
    string? ApiSecret = null,
    string? Username = null,
    string? Password = null,
    string? AffiliateCode = null,
    FreeStays.Domain.Enums.ServiceIntegrationMode IntegrationMode = FreeStays.Domain.Enums.ServiceIntegrationMode.Api,
    bool IsActive = true,
    string? Settings = null);

public record UpdateExternalServiceConfigRequest(
    string? BaseUrl = null,
    string? ApiKey = null,
    string? ApiSecret = null,
    string? Username = null,
    string? Password = null,
    string? AffiliateCode = null,
    FreeStays.Domain.Enums.ServiceIntegrationMode? IntegrationMode = null,
    bool? IsActive = null,
    string? Settings = null);
