using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeStays.Infrastructure.ExternalServices.SunHotels;

public class SunHotelsService : ISunHotelsService
{
    private readonly HttpClient _httpClient;
    private SunHotelsConfig _config;
    private readonly ILogger<SunHotelsService> _logger;
    private readonly IExternalServiceConfigRepository _serviceConfigRepository;
    private bool _configLoaded = false;

    private const string NonStaticApiUrl = "http://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx";
    private const string StaticApiUrl = "http://xml.sunhotels.net/15/PostGet/StaticXMLAPI.asmx";

    public SunHotelsService(
        HttpClient httpClient,
        IOptions<SunHotelsConfig> config,
        ILogger<SunHotelsService> logger,
        IExternalServiceConfigRepository serviceConfigRepository)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _serviceConfigRepository = serviceConfigRepository;

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
    }

    private async Task EnsureConfigLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_configLoaded) return;

        try
        {
            var dbConfig = await _serviceConfigRepository.GetByServiceNameAsync("SunHotels", cancellationToken);

            if (dbConfig != null && dbConfig.IsActive)
            {
                _config.Username = dbConfig.Username ?? string.Empty;
                _config.Password = dbConfig.Password ?? string.Empty;
                _config.BaseUrl = dbConfig.BaseUrl;
                _config.AffiliateCode = dbConfig.AffiliateCode;

                _logger.LogInformation("SunHotels configuration loaded from database for service: {ServiceName}", dbConfig.ServiceName);
            }
            else
            {
                _logger.LogWarning("SunHotels configuration not found in database, using default config");
            }

            _configLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load SunHotels configuration from database, using default config");
            _configLoaded = true; // Don't try to load again
        }
    }

    #region Static Data Methods

    public async Task<List<SunHotelsDestination>> GetDestinationsAsync(string? language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language ?? "en" },
                { "destinationCode", "" },
                { "sortBy", "" },
                { "sortOrder", "" },
                { "exactDestinationMatch", "" }
            };

            var response = await SendStaticRequestAsync("GetDestinations", parameters, cancellationToken);
            return ParseDestinations(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting destinations from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get destinations", ex);
        }
    }

    public async Task<List<SunHotelsResort>> GetResortsAsync(string? destinationId = null, string? language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language ?? "en" },
                { "destinationID", destinationId ?? "" },
                { "destinationCode", "" },
                { "sortBy", "" },
                { "sortOrder", "" },
                { "exactDestinationMatch", "" }
            };

            var response = await SendStaticRequestAsync("GetResorts", parameters, cancellationToken);
            return ParseResorts(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resorts from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get resorts", ex);
        }
    }

    public async Task<List<SunHotelsMeal>> GetMealsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetMeals", parameters, cancellationToken);
            return ParseMeals(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meals from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get meals", ex);
        }
    }

    public async Task<List<SunHotelsRoomType>> GetRoomTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetRoomTypes", parameters, cancellationToken);
            return ParseRoomTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get room types", ex);
        }
    }

    public async Task<List<SunHotelsFeature>> GetFeaturesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetFeatures", parameters, cancellationToken);
            return ParseFeatures(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting features from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get features", ex);
        }
    }

    public async Task<List<SunHotelsLanguage>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var response = await SendStaticRequestAsync("GetLanguages", new Dictionary<string, string>(), cancellationToken);
            return ParseLanguages(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting languages from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get languages", ex);
        }
    }

    public async Task<List<SunHotelsTheme>> GetThemesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var response = await SendStaticRequestAsync("GetThemes", new Dictionary<string, string>(), cancellationToken);
            return ParseThemes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting themes from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get themes", ex);
        }
    }

    public async Task<List<SunHotelsTransferType>> GetTransferTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language },
                { "transferTypeCode", "" }
            };

            var response = await SendStaticRequestAsync("GetTransferTypes", parameters, cancellationToken);
            return ParseTransferTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get transfer types", ex);
        }
    }

    public async Task<List<SunHotelsNoteType>> GetHotelNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetHotelNoteTypes", parameters, cancellationToken);
            return ParseNoteTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel note types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get hotel note types", ex);
        }
    }

    public async Task<List<SunHotelsNoteType>> GetRoomNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetRoomNoteTypes", parameters, cancellationToken);
            return ParseNoteTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room note types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get room note types", ex);
        }
    }

    public async Task<List<SunHotelsStaticHotel>> GetStaticHotelsAndRoomsAsync(
        string? destination = null,
        string? hotelIds = null,
        string? resortIds = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting static hotels and rooms - Destination: {Destination}, HotelIds: {HotelIds}, ResortIds: {ResortIds}, Language: {Language}",
                destination ?? "null", hotelIds ?? "null", resortIds ?? "null", language);

            var parameters = new Dictionary<string, string>
            {
                { "language", language },
                { "hotelIds", hotelIds ?? string.Empty },
                { "resortIds", resortIds ?? string.Empty },
                { "accommodationTypes", string.Empty },
                { "sortBy", string.Empty },
                { "sortOrder", string.Empty },
                { "exactDestinationMatch", string.Empty }
            };
            if (!string.IsNullOrEmpty(destination))
                parameters.Add("destination", destination);

            var response = await SendStaticRequestAsync("GetStaticHotelsAndRooms", parameters, cancellationToken);
            var hotels = ParseStaticHotels(response);

            _logger.LogInformation("Successfully retrieved {Count} hotels from SunHotels", hotels.Count);
            return hotels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting static hotels from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get static hotels", ex);
        }
    }

    #endregion

    #region Search Methods

    public async Task<List<SunHotelsSearchResult>> SearchHotelsAsync(SunHotelsSearchRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "destination", request.DestinationId },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "numberOfAdults", request.Adults.ToString() },
                { "numberOfChildren", request.Children.ToString() },
                { "currency", request.Currency }
            };

            var response = await SendNonStaticRequestAsync("search", parameters, cancellationToken);
            return ParseSearchResults(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hotels from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to search hotels", ex);
        }
    }

    public async Task<List<SunHotelsSearchResultV3>> SearchHotelsV3Async(SunHotelsSearchRequestV3 request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "destination", request.DestinationId },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "rooms", request.NumberOfRooms.ToString() },
                { "adults", request.Adults.ToString() },
                { "children", request.Children.ToString() },
                { "infant", request.Infant.ToString() },
                { "currency", request.Currency },
                { "language", request.Language },
                { "b2c", request.B2C ? "1" : "0" },
                { "showCoordinates", request.ShowCoordinates ? "1" : "0" },
                { "showReviews", request.ShowReviews ? "1" : "0" },
                { "showRoomTypeName", request.ShowRoomTypeName ? "1" : "0" },
                { "excludeSharedRooms", request.ExcludeSharedRooms ? "1" : "0" },
                { "excludeSharedFacilities", request.ExcludeSharedFacilities ? "1" : "0" }
            };

            if (!string.IsNullOrEmpty(request.ChildrenAges))
                parameters.Add("childrenAges", request.ChildrenAges);
            if (!string.IsNullOrEmpty(request.HotelIds))
                parameters.Add("hotelIds", request.HotelIds);
            if (!string.IsNullOrEmpty(request.ResortIds))
                parameters.Add("resortIds", request.ResortIds);
            if (!string.IsNullOrEmpty(request.MealIds))
                parameters.Add("mealIds", request.MealIds);
            if (!string.IsNullOrEmpty(request.FeatureIds))
                parameters.Add("featureIds", request.FeatureIds);
            if (!string.IsNullOrEmpty(request.ThemeIds))
                parameters.Add("themeIds", request.ThemeIds);
            if (request.MinStarRating.HasValue)
                parameters.Add("minStarRating", request.MinStarRating.Value.ToString());
            if (request.MaxStarRating.HasValue)
                parameters.Add("maxStarRating", request.MaxStarRating.Value.ToString());
            if (request.MinPrice.HasValue)
                parameters.Add("minPrice", request.MinPrice.Value.ToString(CultureInfo.InvariantCulture));
            if (request.MaxPrice.HasValue)
                parameters.Add("maxPrice", request.MaxPrice.Value.ToString(CultureInfo.InvariantCulture));
            if (request.ReferenceLatitude.HasValue && request.ReferenceLongitude.HasValue)
            {
                parameters.Add("referencePointLatitude", request.ReferenceLatitude.Value.ToString(CultureInfo.InvariantCulture));
                parameters.Add("referencePointLongitude", request.ReferenceLongitude.Value.ToString(CultureInfo.InvariantCulture));
                if (request.MaxDistanceKm.HasValue)
                    parameters.Add("maxDistanceFromReferencePoint", request.MaxDistanceKm.Value.ToString());
            }
            if (!string.IsNullOrEmpty(request.CustomerCountry))
                parameters.Add("customerCountry", request.CustomerCountry);
            if (request.PaymentMethodId.HasValue)
                parameters.Add("paymentMethodId", request.PaymentMethodId.Value.ToString());

            var response = await SendNonStaticRequestAsync("searchV3", parameters, cancellationToken);
            return ParseSearchResultsV3(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hotels V3 from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to search hotels V3", ex);
        }
    }

    public async Task<SunHotelsSearchResultV3?> GetHotelDetailsAsync(int hotelId, DateTime checkIn, DateTime checkOut, int adults, int children = 0, string currency = "EUR", CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SunHotelsSearchRequestV3
            {
                HotelIds = hotelId.ToString(),
                DestinationId = "",
                CheckIn = checkIn,
                CheckOut = checkOut,
                Adults = adults,
                Children = children,
                Currency = currency,
                ShowCoordinates = true,
                ShowReviews = true,
                ShowRoomTypeName = true
            };

            var results = await SearchHotelsV3Async(request, cancellationToken);
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel details from SunHotels for hotel {HotelId}", hotelId);
            throw new ExternalServiceException("SunHotels", $"Failed to get hotel details for {hotelId}", ex);
        }
    }

    #endregion

    #region PreBook Methods

    public async Task<SunHotelsPreBookResult> PreBookAsync(SunHotelsPreBookRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "hotelId", request.HotelId },
                { "roomId", request.RoomId },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "numberOfAdults", request.Adults.ToString() },
                { "numberOfChildren", request.Children.ToString() }
            };

            var response = await SendNonStaticRequestAsync("preBook", parameters, cancellationToken);
            return ParsePreBookResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pre-booking from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to pre-book", ex);
        }
    }

    public async Task<SunHotelsPreBookResultV3> PreBookV3Async(SunHotelsPreBookRequestV3 request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "hotelId", request.HotelId.ToString() },
                { "roomId", request.RoomId.ToString() },
                { "roomtypeId", request.RoomTypeId.ToString() },
                { "mealId", request.MealId.ToString() },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "rooms", request.Rooms.ToString() },
                { "adults", request.Adults.ToString() },
                { "children", request.Children.ToString() },
                { "infant", request.Infant.ToString() },
                { "currency", request.Currency },
                { "language", request.Language },
                { "b2c", request.B2C ? "1" : "0" },
                { "searchPrice", request.SearchPrice.ToString(CultureInfo.InvariantCulture) },
                { "blockSuperDeal", request.BlockSuperDeal ? "1" : "0" },
                { "showPriceBreakdown", request.ShowPriceBreakdown ? "1" : "0" }
            };

            if (!string.IsNullOrEmpty(request.ChildrenAges))
                parameters.Add("childrenAges", request.ChildrenAges);
            if (!string.IsNullOrEmpty(request.CustomerCountry))
                parameters.Add("customerCountry", request.CustomerCountry);

            var response = await SendNonStaticRequestAsync("preBookV3", parameters, cancellationToken);
            return ParsePreBookResultV3(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pre-booking V3 from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to pre-book V3", ex);
        }
    }

    #endregion

    #region Book Methods

    public async Task<SunHotelsBookResult> BookAsync(SunHotelsBookRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "preBookCode", request.PreBookCode },
                { "guestName", request.GuestName },
                { "guestEmail", request.GuestEmail },
                { "guestPhone", request.GuestPhone ?? "" },
                { "specialRequests", request.SpecialRequests ?? "" }
            };

            var response = await SendNonStaticRequestAsync("book", parameters, cancellationToken);
            return ParseBookResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to complete booking", ex);
        }
    }

    public async Task<SunHotelsBookResultV3> BookV3Async(SunHotelsBookRequestV3 request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "preBookCode", request.PreBookCode },
                { "roomId", request.RoomId.ToString() },
                { "mealId", request.MealId.ToString() },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "rooms", request.Rooms.ToString() },
                { "adults", request.Adults.ToString() },
                { "children", request.Children.ToString() },
                { "infant", request.Infant.ToString() },
                { "currency", request.Currency },
                { "language", request.Language },
                { "email", request.Email },
                { "b2c", request.B2C ? "1" : "0" },
                { "paymentMethodId", request.PaymentMethodId.ToString() }
            };

            if (!string.IsNullOrEmpty(request.YourRef))
                parameters.Add("yourRef", request.YourRef);
            if (!string.IsNullOrEmpty(request.SpecialRequest))
                parameters.Add("specialRequest", request.SpecialRequest);
            if (!string.IsNullOrEmpty(request.CustomerCountry))
                parameters.Add("customerCountry", request.CustomerCountry);
            if (!string.IsNullOrEmpty(request.InvoiceRef))
                parameters.Add("invoiceRef", request.InvoiceRef);
            if (request.CommissionAmount.HasValue)
                parameters.Add("commissionAmount", request.CommissionAmount.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(request.CustomerEmail))
                parameters.Add("customerEmail", request.CustomerEmail);

            // Add adult guests
            for (int i = 0; i < request.AdultGuests.Count && i < 9; i++)
            {
                var guest = request.AdultGuests[i];
                parameters.Add($"adultGuest{i + 1}FirstName", guest.FirstName);
                parameters.Add($"adultGuest{i + 1}LastName", guest.LastName);
            }

            // Add child guests
            for (int i = 0; i < request.ChildrenGuests.Count && i < 9; i++)
            {
                var child = request.ChildrenGuests[i];
                parameters.Add($"childGuest{i + 1}FirstName", child.FirstName);
                parameters.Add($"childGuest{i + 1}LastName", child.LastName);
                parameters.Add($"childGuest{i + 1}Age", child.Age.ToString());
            }

            // Add credit card if provided
            if (request.CreditCard != null)
            {
                parameters.Add("creditCardType", request.CreditCard.CardType);
                parameters.Add("creditCardNumber", request.CreditCard.CardNumber);
                parameters.Add("creditCardHolder", request.CreditCard.CardHolder);
                parameters.Add("creditCardCVV2", request.CreditCard.CVV);
                parameters.Add("creditCardExpYear", request.CreditCard.ExpYear);
                parameters.Add("creditCardExpMonth", request.CreditCard.ExpMonth);
            }

            var response = await SendNonStaticRequestAsync("bookV3", parameters, cancellationToken);
            return ParseBookResultV3(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking V3 from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to complete booking V3", ex);
        }
    }

    public async Task<SunHotelsCancelResult> CancelBookingAsync(string bookingId, string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", bookingId },
                { "language", language }
            };

            var response = await SendNonStaticRequestAsync("cancelBooking", parameters, cancellationToken);
            return ParseCancelResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking from SunHotels for booking {BookingId}", bookingId);
            throw new ExternalServiceException("SunHotels", $"Failed to cancel booking {bookingId}", ex);
        }
    }

    public async Task<List<SunHotelsBookingInfo>> GetBookingInformationAsync(SunHotelsGetBookingRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", request.Language },
                { "showGuests", request.ShowGuests ? "1" : "0" }
            };

            if (!string.IsNullOrEmpty(request.BookingId))
                parameters.Add("bookingnumber", request.BookingId);
            if (!string.IsNullOrEmpty(request.Reference))
                parameters.Add("yourRef", request.Reference);
            if (request.CreatedDateFrom.HasValue)
                parameters.Add("createdDateFrom", request.CreatedDateFrom.Value.ToString("yyyy-MM-dd"));
            if (request.CreatedDateTo.HasValue)
                parameters.Add("createdDateTo", request.CreatedDateTo.Value.ToString("yyyy-MM-dd"));
            if (request.ArrivalDateFrom.HasValue)
                parameters.Add("arrivalDateFrom", request.ArrivalDateFrom.Value.ToString("yyyy-MM-dd"));
            if (request.ArrivalDateTo.HasValue)
                parameters.Add("arrivalDateTo", request.ArrivalDateTo.Value.ToString("yyyy-MM-dd"));

            var response = await SendNonStaticRequestAsync("getBookingInformationV3", parameters, cancellationToken);
            return ParseBookingInformation(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking information from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get booking information", ex);
        }
    }

    #endregion

    #region Amendment Methods

    public async Task<SunHotelsAmendmentPriceResult> GetAmendmentPriceAsync(SunHotelsAmendmentPriceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", request.BookingId },
                { "newCheckInDate", request.NewCheckIn.ToString("yyyy-MM-dd") },
                { "newCheckOutDate", request.NewCheckOut.ToString("yyyy-MM-dd") },
                { "language", request.Language }
            };

            var response = await SendNonStaticRequestAsync("amendmentPriceRequest", parameters, cancellationToken);
            return ParseAmendmentPriceResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting amendment price from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get amendment price", ex);
        }
    }

    public async Task<SunHotelsAmendmentResult> AmendBookingAsync(SunHotelsAmendmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", request.BookingId },
                { "newCheckInDate", request.NewCheckIn.ToString("yyyy-MM-dd") },
                { "newCheckOutDate", request.NewCheckOut.ToString("yyyy-MM-dd") },
                { "maxPrice", request.MaxPrice.ToString(CultureInfo.InvariantCulture) },
                { "language", request.Language },
                { "bookingType", request.BookingType }
            };

            if (request.NewRoomId.HasValue)
                parameters.Add("newRoomId", request.NewRoomId.Value.ToString());

            var response = await SendNonStaticRequestAsync("amendmentRequest", parameters, cancellationToken);
            return ParseAmendmentResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error amending booking from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to amend booking", ex);
        }
    }

    #endregion

    #region Special Request Methods

    public async Task<SunHotelsSpecialRequestResult> UpdateSpecialRequestAsync(string bookingId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", bookingId },
                { "specialRequest", text }
            };

            var response = await SendNonStaticRequestAsync("updateSpecialRequest", parameters, cancellationToken);
            return ParseSpecialRequestResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating special request from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to update special request", ex);
        }
    }

    public async Task<SunHotelsSpecialRequestResult> GetSpecialRequestAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", bookingId }
            };

            var response = await SendNonStaticRequestAsync("getSpecialRequest", parameters, cancellationToken);
            return ParseSpecialRequestResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting special request from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get special request", ex);
        }
    }

    #endregion

    #region Transfer Methods

    public async Task<List<SunHotelsTransferSearchResult>> SearchTransfersAsync(SunHotelsTransferSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "arrivalDate", request.ArrivalDate.ToString("yyyy-MM-dd") },
                { "arrivalTime", request.ArrivalTime },
                { "currency", request.Currency },
                { "language", request.Language }
            };

            if (request.HotelId.HasValue)
                parameters.Add("hotelId", request.HotelId.Value.ToString());
            if (request.RoomId.HasValue)
                parameters.Add("roomId", request.RoomId.Value.ToString());
            if (!string.IsNullOrEmpty(request.BookingId))
                parameters.Add("bookingnumber", request.BookingId);
            if (request.ResortId.HasValue)
                parameters.Add("resortId", request.ResortId.Value.ToString());
            if (request.TransferId.HasValue)
                parameters.Add("transferId", request.TransferId.Value.ToString());
            if (!string.IsNullOrEmpty(request.GiataCode))
                parameters.Add("giataCode", request.GiataCode);
            if (request.ReturnDepartureDate.HasValue)
                parameters.Add("returnDepartureDate", request.ReturnDepartureDate.Value.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrEmpty(request.ReturnDepartureTime))
                parameters.Add("returnDepartureTime", request.ReturnDepartureTime);

            var response = await SendNonStaticRequestAsync("searchTransfersV2", parameters, cancellationToken);
            return ParseTransferSearchResults(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transfers from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to search transfers", ex);
        }
    }

    public async Task<SunHotelsAddTransferResult> AddTransferAsync(SunHotelsAddTransferRequestV2 request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", request.BookingId },
                { "hotelName", request.HotelName },
                { "contactPerson", request.ContactPerson },
                { "contactCellphone", request.ContactCellphone },
                { "airlineCruiseline", request.AirlineCruiseline },
                { "flightNumber", request.FlightNumber },
                { "departureTime", request.DepartureTime },
                { "arrivalTime", request.ArrivalTime },
                { "arrivalDate", request.ArrivalDate.ToString("yyyy-MM-dd") },
                { "passengers", request.Passengers.ToString() },
                { "transferId", request.TransferId.ToString() },
                { "returnTransfer", request.ReturnTransfer ? "1" : "0" },
                { "currency", request.Currency },
                { "language", request.Language },
                { "email", request.Email }
            };

            if (request.HotelId.HasValue)
                parameters.Add("hotelId", request.HotelId.Value.ToString());
            if (!string.IsNullOrEmpty(request.HotelGiataCode))
                parameters.Add("hotelGiataCode", request.HotelGiataCode);
            if (!string.IsNullOrEmpty(request.HotelAddress))
                parameters.Add("hotelAddress", request.HotelAddress);
            if (!string.IsNullOrEmpty(request.OriginTerminal))
                parameters.Add("originTerminal", request.OriginTerminal);
            if (!string.IsNullOrEmpty(request.DepartureIataCode))
                parameters.Add("departureIataCode", request.DepartureIataCode);
            if (!string.IsNullOrEmpty(request.InvoiceRef))
                parameters.Add("invoiceRef", request.InvoiceRef);
            if (!string.IsNullOrEmpty(request.YourRef))
                parameters.Add("yourRef", request.YourRef);

            if (request.ReturnTransfer)
            {
                if (!string.IsNullOrEmpty(request.ReturnAirlineCruiseline))
                    parameters.Add("returnAirlineCruiseline", request.ReturnAirlineCruiseline);
                if (!string.IsNullOrEmpty(request.ReturnFlightNumber))
                    parameters.Add("returnFlightNumber", request.ReturnFlightNumber);
                if (request.ReturnDepartureDate.HasValue)
                    parameters.Add("returnDepartureDate", request.ReturnDepartureDate.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(request.ReturnDepartureTime))
                    parameters.Add("returnDepartureTime", request.ReturnDepartureTime);
            }

            var response = await SendNonStaticRequestAsync("addTransferV2", parameters, cancellationToken);
            return ParseAddTransferResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transfer from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to add transfer", ex);
        }
    }

    public async Task<SunHotelsCancelTransferResult> CancelTransferAsync(string transferBookingId, string email, string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "transferBookingId", transferBookingId },
                { "email", email },
                { "language", language }
            };

            var response = await SendNonStaticRequestAsync("cancelTransferBooking", parameters, cancellationToken);
            return ParseCancelTransferResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to cancel transfer", ex);
        }
    }

    public async Task<List<SunHotelsBookingInfo>> GetTransferBookingInformationAsync(
        string? bookingId = null,
        DateTime? createdDateFrom = null,
        DateTime? createdDateTo = null,
        DateTime? arrivalDateFrom = null,
        DateTime? arrivalDateTo = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            if (!string.IsNullOrEmpty(bookingId))
                parameters.Add("transferBookingId", bookingId);
            if (createdDateFrom.HasValue)
                parameters.Add("createdDateFrom", createdDateFrom.Value.ToString("yyyy-MM-dd"));
            if (createdDateTo.HasValue)
                parameters.Add("createdDateTo", createdDateTo.Value.ToString("yyyy-MM-dd"));
            if (arrivalDateFrom.HasValue)
                parameters.Add("arrivalDateFrom", arrivalDateFrom.Value.ToString("yyyy-MM-dd"));
            if (arrivalDateTo.HasValue)
                parameters.Add("arrivalDateTo", arrivalDateTo.Value.ToString("yyyy-MM-dd"));

            var response = await SendNonStaticRequestAsync("getTransferBookingInformation", parameters, cancellationToken);
            return ParseBookingInformation(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer booking information from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get transfer booking information", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private string BuildRequestUrl(string baseUrl, string method, Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder($"{baseUrl}/{method}?");
        sb.Append($"userName={_config.Username}");
        sb.Append($"&password={_config.Password}");

        foreach (var param in parameters)
        {
            sb.Append($"&{param.Key}={param.Value}");
        }

        return sb.ToString();
    }

    private async Task<string> SendStaticRequestAsync(string method, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var url = BuildRequestUrl(StaticApiUrl, method, parameters);
        _logger.LogInformation("Sending static request to SunHotels: {Method} - URL: {Url}", method, url);

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SunHotels API error - Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<string> SendNonStaticRequestAsync(string method, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var url = BuildRequestUrl(NonStaticApiUrl, method, parameters);
        _logger.LogDebug("Sending non-static request to SunHotels: {Method}", method);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    #endregion

    #region Parse Static Data Methods

    private List<SunHotelsDestination> ParseDestinations(string xmlResponse)
    {
        var destinations = new List<SunHotelsDestination>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var destinationElements = doc.Descendants(ns + "Destination");

            foreach (var elem in destinationElements)
            {
                destinations.Add(new SunHotelsDestination
                {
                    Id = elem.Element(ns + "destination_id")?.Value ?? "",
                    Code = elem.Element(ns + "DestinationCode")?.Value ?? "",
                    Name = elem.Element(ns + "DestinationName")?.Value ?? "",
                    Country = elem.Element(ns + "CountryName")?.Value ?? "",
                    CountryCode = elem.Element(ns + "CountryCode")?.Value ?? "",
                    CountryId = elem.Element(ns + "CountryId")?.Value ?? "",
                    TimeZone = elem.Element(ns + "TimeZone")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} destinations from XML", destinations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing destinations XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return destinations;
    }

    private List<SunHotelsResort> ParseResorts(string xmlResponse)
    {
        var resorts = new List<SunHotelsResort>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var resortElements = doc.Descendants(ns + "Resort");

            foreach (var elem in resortElements)
            {
                resorts.Add(new SunHotelsResort
                {
                    Id = int.TryParse(elem.Element(ns + "ResortId")?.Value, out var id) ? id : 0,
                    Name = elem.Element(ns + "ResortName")?.Value ?? "",
                    DestinationId = elem.Element(ns + "destination_id")?.Value ?? "",
                    DestinationName = elem.Element(ns + "DestinationName")?.Value ?? "",
                    CountryName = elem.Element(ns + "CountryName")?.Value ?? "",
                    CountryCode = elem.Element(ns + "CountryCode")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} resorts from XML", resorts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing resorts XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return resorts;
    }

    private List<SunHotelsMeal> ParseMeals(string xmlResponse)
    {
        var meals = new List<SunHotelsMeal>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var mealElements = doc.Descendants(ns + "meal");

            foreach (var elem in mealElements)
            {
                var id = int.TryParse(elem.Element(ns + "id")?.Value, out var mealId) ? mealId : 0;
                var name = elem.Element(ns + "name")?.Value ?? "";

                // labels içindeki tüm label'ları topla
                var labels = elem.Descendants(ns + "label")
                    .Select(l => l.Element(ns + "text")?.Value)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                meals.Add(new SunHotelsMeal
                {
                    Id = id,
                    Name = name,
                    Labels = labels!
                });
            }

            _logger.LogInformation("Parsed {Count} meals from XML", meals.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing meals XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return meals;
    }
    private List<SunHotelsRoomType> ParseRoomTypes(string xmlResponse)
    {
        var roomTypes = new List<SunHotelsRoomType>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var roomTypeElements = doc.Descendants(ns + "roomType");

            foreach (var elem in roomTypeElements)
            {
                var id = int.TryParse(elem.Element(ns + "id")?.Value, out var roomTypeId) ? roomTypeId : 0;
                var name = elem.Element(ns + "name")?.Value ?? "";

                roomTypes.Add(new SunHotelsRoomType
                {
                    Id = id,
                    Name = name
                });
            }

            _logger.LogInformation("Parsed {Count} room types from XML", roomTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing room types XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return roomTypes;
    }

    private List<SunHotelsFeature> ParseFeatures(string xmlResponse)
    {
        var features = new List<SunHotelsFeature>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var featureElements = doc.Descendants(ns + "feature");

            foreach (var elem in featureElements)
            {
                var id = int.TryParse(elem.Attribute("id")?.Value, out var featureId) ? featureId : 0;
                var name = elem.Attribute("name")?.Value ?? "";

                features.Add(new SunHotelsFeature
                {
                    Id = id,
                    Name = name
                });
            }

            _logger.LogInformation("Parsed {Count} features from XML", features.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing features XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return features;
    }

    private List<SunHotelsLanguage> ParseLanguages(string xmlResponse)
    {
        var languages = new List<SunHotelsLanguage>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var languageElements = doc.Descendants(ns + "language");

            foreach (var elem in languageElements)
            {
                languages.Add(new SunHotelsLanguage
                {
                    Code = elem.Element(ns + "isoCode")?.Value ?? "",
                    Name = elem.Element(ns + "name")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} languages from XML", languages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing languages XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return languages;
    }

    private List<SunHotelsTheme> ParseThemes(string xmlResponse)
    {
        var themes = new List<SunHotelsTheme>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var themeElements = doc.Descendants(ns + "theme");

            foreach (var elem in themeElements)
            {
                themes.Add(new SunHotelsTheme
                {
                    Id = int.TryParse(elem.Attribute("id")?.Value, out var id) ? id : 0,
                    Name = elem.Attribute("name")?.Value ?? "",
                    EnglishName = elem.Attribute("name")?.Value ?? "" // Themes sadece name attribute'ı var
                });
            }

            _logger.LogInformation("Parsed {Count} themes from XML", themes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing themes XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return themes;
    }

    private List<SunHotelsTransferType> ParseTransferTypes(string xmlResponse)
    {
        var transferTypes = new List<SunHotelsTransferType>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var transferTypeElements = doc.Descendants(ns + "transferType");

            foreach (var elem in transferTypeElements)
            {
                transferTypes.Add(new SunHotelsTransferType
                {
                    Id = int.TryParse(elem.Element(ns + "id")?.Value, out var id) ? id : 0,
                    Name = elem.Element(ns + "name")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} transfer types from XML", transferTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing transfer types XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return transferTypes;
    }

    private List<SunHotelsNoteType> ParseNoteTypes(string xmlResponse)
    {
        var noteTypes = new List<SunHotelsNoteType>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var noteTypeElements = doc.Descendants(ns + "noteType");

            foreach (var elem in noteTypeElements)
            {
                noteTypes.Add(new SunHotelsNoteType
                {
                    Id = int.TryParse(elem.Attribute("id")?.Value, out var id) ? id : 0,
                    Name = elem.Attribute("text")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} note types from XML", noteTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing note types XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return noteTypes;
    }

    private List<SunHotelsStaticHotel> ParseStaticHotels(string xmlResponse)
    {
        var hotels = new List<SunHotelsStaticHotel>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";

            // Try with namespace first, fall back to no namespace
            var hotelElements = doc.Descendants(ns + "hotel");
            if (!hotelElements.Any())
            {
                hotelElements = doc.Descendants("hotel");
            }

            _logger.LogInformation("Found {Count} hotel elements in XML response", hotelElements.Count());

            foreach (var elem in hotelElements)
            {
                // Helper function to get element value with different possible names
                string GetElementValue(params string[] names)
                {
                    foreach (var name in names)
                    {
                        var value = elem.Element(ns + name)?.Value ?? elem.Element(name)?.Value;
                        if (!string.IsNullOrEmpty(value))
                            return value;
                    }
                    return "";
                }

                var hotel = new SunHotelsStaticHotel
                {
                    HotelId = int.TryParse(GetElementValue("hotel.id", "hotelId"), out var id) ? id : 0,
                    Name = GetElementValue("name", "hotelName"),
                    Description = GetElementValue("description"),
                    Address = GetElementValue("hotel.address", "address"),
                    ZipCode = GetElementValue("hotel.addr.zip", "zipCode"),
                    City = GetElementValue("hotel.addr.city", "city"),
                    Country = GetElementValue("hotel.addr.country", "countryName"),
                    CountryCode = GetElementValue("hotel.addr.countrycode", "countryCode"),
                    Category = int.TryParse(GetElementValue("classification", "category"), out var cat) ? cat : 0,
                    ResortId = int.TryParse(GetElementValue("resort_id", "resortId"), out var resortId) ? resortId : 0,
                    ResortName = GetElementValue("resort", "resortName"),
                    Phone = GetElementValue("phone"),
                    Fax = GetElementValue("fax"),
                    Email = GetElementValue("email"),
                    Website = GetElementValue("website")
                };

                // Parse coordinates
                var coordsElem = elem.Element(ns + "coordinates") ?? elem.Element("coordinates");
                if (coordsElem != null)
                {
                    if (double.TryParse(coordsElem.Element(ns + "latitude")?.Value ?? coordsElem.Element("latitude")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                        hotel.Latitude = lat;
                    if (double.TryParse(coordsElem.Element(ns + "longitude")?.Value ?? coordsElem.Element("longitude")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                        hotel.Longitude = lng;
                }

                // Parse GIATA code from codes section
                var codesElem = elem.Element(ns + "codes") ?? elem.Element("codes");
                if (codesElem != null)
                {
                    var giataElem = codesElem.Elements().FirstOrDefault(e =>
                        e.Name.LocalName.ToLower().Contains("giata"));
                    if (giataElem != null)
                        hotel.GiataCode = giataElem.Value;
                }

                // Parse images
                int order = 0;
                var imagesContainer = elem.Element(ns + "images") ?? elem.Element("images");
                if (imagesContainer != null)
                {
                    foreach (var imgElem in imagesContainer.Elements())
                    {
                        if (imgElem.Name.LocalName == "image")
                        {
                            var fullSizeElem = imgElem.Element(ns + "fullSizeImage") ?? imgElem.Element("fullSizeImage");
                            var smallImageElem = imgElem.Element(ns + "smallImage") ?? imgElem.Element("smallImage");

                            var imageUrl = fullSizeElem?.Attribute("url")?.Value
                                        ?? smallImageElem?.Attribute("url")?.Value
                                        ?? imgElem.Element(ns + "url")?.Value
                                        ?? imgElem.Element("url")?.Value
                                        ?? imgElem.Value;

                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                // If URL is relative (starts with ?), construct full URL
                                if (imageUrl.StartsWith("?"))
                                {
                                    imageUrl = $"http://xml.sunhotels.net/15/GetImage.aspx{imageUrl}";
                                }

                                hotel.Images.Add(new SunHotelsImage
                                {
                                    Url = imageUrl,
                                    Order = order++
                                });
                            }
                        }
                    }
                }

                // Parse features
                var featuresContainer = elem.Element(ns + "features") ?? elem.Element("features");
                if (featuresContainer != null)
                {
                    foreach (var featureElem in featuresContainer.Elements())
                    {
                        if (featureElem.Name.LocalName == "feature")
                        {
                            if (int.TryParse(featureElem.Attribute("id")?.Value, out var featureId))
                                hotel.FeatureIds.Add(featureId);
                        }
                    }
                }

                // Parse themes
                var themesContainer = elem.Element(ns + "themes") ?? elem.Element("themes");
                if (themesContainer != null)
                {
                    foreach (var themeElem in themesContainer.Elements())
                    {
                        if (themeElem.Name.LocalName == "theme")
                        {
                            if (int.TryParse(themeElem.Attribute("id")?.Value, out var themeId))
                                hotel.ThemeIds.Add(themeId);
                        }
                    }
                }

                // Parse rooms - structure is: roomtypes > roomtype > rooms > room
                var roomtypesContainer = elem.Element(ns + "roomtypes") ?? elem.Element("roomtypes");
                if (roomtypesContainer != null)
                {
                    foreach (var roomtypeElem in roomtypesContainer.Elements())
                    {
                        if (roomtypeElem.Name.LocalName == "roomtype")
                        {
                            var roomTypeName = roomtypeElem.Element(ns + "room.type")?.Value
                                            ?? roomtypeElem.Element("room.type")?.Value ?? "";
                            var roomTypeId = int.TryParse(
                                roomtypeElem.Element(ns + "roomtype.ID")?.Value
                                ?? roomtypeElem.Element("roomtype.ID")?.Value, out var rtId) ? rtId : 0;

                            var roomsContainer = roomtypeElem.Element(ns + "rooms") ?? roomtypeElem.Element("rooms");
                            if (roomsContainer != null)
                            {
                                foreach (var roomElem in roomsContainer.Elements())
                                {
                                    if (roomElem.Name.LocalName == "room")
                                    {
                                        var room = new SunHotelsStaticRoom
                                        {
                                            RoomTypeId = roomTypeId,
                                            Name = roomTypeName,
                                            EnglishName = roomTypeName,
                                            Description = roomtypeElem.Element(ns + "description")?.Value
                                                       ?? roomtypeElem.Element("description")?.Value,
                                            MaxOccupancy = int.TryParse(
                                                roomElem.Element(ns + "beds")?.Value
                                                ?? roomElem.Element("beds")?.Value, out var beds) ? beds : 0,
                                            MinOccupancy = 1
                                        };

                                        // Parse room features
                                        var roomFeaturesContainer = roomElem.Element(ns + "features") ?? roomElem.Element("features");
                                        if (roomFeaturesContainer != null)
                                        {
                                            foreach (var roomFeatureElem in roomFeaturesContainer.Elements())
                                            {
                                                if (roomFeatureElem.Name.LocalName == "feature")
                                                {
                                                    if (int.TryParse(roomFeatureElem.Attribute("id")?.Value, out var featureId))
                                                        room.FeatureIds.Add(featureId);
                                                }
                                            }
                                        }

                                        hotel.Rooms.Add(room);
                                    }
                                }
                            }
                        }
                    }
                }

                hotels.Add(hotel);
            }

            _logger.LogInformation("Parsed {Count} static hotels from XML with total {RoomCount} rooms",
                hotels.Count, hotels.Sum(h => h.Rooms.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing static hotels XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return hotels;
    }

    #endregion
    #region Parse Search Results Methods

    private List<SunHotelsSearchResult> ParseSearchResults(string xmlResponse)
    {
        var results = new List<SunHotelsSearchResult>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var hotelElements = doc.Descendants("hotel");

            foreach (var elem in hotelElements)
            {
                var hotel = new SunHotelsSearchResult
                {
                    HotelId = elem.Element("hotelId")?.Value ?? "",
                    Name = elem.Element("hotelName")?.Value ?? "",
                    Description = elem.Element("description")?.Value ?? "",
                    Address = elem.Element("address")?.Value ?? "",
                    City = elem.Element("city")?.Value ?? "",
                    Country = elem.Element("countryName")?.Value ?? "",
                    Category = int.TryParse(elem.Element("category")?.Value, out var cat) ? cat : 0,
                    Latitude = double.TryParse(elem.Element("latitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ? lat : 0,
                    Longitude = double.TryParse(elem.Element("longitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng) ? lng : 0,
                    MinPrice = decimal.TryParse(elem.Element("minPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR"
                };

                // Parse images
                int order = 0;
                foreach (var imgElem in elem.Descendants("image"))
                {
                    hotel.Images.Add(new SunHotelsImage
                    {
                        Url = imgElem.Element("url")?.Value ?? imgElem.Value,
                        Order = order++
                    });
                }

                results.Add(hotel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing search results XML");
        }

        return results;
    }

    private List<SunHotelsSearchResultV3> ParseSearchResultsV3(string xmlResponse)
    {
        var results = new List<SunHotelsSearchResultV3>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var hotelElements = doc.Descendants("hotel");

            foreach (var elem in hotelElements)
            {
                var hotel = new SunHotelsSearchResultV3
                {
                    HotelId = int.TryParse(elem.Element("hotelId")?.Value, out var id) ? id : 0,
                    Name = elem.Element("hotelName")?.Value ?? "",
                    Description = elem.Element("description")?.Value ?? "",
                    Address = elem.Element("address")?.Value ?? "",
                    City = elem.Element("city")?.Value ?? "",
                    Country = elem.Element("countryName")?.Value ?? "",
                    CountryCode = elem.Element("countryCode")?.Value ?? "",
                    Category = int.TryParse(elem.Element("category")?.Value, out var cat) ? cat : 0,
                    Latitude = double.TryParse(elem.Element("latitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ? lat : 0,
                    Longitude = double.TryParse(elem.Element("longitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng) ? lng : 0,
                    GiataCode = elem.Element("giataCode")?.Value,
                    ResortId = int.TryParse(elem.Element("resortId")?.Value, out var resortId) ? resortId : 0,
                    ResortName = elem.Element("resortName")?.Value ?? "",
                    MinPrice = decimal.TryParse(elem.Element("minPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR",
                    ReviewScore = double.TryParse(elem.Element("reviewScore")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var score) ? score : null,
                    ReviewCount = int.TryParse(elem.Element("reviewCount")?.Value, out var count) ? count : null
                };

                // Parse images
                int order = 0;
                foreach (var imgElem in elem.Descendants("image"))
                {
                    hotel.Images.Add(new SunHotelsImage
                    {
                        Url = imgElem.Element("url")?.Value ?? imgElem.Value,
                        Order = order++
                    });
                }

                // Parse rooms
                foreach (var roomElem in elem.Descendants("room"))
                {
                    var room = new SunHotelsRoomV3
                    {
                        RoomId = int.TryParse(roomElem.Element("roomId")?.Value, out var roomId) ? roomId : 0,
                        RoomTypeId = int.TryParse(roomElem.Element("roomTypeId")?.Value, out var roomTypeId) ? roomTypeId : 0,
                        RoomTypeName = roomElem.Element("roomTypeName")?.Value ?? "",
                        Name = roomElem.Element("roomName")?.Value ?? "",
                        Description = roomElem.Element("description")?.Value ?? "",
                        MealId = int.TryParse(roomElem.Element("mealId")?.Value, out var mealId) ? mealId : 0,
                        MealName = roomElem.Element("mealName")?.Value ?? "",
                        Price = decimal.TryParse(roomElem.Element("price")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var roomPrice) ? roomPrice : 0,
                        Currency = roomElem.Element("currency")?.Value ?? "EUR",
                        IsRefundable = roomElem.Element("nonRefundable")?.Value?.ToLower() != "true",
                        IsSuperDeal = roomElem.Element("isSuperDeal")?.Value?.ToLower() == "true",
                        AvailableRooms = int.TryParse(roomElem.Element("availableRooms")?.Value, out var avail) ? avail : 0
                    };

                    if (decimal.TryParse(roomElem.Element("originalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var origPrice))
                        room.OriginalPrice = origPrice;

                    if (DateTime.TryParse(roomElem.Element("earliestNonFreeCancellationDate")?.Value, out var cancelDate))
                        room.EarliestNonFreeCancellationDate = cancelDate;

                    // Parse cancellation policies
                    foreach (var policyElem in roomElem.Descendants("cancellationPolicy"))
                    {
                        room.CancellationPolicies.Add(ParseCancellationPolicy(policyElem));
                    }

                    hotel.Rooms.Add(room);
                }

                // Parse feature IDs
                foreach (var featureElem in elem.Descendants("featureId"))
                {
                    if (int.TryParse(featureElem.Value, out var featureId))
                        hotel.FeatureIds.Add(featureId);
                }

                // Parse theme IDs
                foreach (var themeElem in elem.Descendants("themeId"))
                {
                    if (int.TryParse(themeElem.Value, out var themeId))
                        hotel.ThemeIds.Add(themeId);
                }

                results.Add(hotel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing search results V3 XML");
        }

        return results;
    }

    private SunHotelsCancellationPolicy ParseCancellationPolicy(XElement elem)
    {
        return new SunHotelsCancellationPolicy
        {
            FromDate = DateTime.TryParse(elem.Element("fromDate")?.Value, out var fromDate) ? fromDate : DateTime.MinValue,
            Percentage = decimal.TryParse(elem.Element("percentage")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct) ? pct : 0,
            FixedAmount = decimal.TryParse(elem.Element("fixedAmount")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) ? amt : null,
            NightsCharged = int.TryParse(elem.Element("nightsCharged")?.Value, out var nights) ? nights : 0
        };
    }

    #endregion

    #region Parse PreBook/Book Results Methods

    private SunHotelsPreBookResult ParsePreBookResult(string xmlResponse)
    {
        var result = new SunHotelsPreBookResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var preBookElem = doc.Descendants("preBookResult").FirstOrDefault() ?? doc.Root;

            if (preBookElem != null)
            {
                result.PreBookCode = preBookElem.Element("preBookCode")?.Value ?? "";
                result.TotalPrice = decimal.TryParse(preBookElem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                result.Currency = preBookElem.Element("currency")?.Value ?? "EUR";
                result.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing pre-book result XML");
        }

        return result;
    }

    private SunHotelsPreBookResultV3 ParsePreBookResultV3(string xmlResponse)
    {
        var result = new SunHotelsPreBookResultV3();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var preBookElem = doc.Descendants("preBookResult").FirstOrDefault() ?? doc.Root;

            if (preBookElem != null)
            {
                result.PreBookCode = preBookElem.Element("preBookCode")?.Value ?? "";
                result.TotalPrice = decimal.TryParse(preBookElem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                result.NetPrice = decimal.TryParse(preBookElem.Element("netPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var netPrice) ? netPrice : 0;
                result.TaxAmount = decimal.TryParse(preBookElem.Element("tax")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var tax) ? tax : null;
                result.FeeAmount = decimal.TryParse(preBookElem.Element("fee")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var fee) ? fee : null;
                result.Currency = preBookElem.Element("currency")?.Value ?? "EUR";
                result.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
                result.PriceChanged = preBookElem.Element("priceChanged")?.Value?.ToLower() == "true";
                result.OriginalPrice = decimal.TryParse(preBookElem.Element("originalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var origPrice) ? origPrice : null;
                result.HotelNotes = preBookElem.Element("hotelNotes")?.Value;
                result.RoomNotes = preBookElem.Element("roomNotes")?.Value;

                if (DateTime.TryParse(preBookElem.Element("earliestNonFreeCancellationDateCET")?.Value, out var cancelDateCet))
                    result.EarliestNonFreeCancellationDateCET = cancelDateCet;
                if (DateTime.TryParse(preBookElem.Element("earliestNonFreeCancellationDateLocal")?.Value, out var cancelDateLocal))
                    result.EarliestNonFreeCancellationDateLocal = cancelDateLocal;

                // Parse price breakdown
                foreach (var priceElem in preBookElem.Descendants("priceBreakdown"))
                {
                    result.PriceBreakdown.Add(new SunHotelsPriceBreakdown
                    {
                        Date = DateTime.TryParse(priceElem.Element("date")?.Value, out var date) ? date : DateTime.MinValue,
                        Price = decimal.TryParse(priceElem.Element("price")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var bPrice) ? bPrice : 0,
                        Currency = priceElem.Element("currency")?.Value ?? "EUR"
                    });
                }

                // Parse cancellation policies
                foreach (var policyElem in preBookElem.Descendants("cancellationPolicy"))
                {
                    result.CancellationPolicies.Add(ParseCancellationPolicy(policyElem));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing pre-book V3 result XML");
        }

        return result;
    }

    private SunHotelsBookResult ParseBookResult(string xmlResponse)
    {
        var result = new SunHotelsBookResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var bookElem = doc.Descendants("bookResult").FirstOrDefault() ?? doc.Root;

            if (bookElem != null)
            {
                result.Success = bookElem.Element("status")?.Value?.ToLower() == "success"
                              || !string.IsNullOrEmpty(bookElem.Element("bookingnumber")?.Value);
                result.BookingId = bookElem.Element("bookingnumber")?.Value ?? "";
                result.ConfirmationNumber = bookElem.Element("confirmationNumber")?.Value ?? "";
                result.Message = bookElem.Element("message")?.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing book result XML");
        }

        return result;
    }

    private SunHotelsBookResultV3 ParseBookResultV3(string xmlResponse)
    {
        var result = new SunHotelsBookResultV3();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var bookElem = doc.Descendants("bookResult").FirstOrDefault() ?? doc.Root;

            if (bookElem != null)
            {
                result.BookingNumber = bookElem.Element("bookingnumber")?.Value;
                result.Success = !string.IsNullOrEmpty(result.BookingNumber);
                result.HotelId = int.TryParse(bookElem.Element("hotelId")?.Value, out var hotelId) ? hotelId : 0;
                result.HotelName = bookElem.Element("hotelName")?.Value ?? "";
                result.HotelAddress = bookElem.Element("hotelAddress")?.Value ?? "";
                result.HotelPhone = bookElem.Element("hotelPhone")?.Value;
                result.RoomType = bookElem.Element("roomType")?.Value ?? "";
                result.MealId = int.TryParse(bookElem.Element("mealId")?.Value, out var mealId) ? mealId : 0;
                result.MealName = bookElem.Element("mealName")?.Value ?? "";
                result.CheckIn = DateTime.TryParse(bookElem.Element("checkIn")?.Value, out var checkIn) ? checkIn : DateTime.MinValue;
                result.CheckOut = DateTime.TryParse(bookElem.Element("checkOut")?.Value, out var checkOut) ? checkOut : DateTime.MinValue;
                result.NumberOfRooms = int.TryParse(bookElem.Element("numberOfRooms")?.Value, out var rooms) ? rooms : 0;
                result.TotalPrice = decimal.TryParse(bookElem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                result.Currency = bookElem.Element("currency")?.Value ?? "EUR";
                result.BookingDate = DateTime.TryParse(bookElem.Element("bookingDate")?.Value, out var bookingDate) ? bookingDate : DateTime.UtcNow;
                result.Voucher = bookElem.Element("voucher")?.Value;
                result.YourRef = bookElem.Element("yourRef")?.Value;
                result.InvoiceRef = bookElem.Element("invoiceRef")?.Value;
                result.ErrorMessage = bookElem.Element("error")?.Value;

                if (DateTime.TryParse(bookElem.Element("earliestNonFreeCancellationDateCET")?.Value, out var cancelDateCet))
                    result.EarliestNonFreeCancellationDateCET = cancelDateCet;
                if (DateTime.TryParse(bookElem.Element("earliestNonFreeCancellationDateLocal")?.Value, out var cancelDateLocal))
                    result.EarliestNonFreeCancellationDateLocal = cancelDateLocal;

                // Parse cancellation policies
                foreach (var policyElem in bookElem.Descendants("cancellationPolicy"))
                {
                    result.CancellationPolicies.Add(ParseCancellationPolicy(policyElem));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing book V3 result XML");
        }

        return result;
    }

    private SunHotelsCancelResult ParseCancelResult(string xmlResponse)
    {
        var result = new SunHotelsCancelResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var cancelElem = doc.Descendants("cancelResult").FirstOrDefault() ?? doc.Root;

            if (cancelElem != null)
            {
                result.Success = cancelElem.Element("status")?.Value?.ToLower() == "success"
                              || cancelElem.Element("cancelled")?.Value?.ToLower() == "true";
                result.Message = cancelElem.Element("message")?.Value ?? "";
                if (decimal.TryParse(cancelElem.Element("refundAmount")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var refund))
                {
                    result.RefundAmount = refund;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cancel result XML");
        }

        return result;
    }

    #endregion

    #region Parse Other Results Methods

    private List<SunHotelsBookingInfo> ParseBookingInformation(string xmlResponse)
    {
        var bookings = new List<SunHotelsBookingInfo>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var bookingElements = doc.Descendants("booking");

            foreach (var elem in bookingElements)
            {
                var booking = new SunHotelsBookingInfo
                {
                    BookingNumber = elem.Element("bookingnumber")?.Value ?? "",
                    HotelId = int.TryParse(elem.Element("hotelId")?.Value, out var hotelId) ? hotelId : 0,
                    HotelName = elem.Element("hotelName")?.Value ?? "",
                    HotelAddress = elem.Element("hotelAddress")?.Value ?? "",
                    HotelPhone = elem.Element("hotelPhone")?.Value,
                    RoomType = elem.Element("roomType")?.Value ?? "",
                    NumberOfRooms = int.TryParse(elem.Element("numberOfRooms")?.Value, out var rooms) ? rooms : 0,
                    MealId = int.TryParse(elem.Element("mealId")?.Value, out var mealId) ? mealId : 0,
                    MealName = elem.Element("mealName")?.Value ?? "",
                    CheckIn = DateTime.TryParse(elem.Element("checkIn")?.Value, out var checkIn) ? checkIn : DateTime.MinValue,
                    CheckOut = DateTime.TryParse(elem.Element("checkOut")?.Value, out var checkOut) ? checkOut : DateTime.MinValue,
                    Status = elem.Element("status")?.Value ?? "",
                    TotalPrice = decimal.TryParse(elem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR",
                    BookingDate = DateTime.TryParse(elem.Element("bookingDate")?.Value, out var bookingDate) ? bookingDate : DateTime.MinValue,
                    YourRef = elem.Element("yourRef")?.Value,
                    InvoiceRef = elem.Element("invoiceRef")?.Value,
                    Voucher = elem.Element("voucher")?.Value,
                    HasTransfer = elem.Element("hasTransfer")?.Value?.ToLower() == "true"
                };

                if (DateTime.TryParse(elem.Element("earliestNonFreeCancellationDateCET")?.Value, out var cancelDate))
                    booking.EarliestNonFreeCancellationDateCET = cancelDate;

                // Parse guests
                foreach (var guestElem in elem.Descendants("adultGuest"))
                {
                    booking.AdultGuests.Add(new SunHotelsGuest
                    {
                        FirstName = guestElem.Element("firstName")?.Value ?? "",
                        LastName = guestElem.Element("lastName")?.Value ?? ""
                    });
                }

                foreach (var childElem in elem.Descendants("childGuest"))
                {
                    booking.ChildrenGuests.Add(new SunHotelsChildGuest
                    {
                        FirstName = childElem.Element("firstName")?.Value ?? "",
                        LastName = childElem.Element("lastName")?.Value ?? "",
                        Age = int.TryParse(childElem.Element("age")?.Value, out var age) ? age : 0
                    });
                }

                // Parse cancellation policies
                foreach (var policyElem in elem.Descendants("cancellationPolicy"))
                {
                    booking.CancellationPolicies.Add(ParseCancellationPolicy(policyElem));
                }

                bookings.Add(booking);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing booking information XML");
        }

        return bookings;
    }

    private SunHotelsAmendmentPriceResult ParseAmendmentPriceResult(string xmlResponse)
    {
        var result = new SunHotelsAmendmentPriceResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var amendElem = doc.Descendants("amendmentPriceResult").FirstOrDefault() ?? doc.Root;

            if (amendElem != null)
            {
                result.AmendmentPossible = amendElem.Element("amendmentPossible")?.Value?.ToLower() == "true";
                result.Success = result.AmendmentPossible || amendElem.Element("error") == null;
                result.CurrentPrice = decimal.TryParse(amendElem.Element("currentPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var current) ? current : null;
                result.NewPrice = decimal.TryParse(amendElem.Element("newPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var newP) ? newP : null;
                result.PriceDifference = decimal.TryParse(amendElem.Element("priceDifference")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var diff) ? diff : null;
                result.Currency = amendElem.Element("currency")?.Value ?? "EUR";
                result.Message = amendElem.Element("message")?.Value ?? amendElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing amendment price result XML");
        }

        return result;
    }

    private SunHotelsAmendmentResult ParseAmendmentResult(string xmlResponse)
    {
        var result = new SunHotelsAmendmentResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var amendElem = doc.Descendants("amendmentResult").FirstOrDefault() ?? doc.Root;

            if (amendElem != null)
            {
                result.NewBookingNumber = amendElem.Element("newBookingnumber")?.Value;
                result.Success = !string.IsNullOrEmpty(result.NewBookingNumber);
                result.NewTotalPrice = decimal.TryParse(amendElem.Element("newTotalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : null;
                result.Currency = amendElem.Element("currency")?.Value ?? "EUR";
                result.Message = amendElem.Element("message")?.Value ?? amendElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing amendment result XML");
        }

        return result;
    }

    private SunHotelsSpecialRequestResult ParseSpecialRequestResult(string xmlResponse)
    {
        var result = new SunHotelsSpecialRequestResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var specialElem = doc.Descendants("specialRequestResult").FirstOrDefault() ?? doc.Root;

            if (specialElem != null)
            {
                result.Success = specialElem.Element("status")?.Value?.ToLower() == "success"
                              || specialElem.Element("error") == null;
                result.CurrentText = specialElem.Element("specialRequest")?.Value ?? specialElem.Element("text")?.Value;
                result.Message = specialElem.Element("message")?.Value ?? specialElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing special request result XML");
        }

        return result;
    }

    private List<SunHotelsTransferSearchResult> ParseTransferSearchResults(string xmlResponse)
    {
        var results = new List<SunHotelsTransferSearchResult>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var transferElements = doc.Descendants("transfer");

            foreach (var elem in transferElements)
            {
                results.Add(new SunHotelsTransferSearchResult
                {
                    TransferId = int.TryParse(elem.Element("transferId")?.Value, out var id) ? id : 0,
                    TransferTypeId = int.TryParse(elem.Element("transferTypeId")?.Value, out var typeId) ? typeId : 0,
                    TransferTypeName = elem.Element("transferTypeName")?.Value ?? "",
                    PickupLocation = elem.Element("pickupLocation")?.Value ?? "",
                    DropoffLocation = elem.Element("dropoffLocation")?.Value ?? "",
                    MaxPassengers = int.TryParse(elem.Element("maxPassengers")?.Value, out var max) ? max : 0,
                    Price = decimal.TryParse(elem.Element("price")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR",
                    IncludesReturn = elem.Element("includesReturn")?.Value?.ToLower() == "true",
                    VehicleType = elem.Element("vehicleType")?.Value,
                    Description = elem.Element("description")?.Value
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing transfer search results XML");
        }

        return results;
    }

    private SunHotelsAddTransferResult ParseAddTransferResult(string xmlResponse)
    {
        var result = new SunHotelsAddTransferResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var transferElem = doc.Descendants("addTransferResult").FirstOrDefault() ?? doc.Root;

            if (transferElem != null)
            {
                result.TransferBookingId = transferElem.Element("transferBookingId")?.Value;
                result.Success = !string.IsNullOrEmpty(result.TransferBookingId);
                result.ConfirmationNumber = transferElem.Element("confirmationNumber")?.Value;
                result.TotalPrice = decimal.TryParse(transferElem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                result.Currency = transferElem.Element("currency")?.Value ?? "EUR";
                result.ErrorMessage = transferElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing add transfer result XML");
        }

        return result;
    }

    private SunHotelsCancelTransferResult ParseCancelTransferResult(string xmlResponse)
    {
        var result = new SunHotelsCancelTransferResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var cancelElem = doc.Descendants("cancelTransferResult").FirstOrDefault() ?? doc.Root;

            if (cancelElem != null)
            {
                result.Success = cancelElem.Element("status")?.Value?.ToLower() == "success"
                              || cancelElem.Element("cancelled")?.Value?.ToLower() == "true";
                result.Message = cancelElem.Element("message")?.Value ?? "";
                if (decimal.TryParse(cancelElem.Element("refundAmount")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var refund))
                    result.RefundAmount = refund;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cancel transfer result XML");
        }

        return result;
    }

    #endregion
}
