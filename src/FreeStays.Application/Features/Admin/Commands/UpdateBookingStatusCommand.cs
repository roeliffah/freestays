using FluentValidation;
using FreeStays.Application.DTOs.Bookings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Commands;

public record UpdateBookingStatusCommand : IRequest<BookingDto>
{
    public Guid Id { get; init; }
    public BookingStatus Status { get; init; }
    public string? Notes { get; init; }
}

public class UpdateBookingStatusCommandValidator : AbstractValidator<UpdateBookingStatusCommand>
{
    public UpdateBookingStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, BookingDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public UpdateBookingStatusCommandHandler(
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        AutoMapper.IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BookingDto> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        if (booking == null)
        {
            throw new NotFoundException("Booking", request.Id);
        }

        booking.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            booking.Notes = request.Notes;
        }

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BookingDto>(booking);
    }
}
