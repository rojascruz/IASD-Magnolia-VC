namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un rol de usuario
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
