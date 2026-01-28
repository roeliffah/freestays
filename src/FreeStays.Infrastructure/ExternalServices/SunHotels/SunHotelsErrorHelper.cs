namespace FreeStays.Infrastructure.ExternalServices.SunHotels;

/// <summary>
/// SunHotels API hata mesajlarını kullanıcı dostu Türkçe mesajlara çeviren helper
/// </summary>
public static class SunHotelsErrorHelper
{
    private static readonly Dictionary<string, string> ErrorMappings = new()
    {
        // API Errors
        { "Invalid credentials", "Otel sistemi bağlantı bilgileri geçersiz. Lütfen sistem yöneticisiyle iletişime geçin." },
        { "Invalid username or password", "Otel sistemi bağlantı bilgileri hatalı." },
        { "Invalid destination", "Geçersiz destinasyon. Lütfen farklı bir destinasyon seçin." },
        { "No hotels found", "Bu tarihler için müsait otel bulunamadı. Lütfen farklı tarihler deneyin." },
        { "Invalid check-in date", "Giriş tarihi geçersiz. Lütfen gelecek bir tarih seçin." },
        { "Invalid check-out date", "Çıkış tarihi geçersiz. Lütfen giriş tarihinden sonraki bir tarih seçin." },
        { "Check-in date must be before check-out date", "Giriş tarihi çıkış tarihinden önce olmalıdır." },
        { "Invalid number of adults", "Yetişkin sayısı geçersiz." },
        { "Invalid number of children", "Çocuk sayısı geçersiz." },
        
        // PreBook Errors
        { "PreBook code expired", "Rezervasyon süresi doldu. Lütfen tekrar arama yapın." },
        { "PreBook code invalid", "Rezervasyon kodu geçersiz. Lütfen tekrar deneyin." },
        { "Price changed", "Fiyat değişti. Güncel fiyat ile devam etmek ister misiniz?" },
        { "Room no longer available", "Seçtiğiniz oda artık müsait değil. Lütfen başka bir oda seçin." },
        
        // Booking Errors
        { "Booking failed", "Rezervasyon işlemi başarısız oldu. Lütfen tekrar deneyin." },
        { "Invalid guest information", "Misafir bilgileri eksik veya hatalı." },
        { "Invalid email", "E-posta adresi geçersiz." },
        { "Payment failed", "Ödeme işlemi başarısız oldu." },
        { "Credit card declined", "Kredi kartınız reddedildi. Lütfen farklı bir kart deneyin." },
        
        // Rate Limiting
        { "Too many requests", "Çok fazla istek gönderildi. Lütfen birkaç saniye bekleyin." },
        { "Rate limit exceeded", "İstek limiti aşıldı. Lütfen kısa bir süre sonra tekrar deneyin." },
        
        // Connection Errors
        { "Connection timeout", "Bağlantı zaman aşımına uğradı. Lütfen tekrar deneyin." },
        { "Connection failed", "Otel sistemine bağlanılamadı. Lütfen daha sonra tekrar deneyin." },
        { "Service unavailable", "Otel sistemi şu anda hizmet veremiyor. Lütfen daha sonra tekrar deneyin." }
    };

    /// <summary>
    /// SunHotels hata mesajını Türkçe kullanıcı dostu mesaja çevirir
    /// </summary>
    public static string GetFriendlyErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
        }

        // Exact match kontrolü
        foreach (var mapping in ErrorMappings)
        {
            if (errorMessage.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
            {
                return mapping.Value;
            }
        }

        // HTTP status code bazlı fallback
        if (errorMessage.Contains("404"))
        {
            return "İstediğiniz kaynak bulunamadı.";
        }
        if (errorMessage.Contains("500") || errorMessage.Contains("503"))
        {
            return "Otel sistemi geçici olarak hizmet veremiyor. Lütfen birkaç dakika sonra tekrar deneyin.";
        }
        if (errorMessage.Contains("400"))
        {
            return "Gönderilen bilgiler geçersiz. Lütfen bilgilerinizi kontrol edip tekrar deneyin.";
        }
        if (errorMessage.Contains("401") || errorMessage.Contains("403"))
        {
            return "Yetkilendirme hatası. Sistem yöneticisiyle iletişime geçin.";
        }

        // Genel hata mesajı
        return "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
    }

    /// <summary>
    /// Exception'dan kullanıcı dostu mesaj üretir
    /// </summary>
    public static string GetFriendlyErrorFromException(Exception ex)
    {
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return "Bağlantı zaman aşımına uğradı. Lütfen tekrar deneyin.";
            }
            return "Otel sistemine bağlanılamadı. Lütfen internet bağlantınızı kontrol edin.";
        }

        if (ex is TaskCanceledException)
        {
            return "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.";
        }

        return GetFriendlyErrorMessage(ex.Message);
    }
}
