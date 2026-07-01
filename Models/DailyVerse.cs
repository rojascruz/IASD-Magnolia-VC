namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo para representar un versículo bíblico diario.
/// Usado por el componente VersiculoDelDia y el servicio VerseOfTheDayService.
/// </summary>
/// <remarks>
/// MANTENIMIENTO:
/// - Si necesitas agregar más propiedades (ej: idioma, categoría), hazlo aquí
/// - Asegúrate de actualizar también el servicio VerseOfTheDayService
/// - Este modelo se usa en toda la aplicación para versículos
/// 
/// CREADO: 2025-01-XX
/// AUTOR: Equipo IASD Magnolia
/// </remarks>
public class DailyVerse
{
    /// <summary>
    /// Texto completo del versículo bíblico
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Referencia bíblica (ej: "Juan 3:16", "Salmos 23:1-3")
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Fuente del versículo: "verseoftheday.com" o "versículo local" (fallback)
    /// </summary>
    public string Source { get; set; } = "verseoftheday.com";

    /// <summary>
    /// Fecha y hora en que se obtuvo el versículo
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
