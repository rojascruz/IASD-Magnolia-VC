namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un tipo de evento de la iglesia.
/// Mapea con la tabla 'event_types' de PostgreSQL.
/// </summary>
public class EventType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Color { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
