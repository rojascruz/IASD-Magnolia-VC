using Npgsql;
using Dapper;
using IASD_Magnolia.Models;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para gestionar recursos (departamentos y liderazgo)
/// </summary>
public class ResourcesService
{
    private readonly string _connectionString;
    private readonly ILogger<ResourcesService> _logger;

    public ResourcesService(IConfiguration configuration, ILogger<ResourcesService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada");
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los departamentos activos
    /// </summary>
    public async Task<List<Department>> GetActiveDepartmentsAsync()
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
                WHERE is_active = true
                ORDER BY display_order ASC, name ASC";

            using var connection = new NpgsqlConnection(_connectionString);
            var departments = await connection.QueryAsync<Department>(sql);

            _logger.LogInformation("Se obtuvieron {Count} departamentos activos", departments.Count());
            return departments.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener departamentos activos");
            return new List<Department>();
        }
    }

    /// <summary>
    /// Obtiene solo los departamentos que tienen liderazgo activo
    /// </summary>
    public async Task<List<Department>> GetDepartmentsWithLeadershipAsync()
    {
        try
        {
            const string sql = @"
                SELECT DISTINCT
                    d.id AS Id,
                    d.name AS Name,
                    d.description AS Description,
                    d.display_order AS DisplayOrder,
                    d.is_active AS IsActive,
                    d.created_at AS CreatedAt
                FROM departments d
                INNER JOIN leadership l ON d.id = l.department_id
                WHERE d.is_active = true 
                    AND l.is_active = true 
                    AND l.deleted_at IS NULL
                ORDER BY d.display_order ASC, d.name ASC";

            using var connection = new NpgsqlConnection(_connectionString);
            var departments = await connection.QueryAsync<Department>(sql);

            _logger.LogInformation("Se obtuvieron {Count} departamentos con liderazgo activo", departments.Count());
            return departments.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener departamentos con liderazgo");
            return new List<Department>();
        }
    }

    /// <summary>
    /// Obtiene todos los líderes activos con su departamento
    /// </summary>
    public async Task<List<Leadership>> GetActiveLeadershipAsync(int? departmentId = null, int limit = 50)
    {
        try
        {
            var sql = @"
                SELECT 
                    l.id AS Id,
                    l.department_id AS DepartmentId,
                    l.full_name AS FullName,
                    l.position AS Position,
                    l.photo_url AS PhotoUrl,
                    l.biography AS Biography,
                    l.public_email AS PublicEmail,
                    l.public_phone AS PublicPhone,
                    l.start_date::timestamp AS StartDate,
                    l.end_date::timestamp AS EndDate,
                    l.display_order AS DisplayOrder,
                    l.is_active AS IsActive,
                    l.created_at AS CreatedAt,
                    l.updated_at AS UpdatedAt,
                    d.name AS DepartmentName
                FROM leadership l
                INNER JOIN departments d ON l.department_id = d.id
                WHERE l.is_active = true 
                    AND l.deleted_at IS NULL
                    AND d.is_active = true";

            if (departmentId.HasValue)
            {
                sql += " AND l.department_id = @DepartmentId";
            }

            sql += @"
                ORDER BY 
                    l.display_order ASC,
                    l.full_name ASC
                LIMIT @Limit";

            using var connection = new NpgsqlConnection(_connectionString);
            var leadership = await connection.QueryAsync<Leadership>(sql, new 
            { 
                DepartmentId = departmentId,
                Limit = limit 
            });

            _logger.LogInformation("Se obtuvieron {Count} recursos de liderazgo", leadership.Count());
            return leadership.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recursos de liderazgo");
            return new List<Leadership>();
        }
    }

    /// <summary>
    /// Obtiene un líder por ID
    /// </summary>
    public async Task<Leadership?> GetLeadershipByIdAsync(Guid id)
    {
        try
        {
            const string sql = @"
                SELECT 
                    l.id AS Id,
                    l.department_id AS DepartmentId,
                    l.full_name AS FullName,
                    l.position AS Position,
                    l.photo_url AS PhotoUrl,
                    l.biography AS Biography,
                    l.public_email AS PublicEmail,
                    l.public_phone AS PublicPhone,
                    l.start_date::timestamp AS StartDate,
                    l.end_date::timestamp AS EndDate,
                    l.display_order AS DisplayOrder,
                    l.is_active AS IsActive,
                    l.created_at AS CreatedAt,
                    l.updated_at AS UpdatedAt,
                    d.name AS DepartmentName
                FROM leadership l
                INNER JOIN departments d ON l.department_id = d.id
                WHERE l.id = @Id 
                    AND l.deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Leadership>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener líder por ID: {Id}", id);
            return null;
        }
    }
}
