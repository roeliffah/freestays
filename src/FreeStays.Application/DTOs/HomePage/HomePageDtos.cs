namespace FreeStays.Application.DTOs.HomePage;

public class HomePageSectionDto
{
    public Guid Id { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public object? Configuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateHomePageSectionRequest
{
    public string SectionType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string Configuration { get; set; } = "{}";
}

public class UpdateHomePageSectionRequest
{
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string Configuration { get; set; } = "{}";
}

public class ReorderSectionsRequest
{
    public List<SectionOrderItem> SectionOrders { get; set; } = new();
}

public class SectionOrderItem
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
}

public class SectionTranslationsDto
{
    public Dictionary<string, TranslationDto> Translations { get; set; } = new();
}

public class TranslationDto
{
    public string Locale { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
}

public class UpdateSectionTranslationsRequest
{
    public Dictionary<string, TranslationDto> Translations { get; set; } = new();
}

public class SectionHotelDto
{
    public Guid Id { get; set; }
    public string HotelId { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public object? HotelDetails { get; set; }
}

public class UpdateSectionHotelsRequest
{
    public List<SectionHotelItem> Hotels { get; set; } = new();
}

public class SectionHotelItem
{
    public string HotelId { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class SectionDestinationDto
{
    public Guid Id { get; set; }
    public string DestinationId { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public object? DestinationDetails { get; set; }
}

public class UpdateSectionDestinationsRequest
{
    public List<SectionDestinationItem> Destinations { get; set; } = new();
}

public class SectionDestinationItem
{
    public string DestinationId { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
