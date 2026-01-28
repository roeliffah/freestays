# DTOs to Add to HotelBookingsController.cs

Add these records before `#endregion`:

```csharp
public record HotelCheckoutRequest(
    int HotelId,
    int RoomId,
    int RoomTypeId,
    int MealId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Rooms,
    int Adults,
    int Children,
    string GuestName,
    string GuestEmail,
    string? Phone,
    string? SpecialRequests,
    decimal SearchPrice,
    bool IsSuperDeal,
    string SuccessUrl,
    string CancelUrl,
    string? ChildrenAges = "",
    int? Infant = 0,
    string? Currency = "EUR",
    string? Language = "en",
    string? CustomerCountry = "TR"
);

public record CheckoutSessionResponse(
    string SessionId,
    Guid BookingId,
    string PreBookCode,
    decimal TotalPrice,
    string Currency,
    string Message
);
```
