using Dapper;
using IASD_Magnolia.Models;
using Npgsql;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para gestionar departamentos/ministerios
/// </summary>
public class DepartmentsService
{
    private readonly string _connectionString;
    private readonly ILogger<DepartmentsService> _logger;

    public DepartmentsService(IConfiguration configuration, ILogger<DepartmentsService> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string 'PostgreSQL' no encontrada");
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los departamentos (activos e inactivos)
    /// </summary>
    public async Task<List<Department>> GetAllDepartmentsAsync()
    {
        try
        {
            const string sql = @"
                SELECT 
                    id AS Id,
                    name AS Name,
                    description AS Description,
                    display_order AS DisplayOrder,
                    is_active AS IsActive,
                    created_at AS CreatedAt
                FROM departments
                ORDER BY display_order ASC, name ASC";

            using var connection = new NpgsqlConnection(_connectionString);
            var departments = await connection.QueryAsync<Department>(sql);

            _logger.LogInformation("Se obtuvieron {Count} departamentos", departments.Count());
            return departments.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los departamentos");
            return new List<Department>();
        }
    }

    /// <summary>
    /// Obtiene un departamento por ID
    /// </summary>
    public async Task<Department?> GetDepartmentByIdAsync(int id)
    {
        try
        {
            const string sql = @"
                SELECT 
                    id AS Id,
                    name AS Name,
                    description AS Description,
                    display_order AS DisplayOrder,
                    is_active AS IsActive,
                    created_at AS CreatedAt
                FROM departments
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Department>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener departamento con ID {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Crea un nuevo departamento
    /// </summary>
    public async Task<int> CreateDepartmentAsync(Department department)
    {
        try
        {
            const string sql = @"
                INSERT INTO departments (name, description, display_order, is_active)
                VALUES (@Name, @Description, @DisplayOrder, @IsActive)
                RETURNING id";

            using var connection = new NpgsqlConnection(_connectionString);
            var id = await connection.ExecuteScalarAsync<int>(sql, department);

            _logger.LogInformation("Departamento creado con ID {Id}: {Name}", id, department.Name);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear departamento {Name}", department.Name);
            throw;
        }
    }

    /// <summary>
    /// Actualiza un departamento existente
    /// </summary>
    public async Task<bool> UpdateDepartmentAsync(Department department)
    {
        try
        {
            const string sql = @"
                UPDATE departments
                SET 
                    name = @Name,
                    description = @Description,
                    display_order = @DisplayOrder,
                    is_active = @IsActive
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(sql, department);

            _logger.LogInformation("Departamento {Id} actualizado: {Name}", department.Id, department.Name);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar departamento {Id}", department.Id);
            throw;
        }
    }

    /// <summary>
    /// Elimina un departamento (solo si no tiene líderes asociados)
    /// </summary>
    public async Task<bool> DeleteDepartmentAsync(int id)
    {
        try
        {
            // Verificar si tiene líderes asociados
            const string checkSql = "SELECT COUNT(*) FROM leadership WHERE department_id = @Id AND deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            var leadershipCount = await connection.ExecuteScalarAsync<int>(checkSql, new { Id = id });

            if (leadershipCount > 0)
            {
                _logger.LogWarning("No se puede eliminar el departamento {Id} porque tiene {Count} líderes asociados", id, leadershipCount);
                return false;
            }

            const string deleteSql = "DELETE FROM departments WHERE id = @Id";
            var rowsAffected = await connection.ExecuteAsync(deleteSql, new { Id = id });

            _logger.LogInformation("Departamento {Id} eliminado", id);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar departamento {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Verifica si un nombre de departamento ya existe
    /// </summary>
    public async Task<bool> DepartmentNameExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            var sql = "SELECT COUNT(*) FROM departments WHERE LOWER(name) = LOWER(@Name)";

            if (excludeId.HasValue)
            {
                sql += " AND id != @ExcludeId";
            }

            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(sql, new { Name = name, ExcludeId = excludeId });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de nombre {Name}", name);
            return false;
        }
    }

    /// <summary>
    /// Obtiene el siguiente número de orden disponible
    /// </summary>
    public async Task<int> GetNextDisplayOrderAsync()
    {
        try
        {
            const string sql = "SELECT COALESCE(MAX(display_order), 0) + 10 FROM departments";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener siguiente orden de visualización");
            return 10;
        }
    }
}
