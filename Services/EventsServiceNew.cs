using IASD_Magnolia.Models;
using Npgsql;
using Dapper;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio actualizado para gestionar eventos con tipos desde PostgreSQL.
/// </summary>
public class EventsServiceNew
{
    private readonly string _connectionString;
    private readonly ILogger<EventsServiceNew> _logger;

    public EventsServiceNew(IConfiguration configuration, ILogger<EventsServiceNew> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string 'PostgreSQL' no encontrada");
        _logger = logger;
    }

    /// <summary>
    /// Obtiene los próximos eventos con su información de tipo
    /// </summary>
    public async Task<List<EventWithType>> GetUpcomingEventsWithTypeAsync(int limit = 12)
    {
        try
        {
            _logger.LogInformation("Obteniendo próximos {Limit} eventos con tipos desde PostgreSQL", limit);

            const string sql = @"
                SELECT 
                    e.id AS Id,
                    e.event_type_id AS EventTypeId,
                    e.title AS Title,
                    e.short_description AS ShortDescription,
                    e.description AS Description,
                    e.event_date AS EventDate,
                    e.start_time AS StartTime,
                    e.end_time AS EndTime,
                    e.location AS Location,
                    e.address AS Address,
                    e.image_url AS ImageUrl,
                    e.is_featured AS IsFeatured,
                    e.is_active AS IsActive,
                    et.name AS EventTypeName,
                    et.color AS EventTypeColor
                FROM events e
                INNER JOIN event_types et ON e.event_type_id = et.id
                WHERE e.is_active = true
                    AND e.deleted_at IS NULL
                    AND e.event_date >= CURRENT_DATE
                    AND et.is_active = true
                ORDER BY 
                    e.is_featured DESC,
                    e.event_date ASC,
                    e.start_time ASC
                LIMIT @Limit";

            using var connection = new NpgsqlConnection(_connectionString);


            var events = await connection.QueryAsync<EventWithType>(sql, new { Limit = limit });
            var eventList = events.ToList();


          

            _logger.LogInformation("Se obtuvieron {Count} eventos con tipos", eventList.Count);
            return eventList;
        }
        catch (Exception ex)
        {
          
          
            _logger.LogError(ex, "Error al obtener eventos con tipos desde PostgreSQL");
            return new List<EventWithType>();
        }
    }

    /// <summary>
    /// Obtiene eventos filtrados por tipo
    /// </summary>
    public async Task<List<EventWithType>> GetEventsByTypeAsync(string typeSlug, int limit = 20)
    {
        try
        {
            const string sql = @"
                SELECT 
                    e.id AS Id,
                    e.event_type_id AS EventTypeId,
                    e.title AS Title,
                    e.slug AS Slug,
                    e.short_description AS ShortDescription,
                    e.description AS Description,
                    e.event_date AS EventDate,
                    e.start_time AS StartTime,
                    e.end_time AS EndTime,
                    e.location AS Location,
                    e.address AS Address,
                    e.image_url AS ImageUrl,
                    e.is_featured AS IsFeatured,
                    e.is_active AS IsActive,
                    et.name AS EventTypeName,
                    et.slug AS EventTypeSlug,
                    et.color AS EventTypeColor
                FROM events e
                INNER JOIN event_types et ON e.event_type_id = et.id
                WHERE e.is_active = true
                    AND e.deleted_at IS NULL
                    AND e.event_date >= CURRENT_DATE
                    AND et.is_active = true
                    AND et.slug = @TypeSlug
                ORDER BY 
                    e.event_date ASC,
                    e.start_time ASC
                LIMIT @Limit";

            using var connection = new NpgsqlConnection(_connectionString);
            var events = await connection.QueryAsync<EventWithType>(sql, new { TypeSlug = typeSlug, Limit = limit });

            return events.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener eventos por tipo");
            return new List<EventWithType>();
        }
    }

    /// <summary>
    /// Obtiene todos los tipos de eventos activos
    /// </summary>
    public async Task<List<EventType>> GetEventTypesAsync()
    {
        try
        {
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
                WHERE is_active = true
                ORDER BY name ASC";

            using var connection = new NpgsqlConnection(_connectionString);

            _logger.LogInformation("EventsServiceNew: Obteniendo tipos de eventos...");
            var types = await connection.QueryAsync<EventType>(sql);
            var typesList = types.ToList();

            _logger.LogInformation("EventsServiceNew: Se obtuvieron {Count} tipos de eventos", typesList.Count);
            if (typesList.Any())
            {
                _logger.LogInformation("EventsServiceNew: Tipos: {Types}", string.Join(", ", typesList.Select(t => t.Name)));
            }

            return typesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventsServiceNew: ❌ ERROR al obtener tipos de eventos");
            return new List<EventType>();
        }
    }

    /// <summary>
    /// Obtiene un evento por su slug
    /// </summary>
    public async Task<EventWithType?> GetEventBySlugAsync(string slug)
    {
        try
        {
            const string sql = @"
                SELECT 
                    e.id AS Id,
                    e.event_type_id AS EventTypeId,
                    e.title AS Title,
                    e.slug AS Slug,
                    e.short_description AS ShortDescription,
                    e.description AS Description,
                    e.event_date AS EventDate,
                    e.start_time AS StartTime,
                    e.end_time AS EndTime,
                    e.location AS Location,
                    e.address AS Address,
                    e.image_url AS ImageUrl,
                    e.is_featured AS IsFeatured,
                    e.is_active AS IsActive,
                    e.registration_required AS RegistrationRequired,
                    et.name AS EventTypeName,
                    et.slug AS EventTypeSlug,
                    et.color AS EventTypeColor
                FROM events e
                INNER JOIN event_types et ON e.event_type_id = et.id
                WHERE e.slug = @Slug
                    AND e.is_active = true
                    AND e.deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            var eventData = await connection.QueryFirstOrDefaultAsync<EventWithType>(sql, new { Slug = slug });

            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener evento por slug");
            return null;
        }
    }

    // =====================================================
    // MÉTODOS DE ADMINISTRACIÓN (CRUD)
    // =====================================================

    /// <summary>
    /// Obtiene TODOS los eventos (incluyendo inactivos y pasados) para administración
    /// </summary>
    public async Task<List<EventWithType>> GetAllEventsForAdminAsync()
    {
        try
        {
            const string sql = @"
                SELECT 
                    e.id AS Id,
                    e.event_type_id AS EventTypeId,
                    e.title AS Title,
                    e.short_description AS ShortDescription,
                    e.description AS Description,
                    e.event_date AS EventDate,
                    e.start_time AS StartTime,
                    e.end_time AS EndTime,
                    e.location AS Location,
                    e.address AS Address,
                    e.image_url AS ImageUrl,
                    e.is_featured AS IsFeatured,
                    e.is_active AS IsActive,
                    e.created_at AS CreatedAt,
                    e.updated_at AS UpdatedAt,
                    et.name AS EventTypeName,
                    et.color AS EventTypeColor
                FROM events e
                INNER JOIN event_types et ON e.event_type_id = et.id
                WHERE e.deleted_at IS NULL
                ORDER BY e.created_at DESC";

            using var connection = new NpgsqlConnection(_connectionString);
            var events = await connection.QueryAsync<EventWithType>(sql);

            return events.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los eventos para admin");
            return new List<EventWithType>();
        }
    }

    /// <summary>
    /// Obtiene un evento por ID para edición
    /// </summary>
    public async Task<EventWithType?> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            const string sql = @"
                SELECT 
                    e.id AS Id,
                    e.event_type_id AS EventTypeId,
                    e.title AS Title,
                    e.short_description AS ShortDescription,
                    e.description AS Description,
                    e.event_date AS EventDate,
                    e.start_time AS StartTime,
                    e.end_time AS EndTime,
                    e.location AS Location,
                    e.address AS Address,
                    e.image_url AS ImageUrl,
                    e.is_featured AS IsFeatured,
                    e.is_active AS IsActive,
                    e.created_at AS CreatedAt,
                    et.name AS EventTypeName,
                    et.color AS EventTypeColor
                FROM events e
                INNER JOIN event_types et ON e.event_type_id = et.id
                WHERE e.id = @EventId AND e.deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EventWithType>(sql, new { EventId = eventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener evento por ID");
            return null;
        }
    }

    /// <summary>
    /// Crea un nuevo evento
    /// </summary>
    public async Task<(bool Success, string Message, Guid? EventId)> CreateEventAsync(
        int eventTypeId,
        string title,
        string? shortDescription,
        string? description,
        DateTime eventDate,
        TimeSpan? startTime,
        TimeSpan? endTime,
        string? location,
        string? address,
        string? imageUrl,
        bool isFeatured,
        bool isActive,
        Guid? createdBy)
    {
        try
        {
            const string insertSql = @"
                INSERT INTO events (
                    event_type_id, created_by, title,
                    short_description, description,
                    event_date, start_time, end_time,
                    location, address, image_url,
                    is_featured, is_active, created_at
                )
                VALUES (
                    @EventTypeId, @CreatedBy, @Title,
                    @ShortDescription, @Description,
                    @EventDate, @StartTime, @EndTime,
                    @Location, @Address, @ImageUrl,
                    @IsFeatured, @IsActive, NOW()
                )
                RETURNING id";

            using var connection = new NpgsqlConnection(_connectionString);
            var eventId = await connection.ExecuteScalarAsync<Guid>(insertSql, new
            {
                EventTypeId = eventTypeId,
                CreatedBy = createdBy,
                Title = title,
                ShortDescription = shortDescription,
                Description = description,
                EventDate = eventDate,
                StartTime = startTime,
                EndTime = endTime,
                Location = location,
                Address = address,
                ImageUrl = imageUrl,
                IsFeatured = isFeatured,
                IsActive = isActive
            });

            _logger.LogInformation("Evento creado exitosamente: {EventId}", eventId);
            return (true, "Evento creado exitosamente", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear evento");
            return (false, $"Error al crear evento: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Actualiza un evento existente
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateEventAsync(
        Guid eventId,
        int eventTypeId,
        string title,
        string? shortDescription,
        string? description,
        DateTime eventDate,
        TimeSpan? startTime,
        TimeSpan? endTime,
        string? location,
        string? address,
        string? imageUrl,
        bool isFeatured,
        bool isActive,
        Guid? updatedBy)
    {
        try
        {
            const string updateSql = @"
                UPDATE events SET
                    event_type_id = @EventTypeId,
                    updated_by = @UpdatedBy,
                    title = @Title,
                    short_description = @ShortDescription,
                    description = @Description,
                    event_date = @EventDate,
                    start_time = @StartTime,
                    end_time = @EndTime,
                    location = @Location,
                    address = @Address,
                    image_url = @ImageUrl,
                    is_featured = @IsFeatured,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @EventId";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(updateSql, new
            {
                EventId = eventId,
                EventTypeId = eventTypeId,
                UpdatedBy = updatedBy,
                Title = title,
                ShortDescription = shortDescription,
                Description = description,
                EventDate = eventDate,
                StartTime = startTime,
                EndTime = endTime,
                Location = location,
                Address = address,
                ImageUrl = imageUrl,
                IsFeatured = isFeatured,
                IsActive = isActive
            });

            _logger.LogInformation("Evento actualizado: {EventId}", eventId);
            return (true, "Evento actualizado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar evento");
            return (false, $"Error al actualizar evento: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina un evento (soft delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteEventAsync(Guid eventId)
    {
        try
        {
            // Primero obtener la imagen del evento para eliminarla del filesystem
            const string getImageSql = "SELECT image_url FROM events WHERE id = @EventId";

            using var connection = new NpgsqlConnection(_connectionString);
            var imageUrl = await connection.QueryFirstOrDefaultAsync<string>(getImageSql, new { EventId = eventId });

            const string deleteSql = @"
                UPDATE events SET
                    deleted_at = NOW(),
                    is_active = FALSE,
                    updated_at = NOW()
                WHERE id = @EventId";

            await connection.ExecuteAsync(deleteSql, new { EventId = eventId });

            // Eliminar la imagen del filesystem si existe
            if (!string.IsNullOrEmpty(imageUrl))
            {
                DeleteImageFromFileSystem(imageUrl);
            }

            _logger.LogInformation("Evento eliminado: {EventId}", eventId);
            return (true, "Evento eliminado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar evento");
            return (false, $"Error al eliminar evento: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina una imagen del filesystem
    /// </summary>
    public void DeleteImageFromFileSystem(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            _logger.LogInformation("DeleteImageFromFileSystem: imageUrl está vacía o null, no hay nada que eliminar");
            return;
        }

        try
        {   
            _logger.LogInformation("DeleteImageFromFileSystem: Intentando eliminar imagen: {ImageUrl}", imageUrl);

            // Convertir la URL relativa a ruta física
            var relativePath = imageUrl.TrimStart('/');
            _logger.LogInformation("DeleteImageFromFileSystem: Ruta relativa: {RelativePath}", relativePath);

            // Construir la ruta completa
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            _logger.LogInformation("DeleteImageFromFileSystem: Ruta completa: {FullPath}", fullPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("✅ Imagen eliminada exitosamente del filesystem: {ImagePath}", fullPath);
            }
            else
            {
                _logger.LogWarning("⚠️ La imagen no existe en el filesystem: {FullPath}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al eliminar la imagen del filesystem: {ImageUrl}", imageUrl);
        }
    }

    /// <summary>
    /// Crea un nuevo tipo de evento
    /// </summary>
    public async Task<(bool Success, string Message, int? TypeId)> CreateEventTypeAsync(
        string name,
        string slug,
        string? description,
        string? icon,
        string? color)
    {
        try
        {
            const string checkSlugSql = "SELECT COUNT(*) FROM event_types WHERE slug = @Slug";

            using var connection = new NpgsqlConnection(_connectionString);
            var slugExists = await connection.ExecuteScalarAsync<int>(checkSlugSql, new { Slug = slug });

            if (slugExists > 0)
            {
                return (false, "El slug del tipo ya existe.", null);
            }

            const string insertSql = @"
                INSERT INTO event_types (name, slug, description, icon, color, is_active, created_at)
                VALUES (@Name, @Slug, @Description, @Icon, @Color, TRUE, NOW())
                RETURNING id";

            var typeId = await connection.ExecuteScalarAsync<int>(insertSql, new
            {
                Name = name,
                Slug = slug,
                Description = description,
                Icon = icon,
                Color = color
            });

            _logger.LogInformation("Tipo de evento creado: {TypeId}", typeId);
            return (true, "Tipo de evento creado exitosamente", typeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de evento");
            return (false, $"Error: {ex.Message}", null);
        }
    }
}
