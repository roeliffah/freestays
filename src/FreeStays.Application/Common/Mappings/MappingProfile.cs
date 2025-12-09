using AutoMapper;
using FreeStays.Application.DTOs.Auth;
using FreeStays.Application.DTOs.Bookings;
using FreeStays.Application.DTOs.Coupons;
using FreeStays.Application.DTOs.Hotels;
using FreeStays.Domain.Entities;

namespace FreeStays.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));
        
        // Hotel mappings
        CreateMap<Hotel, HotelDto>();
        CreateMap<Hotel, HotelSearchResultDto>()
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s => 
                s.Images.OrderBy(i => i.Order).FirstOrDefault() != null 
                    ? s.Images.OrderBy(i => i.Order).First().Url 
                    : null));
        CreateMap<HotelImage, HotelImageDto>();
        CreateMap<HotelFacility, HotelFacilityDto>();
        
        // Destination mappings
        CreateMap<Destination, DestinationDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));
        
        // Booking mappings
        CreateMap<Booking, BookingDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
        
        CreateMap<HotelBooking, HotelBookingDto>()
            .ForMember(d => d.HotelName, opt => opt.MapFrom(s => s.Hotel != null ? s.Hotel.Name : null));
        
        CreateMap<FlightBooking, FlightBookingDto>();
        CreateMap<CarRental, CarRentalDto>();
        
        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
        
        // Coupon mappings
        CreateMap<Coupon, CouponDto>()
            .ForMember(d => d.DiscountType, opt => opt.MapFrom(s => s.DiscountType.ToString()));
    }
}
