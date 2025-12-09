using FluentValidation;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Bookings.Commands;

public record CreateHotelBookingCommand : IRequest<Guid>
{
    public Guid HotelId { get; init; }
    public string? RoomTypeId { get; init; }
    public string? RoomTypeName { get; init; }
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public int Adults { get; init; }
    public int Children { get; init; }
    public string GuestName { get; init; } = string.Empty;
    public string GuestEmail { get; init; } = string.Empty;
    public string? SpecialRequests { get; init; }
    public decimal TotalPrice { get; init; }
    public string? CouponCode { get; init; }
}

public class CreateHotelBookingCommandValidator : AbstractValidator<CreateHotelBookingCommand>
{
    public CreateHotelBookingCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.CheckIn).GreaterThan(DateTime.UtcNow.Date);
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn);
        RuleFor(x => x.Adults).GreaterThan(0);
        RuleFor(x => x.GuestName).NotEmpty();
        RuleFor(x => x.GuestEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.TotalPrice).GreaterThan(0);
    }
}

public class CreateHotelBookingCommandHandler : IRequestHandler<CreateHotelBookingCommand, Guid>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateHotelBookingCommandHandler(
        IBookingRepository bookingRepository,
        IHotelRepository hotelRepository,
        ICouponRepository couponRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _hotelRepository = hotelRepository;
        _couponRepository = couponRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Guid> Handle(CreateHotelBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedException();
        
        var hotel = await _hotelRepository.GetByIdAsync(request.HotelId, cancellationToken);
        if (hotel == null)
            throw new NotFoundException("Hotel", request.HotelId);
        
        var totalPrice = request.TotalPrice;
        decimal couponDiscount = 0;
        Coupon? coupon = null;
        
        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, cancellationToken);
            if (coupon != null && coupon.IsActive && 
                coupon.ValidFrom <= DateTime.UtcNow && 
                coupon.ValidUntil >= DateTime.UtcNow)
            {
                if (coupon.DiscountType == DiscountType.Percentage)
                {
                    couponDiscount = totalPrice * (coupon.DiscountValue / 100);
                }
                else
                {
                    couponDiscount = coupon.DiscountValue;
                }
                
                totalPrice -= couponDiscount;
                coupon.UsedCount++;
            }
        }
        
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId.Value,
            Type = BookingType.Hotel,
            Status = BookingStatus.Pending,
            TotalPrice = totalPrice,
            Commission = totalPrice * 0.10m, // 10% commission
            Currency = hotel.Currency,
            CouponId = coupon?.Id,
            CouponDiscount = couponDiscount
        };
        
        var hotelBooking = new HotelBooking
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            HotelId = request.HotelId,
            RoomTypeId = request.RoomTypeId,
            RoomTypeName = request.RoomTypeName,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Adults = request.Adults,
            Children = request.Children,
            GuestName = request.GuestName,
            GuestEmail = request.GuestEmail,
            SpecialRequests = request.SpecialRequests
        };
        
        booking.HotelBooking = hotelBooking;
        
        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return booking.Id;
    }
}
