using Npgsql;
using Dapper;
using IASD_Magnolia.Models;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para gestionar tipos de eventos desde PostgreSQL.
/// </summary>
public class EventTypesService
{
    private readonly string _connectionString;
    private readonly ILogger<EventTypesService> _logger;

    public EventTypesService(IConfiguration configuration, ILogger<EventTypesService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada");
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los tipos de eventos para administración
    /// </summary>
    public async Task<List<EventTypeAdmin>> GetAllEventTypesAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo todos los tipos de eventos");

            const string sql = @"
                SELECT 
                    id AS Id,
                    name AS Name,
                    description AS Description,
                    color AS Color,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM event_types
                ORDER BY name";

            using var connection = new NpgsqlConnection(_connectionString);
            var eventTypes = await connection.QueryAsync<EventTypeAdmin>(sql);

            _logger.LogInformation("Se obtuvieron {Count} tipos de eventos", eventTypes.Count());
            return eventTypes.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de eventos");
            throw;
        }
    }

    /// <summary>
    /// Obtiene un tipo de evento por su ID
    /// </summary>
    public async Task<EventTypeAdmin?> GetEventTypeByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Obteniendo tipo de evento con ID: {Id}", id);

            const string sql = @"
                SELECT 
                    id AS Id,
                    name AS Name,
                    description AS Description,
                    color AS Color,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM event_types
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            var eventType = await connection.QueryFirstOrDefaultAsync<EventTypeAdmin>(sql, new { Id = id });

            if (eventType == null)
            {
                _logger.LogWarning("No se encontró tipo de evento con ID: {Id}", id);
            }

            return eventType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipo de evento con ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Crea un nuevo tipo de evento
    /// </summary>
    public async Task<int> CreateEventTypeAsync(EventTypeAdmin eventType)
    {
        try
        {
            _logger.LogInformation("Creando nuevo tipo de evento: {Name}", eventType.Name);

            const string sql = @"
                INSERT INTO event_types (name, description, color, is_active, created_at)
                VALUES (@Name, @Description, @Color, @IsActive, NOW())
                RETURNING id";

            using var connection = new NpgsqlConnection(_connectionString);
            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                eventType.Name,
                eventType.Description,
                eventType.Color,
                eventType.IsActive
            });

            _logger.LogInformation("Tipo de evento creado exitosamente con ID: {Id}", id);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de evento: {Name}", eventType.Name);
            throw;
        }
    }

    /// <summary>
    /// Actualiza un tipo de evento existente
    /// </summary>
    public async Task<bool> UpdateEventTypeAsync(EventTypeAdmin eventType)
    {
        try
        {
            _logger.LogInformation("Actualizando tipo de evento ID: {Id}", eventType.Id);

            const string sql = @"
                UPDATE event_types
                SET 
                    name = @Name,
                    description = @Description,
                    color = @Color,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                eventType.Id,
                eventType.Name,
                eventType.Description,
                eventType.Color,
                eventType.IsActive
            });

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Tipo de evento actualizado exitosamente");
                return true;
            }

            _logger.LogWarning("No se actualizó ningún tipo de evento con ID: {Id}", eventType.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de evento ID: {Id}", eventType.Id);
            throw;
        }
    }

    /// <summary>
    /// Elimina un tipo de evento
    /// </summary>
    public async Task<bool> DeleteEventTypeAsync(int id)
    {
        try
        {
            _logger.LogInformation("Eliminando tipo de evento ID: {Id}", id);

            const string sql = "DELETE FROM event_types WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Tipo de evento eliminado exitosamente");
                return true;
            }

            _logger.LogWarning("No se eliminó ningún tipo de evento con ID: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tipo de evento ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Verifica si un nombre ya existe (excluyendo un ID específico)
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            string sql = "SELECT COUNT(*) FROM event_types WHERE name = @Name";

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
            _logger.LogError(ex, "Error al verificar nombre: {Name}", name);
            throw;
        }
    }
}
