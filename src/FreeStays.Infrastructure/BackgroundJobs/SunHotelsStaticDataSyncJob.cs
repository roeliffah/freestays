using FreeStays.Domain.Entities.Cache;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreeStays.Infrastructure.BackgroundJobs;

/// <summary>
/// SunHotels Static Data Senkronizasyon Job'ı
/// Günlük çalışarak statik verileri API'den çekip veritabanına kaydeder
/// </summary>
public class SunHotelsStaticDataSyncJob
{
    private readonly FreeStaysDbContext _dbContext;
    private readonly ISunHotelsService _sunHotelsService;
    private readonly ILogger<SunHotelsStaticDataSyncJob> _logger;

    public SunHotelsStaticDataSyncJob(
        FreeStaysDbContext dbContext,
        ISunHotelsService sunHotelsService,
        ILogger<SunHotelsStaticDataSyncJob> logger)
    {
        _dbContext = dbContext;
        _sunHotelsService = sunHotelsService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm statik verileri senkronize eder
    /// </summary>
    public async Task SyncAllStaticDataAsync()
    {
        _logger.LogInformation("Starting SunHotels static data synchronization...");

        try
        {
            // Önce dilleri ve dil-bağımsız verileri senkronize et
            await SyncLanguagesAsync();
            await SyncThemesAsync();

            // Desteklenen dilleri API'den çek
            var supportedLanguages = await GetSupportedLanguagesAsync();
            _logger.LogInformation("Found {Count} supported languages: {Languages}",
                supportedLanguages.Count, string.Join(", ", supportedLanguages));

            // Dil bazlı veriler - sadece desteklenen diller için
            foreach (var language in supportedLanguages)
            {
                try
                {
                    await SyncDestinationsAsync(language);
                    await SyncResortsAsync(language);
                    await SyncMealsAsync(language);
                    await SyncRoomTypesAsync(language);
                    await SyncFeaturesAsync(language);
                    await SyncTransferTypesAsync(language);
                    await SyncNoteTypesAsync(language);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing data for language: {Language}", language);
                    // Bir dil için hata olsa bile diğer dillere devam et
                }
            }

            _logger.LogInformation("SunHotels static data synchronization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SunHotels static data synchronization");
            throw;
        }
    }

    /// <summary>
    /// Sadece temel verileri senkronize eder (hızlı sync)
    /// </summary>
    public async Task SyncBasicDataAsync()
    {
        _logger.LogInformation("Starting SunHotels basic data synchronization...");

        try
        {
            await SyncLanguagesAsync();
            await SyncThemesAsync();
            await SyncDestinationsAsync("en");
            await SyncMealsAsync("en");
            await SyncRoomTypesAsync("en");
            await SyncFeaturesAsync("en");

            _logger.LogInformation("SunHotels basic data synchronization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SunHotels basic data synchronization");
            throw;
        }
    }

    /// <summary>
    /// Destinasyonları senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncDestinationsAsync(string language = "en")
    {
        _logger.LogInformation("Syncing destinations for language: {Language}", language);

        try
        {
            var destinations = await _sunHotelsService.GetDestinationsAsync(language);
            var now = DateTime.UtcNow;

            // Tüm mevcut kayıtları hafızaya al (batch optimization)
            var existingDestinations = await _dbContext.SunHotelsDestinations
                .AsNoTracking()
                .ToDictionaryAsync(x => x.DestinationId, x => x);

            var toAdd = new List<SunHotelsDestinationCache>();
            var toUpdate = new List<SunHotelsDestinationCache>();

            foreach (var dest in destinations)
            {
                if (existingDestinations.TryGetValue(dest.Id, out var existing))
                {
                    // Güncelleme gerekiyorsa
                    if (existing.Name != dest.Name || existing.Country != dest.Country)
                    {
                        existing.Name = dest.Name;
                        existing.Country = dest.Country;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsDestinationCache
                    {
                        DestinationId = dest.Id,
                        Name = dest.Name,
                        Country = dest.Country,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            // Toplu ekleme
            if (toAdd.Any())
            {
                await _dbContext.SunHotelsDestinations.AddRangeAsync(toAdd);
                _logger.LogInformation("Adding {Count} new destinations", toAdd.Count);
            }

            // Toplu güncelleme
            if (toUpdate.Any())
            {
                _dbContext.SunHotelsDestinations.UpdateRange(toUpdate);
                _logger.LogInformation("Updating {Count} existing destinations", toUpdate.Count);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} destinations for language: {Language} (Added: {Added}, Updated: {Updated})",
                destinations.Count, language, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing destinations for language: {Language}", language);
        }
    }

    /// <summary>
    /// Resort/Bölgeleri senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncResortsAsync(string language = "en")
    {
        _logger.LogInformation("Syncing resorts for language: {Language}", language);

        try
        {
            var resorts = await _sunHotelsService.GetResortsAsync(null, language);
            var now = DateTime.UtcNow;

            var existingResorts = await _dbContext.SunHotelsResorts
                .Where(x => x.Language == language)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ResortId, x => x);

            var toAdd = new List<SunHotelsResortCache>();
            var toUpdate = new List<SunHotelsResortCache>();

            foreach (var resort in resorts)
            {
                if (existingResorts.TryGetValue(resort.Id, out var existing))
                {
                    if (existing.Name != resort.Name ||
                        existing.DestinationId != resort.DestinationId ||
                        existing.CountryCode != resort.CountryCode)
                    {
                        existing.Name = resort.Name;
                        existing.DestinationId = resort.DestinationId;
                        existing.DestinationName = resort.DestinationName;
                        existing.CountryCode = resort.CountryCode;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsResortCache
                    {
                        ResortId = resort.Id,
                        Name = resort.Name,
                        DestinationId = resort.DestinationId,
                        DestinationName = resort.DestinationName,
                        CountryCode = resort.CountryCode,
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsResorts.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsResorts.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} resorts for language: {Language} (Added: {Added}, Updated: {Updated})",
                resorts.Count, language, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing resorts for language: {Language}", language);
        }
    }

    /// <summary>
    /// Yemek tiplerini senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncMealsAsync(string language = "en")
    {
        _logger.LogInformation("Syncing meals for language: {Language}", language);

        try
        {
            var meals = await _sunHotelsService.GetMealsAsync(language);
            var now = DateTime.UtcNow;

            var existingMeals = await _dbContext.SunHotelsMeals
                .Where(x => x.Language == language)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.MealId, x => x);

            var toAdd = new List<SunHotelsMealCache>();
            var toUpdate = new List<SunHotelsMealCache>();

            foreach (var meal in meals)
            {
                var labelsJson = System.Text.Json.JsonSerializer.Serialize(meal.Labels);

                if (existingMeals.TryGetValue(meal.Id, out var existing))
                {
                    if (existing.Name != meal.Name || existing.Labels != labelsJson)
                    {
                        existing.Name = meal.Name;
                        existing.Labels = labelsJson;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsMealCache
                    {
                        MealId = meal.Id,
                        Name = meal.Name,
                        Labels = labelsJson,
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsMeals.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsMeals.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} meals for language: {Language} (Added: {Added}, Updated: {Updated})",
                meals.Count, language, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing meals for language: {Language}", language);
        }
    }

    /// <summary>
    /// Oda tiplerini senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncRoomTypesAsync(string language = "en")
    {
        _logger.LogInformation("Syncing room types for language: {Language}", language);

        try
        {
            var roomTypes = await _sunHotelsService.GetRoomTypesAsync(language);
            var now = DateTime.UtcNow;

            var existingRoomTypes = await _dbContext.SunHotelsRoomTypes
                .Where(x => x.Language == language)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.RoomTypeId, x => x);

            var toAdd = new List<SunHotelsRoomTypeCache>();
            var toUpdate = new List<SunHotelsRoomTypeCache>();

            foreach (var roomType in roomTypes)
            {
                if (existingRoomTypes.TryGetValue(roomType.Id, out var existing))
                {
                    if (existing.Name != roomType.Name)
                    {
                        existing.Name = roomType.Name;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsRoomTypeCache
                    {
                        RoomTypeId = roomType.Id,
                        Name = roomType.Name,
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsRoomTypes.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsRoomTypes.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} room types for language: {Language} (Added: {Added}, Updated: {Updated})",
                roomTypes.Count, language, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing room types for language: {Language}", language);
        }
    }

    /// <summary>
    /// Özellikleri senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncFeaturesAsync(string language = "en")
    {
        _logger.LogInformation("Syncing features for language: {Language}", language);

        try
        {
            var features = await _sunHotelsService.GetFeaturesAsync(language);
            var now = DateTime.UtcNow;

            var existingFeatures = await _dbContext.SunHotelsFeatures
                .Where(x => x.Language == language)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.FeatureId, x => x);

            var toAdd = new List<SunHotelsFeatureCache>();
            var toUpdate = new List<SunHotelsFeatureCache>();

            foreach (var feature in features)
            {
                if (existingFeatures.TryGetValue(feature.Id, out var existing))
                {
                    if (existing.Name != feature.Name)
                    {
                        existing.Name = feature.Name;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsFeatureCache
                    {
                        FeatureId = feature.Id,
                        Name = feature.Name,
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsFeatures.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsFeatures.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} features for language: {Language}", features.Count, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing features for language: {Language}", language);
        }
    }

    /// <summary>
    /// Temaları senkronize eder (dil bağımsız) - (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncThemesAsync()
    {
        _logger.LogInformation("Syncing themes...");

        try
        {
            var themes = await _sunHotelsService.GetThemesAsync();
            var now = DateTime.UtcNow;

            var existingThemes = await _dbContext.SunHotelsThemes
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ThemeId, x => x);

            var toAdd = new List<SunHotelsThemeCache>();
            var toUpdate = new List<SunHotelsThemeCache>();

            foreach (var theme in themes)
            {
                if (existingThemes.TryGetValue(theme.Id, out var existing))
                {
                    if (existing.Name != theme.Name || existing.EnglishName != theme.EnglishName)
                    {
                        existing.Name = theme.Name;
                        existing.EnglishName = theme.EnglishName;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsThemeCache
                    {
                        ThemeId = theme.Id,
                        Name = theme.Name,
                        EnglishName = theme.EnglishName,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsThemes.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsThemes.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} themes (Added: {Added}, Updated: {Updated})",
                themes.Count, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing themes");
        }
    }

    /// <summary>
    /// Dilleri senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncLanguagesAsync()
    {
        _logger.LogInformation("Syncing languages...");

        try
        {
            var languages = await _sunHotelsService.GetLanguagesAsync();
            var now = DateTime.UtcNow;

            var existingLanguages = await _dbContext.SunHotelsLanguages
                .AsNoTracking()
                .ToDictionaryAsync(x => x.LanguageCode, x => x);

            var toAdd = new List<SunHotelsLanguageCache>();
            var toUpdate = new List<SunHotelsLanguageCache>();

            foreach (var lang in languages)
            {
                if (existingLanguages.TryGetValue(lang.Code, out var existing))
                {
                    if (existing.Name != lang.Name)
                    {
                        existing.Name = lang.Name;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsLanguageCache
                    {
                        LanguageCode = lang.Code,
                        Name = lang.Name,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsLanguages.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsLanguages.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} languages (Added: {Added}, Updated: {Updated})",
                languages.Count, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing languages");
        }
    }

    /// <summary>
    /// Transfer tiplerini senkronize eder (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncTransferTypesAsync(string language = "en")
    {
        _logger.LogInformation("Syncing transfer types for language: {Language}", language);

        try
        {
            var transferTypes = await _sunHotelsService.GetTransferTypesAsync(language);
            var now = DateTime.UtcNow;

            var existingTransferTypes = await _dbContext.SunHotelsTransferTypes
                .Where(x => x.Language == language)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.TransferTypeId, x => x);

            var toAdd = new List<SunHotelsTransferTypeCache>();
            var toUpdate = new List<SunHotelsTransferTypeCache>();

            foreach (var transferType in transferTypes)
            {
                if (existingTransferTypes.TryGetValue(transferType.Id, out var existing))
                {
                    if (existing.Name != transferType.Name)
                    {
                        existing.Name = transferType.Name;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsTransferTypeCache
                    {
                        TransferTypeId = transferType.Id,
                        Name = transferType.Name,
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsTransferTypes.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsTransferTypes.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} transfer types for language: {Language} (Added: {Added}, Updated: {Updated})",
                transferTypes.Count, language, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing transfer types for language: {Language}", language);
        }
    }

    /// <summary>
    /// Not tiplerini senkronize eder (Hotel ve Room) - (Batch Processing - Optimized)
    /// </summary>
    public async Task SyncNoteTypesAsync(string language = "en")
    {
        _logger.LogInformation("Syncing note types for language: {Language}", language);

        try
        {
            var now = DateTime.UtcNow;

            // Mevcut tüm note types'ı hafızaya al
            var existingNoteTypes = await _dbContext.SunHotelsNoteTypes
                .Where(x => x.Language == language)
                .AsNoTracking()
                .ToListAsync();

            var toAdd = new List<SunHotelsNoteTypeCache>();
            var toUpdate = new List<SunHotelsNoteTypeCache>();

            // Hotel Note Types
            var hotelNoteTypes = await _sunHotelsService.GetHotelNoteTypesAsync(language);
            foreach (var noteType in hotelNoteTypes)
            {
                var existing = existingNoteTypes.FirstOrDefault(x =>
                    x.NoteTypeId == noteType.Id && x.NoteCategory == "Hotel");

                if (existing != null)
                {
                    if (existing.Name != noteType.Name)
                    {
                        existing.Name = noteType.Name;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsNoteTypeCache
                    {
                        NoteTypeId = noteType.Id,
                        Name = noteType.Name,
                        NoteCategory = "Hotel",
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            // Room Note Types
            var roomNoteTypes = await _sunHotelsService.GetRoomNoteTypesAsync(language);
            foreach (var noteType in roomNoteTypes)
            {
                var existing = existingNoteTypes.FirstOrDefault(x =>
                    x.NoteTypeId == noteType.Id && x.NoteCategory == "Room");

                if (existing != null)
                {
                    if (existing.Name != noteType.Name)
                    {
                        existing.Name = noteType.Name;
                        existing.LastSyncedAt = now;
                        existing.UpdatedAt = now;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(new SunHotelsNoteTypeCache
                    {
                        NoteTypeId = noteType.Id,
                        Name = noteType.Name,
                        NoteCategory = "Room",
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    });
                }
            }

            if (toAdd.Any()) await _dbContext.SunHotelsNoteTypes.AddRangeAsync(toAdd);
            if (toUpdate.Any()) _dbContext.SunHotelsNoteTypes.UpdateRange(toUpdate);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {HotelCount} hotel note types and {RoomCount} room note types for language: {Language} (Added: {Added}, Updated: {Updated})",
                hotelNoteTypes.Count, roomNoteTypes.Count, language, toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing note types for language: {Language}", language);
        }
    }

    /// <summary>
    /// Belirli bir destinasyonun otellerini senkronize eder
    /// </summary>
    public async Task SyncHotelsForDestinationAsync(string destinationId, string language = "en")
    {
        _logger.LogInformation("Syncing hotels for destination: {DestinationId}, language: {Language}", destinationId, language);

        try
        {
            var hotels = await _sunHotelsService.GetStaticHotelsAndRoomsAsync(destinationId, null, null, language);
            var now = DateTime.UtcNow;

            foreach (var hotel in hotels)
            {
                var existing = await _dbContext.SunHotelsHotels
                    .Include(x => x.Rooms)
                    .FirstOrDefaultAsync(x => x.HotelId == hotel.HotelId && x.Language == language);

                var featureIdsJson = System.Text.Json.JsonSerializer.Serialize(hotel.FeatureIds);
                var themeIdsJson = System.Text.Json.JsonSerializer.Serialize(hotel.ThemeIds);
                var imageUrlsJson = System.Text.Json.JsonSerializer.Serialize(hotel.Images.Select(x => x.Url).ToList());

                if (existing != null)
                {
                    existing.Name = hotel.Name;
                    existing.Description = hotel.Description;
                    existing.Address = hotel.Address;
                    existing.ZipCode = hotel.ZipCode;
                    existing.City = hotel.City;
                    existing.Country = hotel.Country;
                    existing.CountryCode = hotel.CountryCode;
                    existing.Category = hotel.Category;
                    existing.Latitude = hotel.Latitude;
                    existing.Longitude = hotel.Longitude;
                    existing.GiataCode = hotel.GiataCode;
                    existing.ResortId = hotel.ResortId;
                    existing.ResortName = hotel.ResortName;
                    existing.Phone = hotel.Phone;
                    existing.Fax = hotel.Fax;
                    existing.Email = hotel.Email;
                    existing.Website = hotel.Website;
                    existing.FeatureIds = featureIdsJson;
                    existing.ThemeIds = themeIdsJson;
                    existing.ImageUrls = imageUrlsJson;
                    existing.LastSyncedAt = now;
                    existing.UpdatedAt = now;

                    // Remove old rooms and add new ones
                    _dbContext.SunHotelsRooms.RemoveRange(existing.Rooms);

                    foreach (var room in hotel.Rooms)
                    {
                        existing.Rooms.Add(new SunHotelsRoomCache
                        {
                            HotelCacheId = existing.Id,
                            HotelId = hotel.HotelId,
                            RoomTypeId = room.RoomTypeId,
                            Name = room.Name,
                            EnglishName = room.EnglishName,
                            Description = room.Description,
                            MaxOccupancy = room.MaxOccupancy,
                            MinOccupancy = room.MinOccupancy,
                            FeatureIds = System.Text.Json.JsonSerializer.Serialize(room.FeatureIds),
                            ImageUrls = System.Text.Json.JsonSerializer.Serialize(room.Images.Select(x => x.Url).ToList()),
                            Language = language,
                            LastSyncedAt = now,
                            CreatedAt = now
                        });
                    }
                }
                else
                {
                    var newHotel = new SunHotelsHotelCache
                    {
                        HotelId = hotel.HotelId,
                        Name = hotel.Name,
                        Description = hotel.Description,
                        Address = hotel.Address,
                        ZipCode = hotel.ZipCode,
                        City = hotel.City,
                        Country = hotel.Country,
                        CountryCode = hotel.CountryCode,
                        Category = hotel.Category,
                        Latitude = hotel.Latitude,
                        Longitude = hotel.Longitude,
                        GiataCode = hotel.GiataCode,
                        ResortId = hotel.ResortId,
                        ResortName = hotel.ResortName,
                        Phone = hotel.Phone,
                        Fax = hotel.Fax,
                        Email = hotel.Email,
                        Website = hotel.Website,
                        FeatureIds = featureIdsJson,
                        ThemeIds = themeIdsJson,
                        ImageUrls = imageUrlsJson,
                        Language = language,
                        LastSyncedAt = now,
                        CreatedAt = now
                    };

                    foreach (var room in hotel.Rooms)
                    {
                        newHotel.Rooms.Add(new SunHotelsRoomCache
                        {
                            HotelId = hotel.HotelId,
                            RoomTypeId = room.RoomTypeId,
                            Name = room.Name,
                            EnglishName = room.EnglishName,
                            Description = room.Description,
                            MaxOccupancy = room.MaxOccupancy,
                            MinOccupancy = room.MinOccupancy,
                            FeatureIds = System.Text.Json.JsonSerializer.Serialize(room.FeatureIds),
                            ImageUrls = System.Text.Json.JsonSerializer.Serialize(room.Images.Select(x => x.Url).ToList()),
                            Language = language,
                            LastSyncedAt = now,
                            CreatedAt = now
                        });
                    }

                    _dbContext.SunHotelsHotels.Add(newHotel);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} hotels for destination: {DestinationId}, language: {Language}",
                hotels.Count, destinationId, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing hotels for destination: {DestinationId}, language: {Language}",
                destinationId, language);
        }
    }

    /// <summary>
    /// SunHotels API'den desteklenen dilleri çeker
    /// </summary>
    private async Task<List<string>> GetSupportedLanguagesAsync()
    {
        try
        {
            var languages = await _sunHotelsService.GetLanguagesAsync();

            if (languages == null || !languages.Any())
            {
                _logger.LogWarning("No languages returned from API, using default language 'en'");
                return new List<string> { "en" };
            }

            // Dil kodlarını al (boş olmayanlar)
            var supportedLanguages = languages
                .Where(l => !string.IsNullOrWhiteSpace(l.Code))
                .Select(l => l.Code.ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!supportedLanguages.Any())
            {
                _logger.LogWarning("No valid language codes found, using default language 'en'");
                return new List<string> { "en" };
            }

            return supportedLanguages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported languages from API, using default language 'en'");
            return new List<string> { "en" };
        }
    }
}
