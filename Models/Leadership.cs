namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un líder/recurso humano de un departamento
/// </summary>
public class Leadership
{
    public Guid Id { get; set; }
    public int DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Biography { get; set; }
    public string? PublicEmail { get; set; }
    public string? PublicPhone { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navegación
    public string? DepartmentName { get; set; }

    // Propiedades calculadas
    public string DisplayPhoto => string.IsNullOrWhiteSpace(PhotoUrl) 
        ? "/img/recursos/default-person.png" 
        : PhotoUrl;

    public string ShortBiography => Biography != null && Biography.Length > 150 
        ? Biography.Substring(0, 150) + "..." 
        : Biography ?? string.Empty;
}
