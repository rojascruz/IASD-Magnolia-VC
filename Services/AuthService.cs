using IASD_Magnolia.Models;
using Npgsql;
using Dapper;
using IASD_Magnolia.Models;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio de autenticación con PostgreSQL y pgcrypto
/// </summary>
public class AuthService
{
    private readonly string _connectionString;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string 'PostgreSQL' no encontrada");
        _logger = logger;
    }

    /// <summary>
    /// Valida las credenciales del usuario y devuelve el usuario si son correctas.
    /// Acepta email o username como identificador.
    /// </summary>
    public async Task<User?> LoginAsync(string emailOrUsername, string password)
    {
        try
        {
            _logger.LogInformation("Intentando login para: {EmailOrUsername}", emailOrUsername);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    u.id AS Id,
                    u.role_id AS RoleId,
                    u.full_name AS FullName,
                    u.username AS Username,
                    u.email AS Email,
                    u.is_active AS IsActive,
                    u.last_login_at AS LastLoginAt,
                    u.created_at AS CreatedAt,
                    r.name AS RoleName
                FROM users u
                INNER JOIN roles r ON u.role_id = r.id
                WHERE (u.email = @EmailOrUsername OR u.username = @EmailOrUsername)
                  AND u.password_hash = crypt(@Password, u.password_hash)
                  AND u.is_active = true
                  AND u.deleted_at IS NULL";

            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { EmailOrUsername = emailOrUsername, Password = password });

            if (user != null)
            {
                // Actualizar last_login_at
                await UpdateLastLoginAsync(user.Id);
                _logger.LogInformation("Login exitoso para: {EmailOrUsername}", emailOrUsername);
            }
            else
            {
                _logger.LogWarning("Login fallido para: {EmailOrUsername}", emailOrUsername);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el login para: {EmailOrUsername}", emailOrUsername);
            return null;
        }
    }

    /// <summary>
    /// Registra un nuevo usuario con contraseña hasheada
    /// </summary>
    public async Task<(bool Success, string Message, Guid? UserId)> RegisterAsync(
        string fullName, 
        string email, 
        string password, 
        string? username = null,
        int roleId = 2)
    {
        try
        {
            _logger.LogInformation("Intentando registrar usuario: {Email}", email);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Verificar si el email ya existe
            const string checkEmailSql = "SELECT COUNT(*) FROM users WHERE email = @Email AND deleted_at IS NULL";
            var emailExists = await connection.ExecuteScalarAsync<int>(checkEmailSql, new { Email = email });

            if (emailExists > 0)
            {
                _logger.LogWarning("El email ya existe: {Email}", email);
                return (false, "El correo electrónico ya está registrado", null);
            }

            // Verificar si el username ya existe (si se proporciona)
            if (!string.IsNullOrWhiteSpace(username))
            {
                const string checkUsernameSql = "SELECT COUNT(*) FROM users WHERE username = @Username AND deleted_at IS NULL";
                var usernameExists = await connection.ExecuteScalarAsync<int>(checkUsernameSql, new { Username = username });

                if (usernameExists > 0)
                {
                    _logger.LogWarning("El username ya existe: {Username}", username);
                    return (false, "El nombre de usuario ya está registrado", null);
                }
            }

            // Insertar usuario con password hasheado usando pgcrypto
            const string insertSql = @"
                INSERT INTO users (role_id, full_name, username, email, password_hash, is_active, created_at)
                VALUES (@RoleId, @FullName, @Username, @Email, crypt(@Password, gen_salt('bf')), true, NOW())
                RETURNING id";

            var userId = await connection.ExecuteScalarAsync<Guid>(insertSql, new
            {
                RoleId = roleId,
                FullName = fullName,
                Username = username,
                Email = email,
                Password = password
            });

            _logger.LogInformation("Usuario registrado exitosamente: {Email}, ID: {UserId}", email, userId);
            return (true, "Usuario registrado exitosamente", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario: {Email}", email);
            return (false, $"Error al registrar usuario: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Actualiza la fecha del último login
    /// </summary>
    private async Task UpdateLastLoginAsync(Guid userId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "UPDATE users SET last_login_at = NOW() WHERE id = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar last_login_at para usuario: {UserId}", userId);
        }
    }

    /// <summary>
    /// Obtiene un usuario por ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    u.id AS Id,
                    u.role_id AS RoleId,
                    u.full_name AS FullName,
                    u.username AS Username,
                    u.email AS Email,
                    u.is_active AS IsActive,
                    u.last_login_at AS LastLoginAt,
                    u.created_at AS CreatedAt,
                    r.name AS RoleName
                FROM users u
                INNER JOIN roles r ON u.role_id = r.id
                WHERE u.id = @UserId
                  AND u.is_active = true
                  AND u.deleted_at IS NULL";

            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Obtiene todos los roles disponibles
    /// </summary>
    public async Task<List<Role>> GetRolesAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT id AS Id, name AS Name, created_at AS CreatedAt FROM roles ORDER BY name";
            var roles = await connection.QueryAsync<Role>(sql);
            return roles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener roles");
            return new List<Role>();
        }
    }

    /// <summary>
    /// Obtiene todos los usuarios del sistema
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    u.id AS Id,
                    u.role_id AS RoleId,
                    u.full_name AS FullName,
                    u.username AS Username,
                    u.email AS Email,
                    u.is_active AS IsActive,
                    u.last_login_at AS LastLoginAt,
                    u.created_at AS CreatedAt,
                    u.updated_at AS UpdatedAt,
                    r.name AS RoleName
                FROM users u
                INNER JOIN roles r ON u.role_id = r.id
                WHERE u.deleted_at IS NULL
                ORDER BY u.created_at DESC";

            var users = await connection.QueryAsync<User>(sql);
            return users.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return new List<User>();
        }
    }

    /// <summary>
    /// Actualiza un usuario existente
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserAsync(
        Guid userId,
        string fullName,
        string? username,
        string email,
        string? phone,
        int roleId,
        bool isActive)
    {
        try
        {
            _logger.LogInformation("Actualizando usuario: {UserId}", userId);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Verificar si el email ya existe en otro usuario
            const string checkEmailSql = @"
                SELECT COUNT(*) FROM users 
                WHERE email = @Email 
                  AND id != @UserId 
                  AND deleted_at IS NULL";
            var emailExists = await connection.ExecuteScalarAsync<int>(checkEmailSql, new { Email = email, UserId = userId });

            if (emailExists > 0)
            {
                return (false, "El correo electrónico ya está registrado en otro usuario");
            }

            // Verificar si el username ya existe en otro usuario
            if (!string.IsNullOrWhiteSpace(username))
            {
                const string checkUsernameSql = @"
                    SELECT COUNT(*) FROM users 
                    WHERE username = @Username 
                      AND id != @UserId 
                      AND deleted_at IS NULL";
                var usernameExists = await connection.ExecuteScalarAsync<int>(checkUsernameSql, new { Username = username, UserId = userId });

                if (usernameExists > 0)
                {
                    return (false, "El nombre de usuario ya está registrado en otro usuario");
                }
            }

            // Actualizar usuario
            const string updateSql = @"
                UPDATE users 
                SET role_id = @RoleId,
                    full_name = @FullName,
                    username = @Username,
                    email = @Email,
                    phone = @Phone,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @UserId";

            await connection.ExecuteAsync(updateSql, new
            {
                UserId = userId,
                RoleId = roleId,
                FullName = fullName,
                Username = username,
                Email = email,
                Phone = phone,
                IsActive = isActive
            });

            _logger.LogInformation("Usuario actualizado exitosamente: {UserId}", userId);
            return (true, "Usuario actualizado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario: {UserId}", userId);
            return (false, $"Error al actualizar usuario: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina un usuario (soft delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteUserAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Eliminando usuario: {UserId}", userId);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string deleteSql = @"
                UPDATE users 
                SET deleted_at = NOW(),
                    is_active = false,
                    updated_at = NOW()
                WHERE id = @UserId";

            await connection.ExecuteAsync(deleteSql, new { UserId = userId });

            _logger.LogInformation("Usuario eliminado exitosamente: {UserId}", userId);
            return (true, "Usuario eliminado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario: {UserId}", userId);
            return (false, $"Error al eliminar usuario: {ex.Message}");
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            _logger.LogInformation("Cambiando contraseña para usuario: {UserId}", userId);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string updateSql = @"
                UPDATE users 
                SET password_hash = crypt(@Password, gen_salt('bf')),
                    updated_at = NOW()
                WHERE id = @UserId";

            await connection.ExecuteAsync(updateSql, new { UserId = userId, Password = newPassword });

            _logger.LogInformation("Contraseña actualizada exitosamente para usuario: {UserId}", userId);
            return (true, "Contraseña actualizada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña para usuario: {UserId}", userId);
            return (false, $"Error al cambiar contraseña: {ex.Message}");
        }
    }
}

