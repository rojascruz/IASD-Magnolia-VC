namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo completo para gestión de tipos de eventos (admin)
/// </summary>
public class EventTypeAdmin
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
