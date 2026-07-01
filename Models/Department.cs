namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un departamento/ministerio
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
