namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un evento con su tipo asociado.
/// Usado para mostrar eventos en el UI con información completa del tipo.
/// </summary>
public class EventWithType
{
    // Propiedades del Evento
    public Guid Id { get; set; }
    public int EventTypeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }
    //public string? Description { get; set; }

    public DateTime EventDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public string? Location { get; set; }
    public string? Address { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }

    // Auditoría
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Propiedades del Tipo de Evento
    public string? EventTypeName { get; set; }
    public string? EventTypeColor { get; set; }

    // Propiedades calculadas para el UI
    public string Day => EventDate.Day.ToString("00");

    public string Month => EventDate.ToString("MMM", new System.Globalization.CultureInfo("es-ES")).ToUpper();

    public string FormattedTime
    {
        get
        {
            if (StartTime.HasValue)
            {
                var hours = StartTime.Value.Hours;
                var minutes = StartTime.Value.Minutes;
                var period = hours >= 12 ? "PM" : "AM";
                hours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);
                return $"{hours}:{minutes:00} {period}";
            }
            return string.Empty;
        }
    }

    public string DisplayLocation => Location ?? "Templo Principal";

    public string DisplayImage => string.IsNullOrWhiteSpace(ImageUrl) 
        ? "/img/eventos/default-event.png" 
        : ImageUrl;

    public string TypeColorClass => EventTypeColor ?? "#003057";
}
