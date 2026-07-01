namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un usuario del sistema
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navegación
    public string? RoleName { get; set; }
}
