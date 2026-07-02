using IASD_Magnolia.Models;
using Npgsql;
using Dapper;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para gestionar eventos de la iglesia desde PostgreSQL.
/// Obtiene eventos activos para mostrar en la página principal y otras páginas.
/// </summary>
/// <remarks>
/// MANTENIMIENTO:
/// 
/// CÓMO FUNCIONA:
/// 1. Se conecta a PostgreSQL usando Npgsql
/// 2. Ejecuta queries SQL usando Dapper (micro-ORM)
/// 3. Mapea resultados automáticamente a objetos Event
/// 4. Filtra solo eventos activos y no eliminados
/// 5. Ordena por: destacados primero, luego por fecha
/// 
/// CONFIGURACIÓN:
/// - Connection string debe estar en appsettings.json
/// - Clave: "ConnectionStrings:PostgreSQL"
/// 
/// AGREGAR MÁS MÉTODOS:
/// - GetEventByIdAsync(Guid id) - Para página de detalle
/// - GetEventsByCategoryAsync(string category) - Si agregas categorías
/// - SearchEventsAsync(string query) - Para búsqueda
/// 
/// DEPENDENCIAS:
/// - Npgsql: Driver de PostgreSQL para .NET
/// - Dapper: Micro-ORM para mapeo objeto-relacional
/// - IConfiguration: Para obtener connection string
/// - ILogger: Para logging
/// 
/// CREADO: 2025-01-XX | AUTOR: Equipo IASD Magnolia
/// </remarks>
public class EventsService
{
    private readonly string _connectionString;
    private readonly ILogger<EventsService> _logger;

    /// <summary>
    /// Constructor - Inyección de dependencias automática
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación (appsettings.json)</param>
    /// <param name="logger">Logger para registrar eventos y errores</param>
    public EventsService(IConfiguration configuration, ILogger<EventsService> logger)
    {
        // Obtener connection string desde appsettings.json
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'PostgreSQL' no encontrada en appsettings.json");

        _logger = logger;
    }

    /// <summary>
    /// Obtiene los próximos eventos activos para mostrar en la página principal.
    /// Máximo 4 eventos, ordenados por: destacados primero, luego por fecha más cercana.
    /// </summary>
    /// <param name="limit">Cantidad máxima de eventos a obtener (default: 4)</param>
    /// <returns>Lista de eventos activos y no eliminados</returns>
    /// <remarks>
    /// FILTROS APLICADOS:
    /// - is_active = true (solo eventos activos)
    /// - deleted_at IS NULL (no eliminados)
    /// - event_date >= HOY (solo eventos futuros o de hoy)
    /// 
    /// ORDEN:
    /// 1. is_featured DESC (destacados primero)
    /// 2. event_date ASC (fecha más cercana primero)
    /// 3. start_time ASC (hora más temprana primero)
    /// 
    /// EJEMPLO DE USO:
    /// var events = await eventsService.GetUpcomingEventsAsync(4);
    /// </remarks>
    public async Task<List<Event>> GetUpcomingEventsAsync(int limit = 4)
    {
        try
        {
            _logger.LogInformation("Obteniendo próximos {Limit} eventos desde PostgreSQL", limit);

            // Query SQL con aliases para mapear snake_case (BD) a PascalCase (C#)
            const string sql = @"
                SELECT 
                    id AS Id,
                    title AS Title,
                    description AS Description,
                    event_date AS EventDate,
                    start_time AS StartTime,
                    end_time AS EndTime,
                    location AS Location,
                    image_url AS ImageUrl,
                    is_featured AS IsFeatured,
                    is_active AS IsActive,
                    created_by AS CreatedBy,
                    updated_by AS UpdatedBy,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt,
                    deleted_at AS DeletedAt
                FROM events
                WHERE 
                    is_active = true 
                    AND deleted_at IS NULL
                    AND event_date >= CURRENT_DATE
                ORDER BY 
                    is_featured DESC,
                    event_date ASC,
                    start_time ASC
                LIMIT @Limit";

            // Conectar a PostgreSQL y ejecutar query
            using var connection = new NpgsqlConnection(_connectionString);

            // Dapper ejecuta el query y mapea automáticamente a List<Event>
            var events = await connection.QueryAsync<Event>(sql, new { Limit = limit });

            var eventsList = events.ToList();

            _logger.LogInformation(
                "Se obtuvieron {Count} eventos exitosamente", 
                eventsList.Count);

            return eventsList;
        }
        catch (NpgsqlException ex)
        {
            // Error específico de PostgreSQL (conexión, permisos, etc.)
            _logger.LogError(ex, 
                "Error de PostgreSQL al obtener eventos: {Message}", 
                ex.Message);

            // Devolver lista vacía en lugar de lanzar excepción
            // La UI mostrará "No hay eventos" en lugar de error
            return new List<Event>();
        }
        catch (Exception ex)
        {
            // Cualquier otro error
            _logger.LogError(ex, 
                "Error inesperado al obtener eventos: {Message}", 
                ex.Message);

            return new List<Event>();
        }
    }

    /// <summary>
    /// Obtiene TODOS los eventos activos (para la página de eventos completa).
    /// Sin límite de cantidad, ordenados por fecha.
    /// </summary>
    /// <returns>Lista completa de eventos activos</returns>
    /// <remarks>
    /// MANTENIMIENTO:
    /// - Usar este método en la página /eventos
    /// - Considerar paginación si hay muchos eventos (>50)
    /// </remarks>
    public async Task<List<Event>> GetAllEventsAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo TODOS los eventos desde PostgreSQL");

            const string sql = @"
                SELECT 
                    id AS Id,
                    title AS Title,
                    description AS Description,
                    event_date AS EventDate,
                    start_time AS StartTime,
                    end_time AS EndTime,
                    location AS Location,
                    image_url AS ImageUrl,
                    is_featured AS IsFeatured,
                    is_active AS IsActive,
                    created_by AS CreatedBy,
                    updated_by AS UpdatedBy,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt,
                    deleted_at AS DeletedAt
                FROM events
                WHERE 
                    is_active = true 
                    AND deleted_at IS NULL
                    AND event_date >= CURRENT_DATE
                ORDER BY 
                    is_featured DESC,
                    event_date ASC,
                    start_time ASC";

            using var connection = new NpgsqlConnection(_connectionString);
            var events = await connection.QueryAsync<Event>(sql);
            var eventsList = events.ToList();

            _logger.LogInformation("Se obtuvieron {Count} eventos totales", eventsList.Count);

            return eventsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los eventos");
            return new List<Event>();
        }
    }

    /// <summary>
    /// Obtiene un evento específico por su ID.
    /// Útil para página de detalle del evento.
    /// </summary>
    /// <param name="id">UUID del evento</param>
    /// <returns>Evento encontrado o null si no existe</returns>
    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Obteniendo evento con ID: {EventId}", id);

            const string sql = @"
                SELECT 
                    id AS Id,
                    title AS Title,
                    description AS Description,
                    event_date AS EventDate,
                    start_time AS StartTime,
                    end_time AS EndTime,
                    location AS Location,
                    image_url AS ImageUrl,
                    is_featured AS IsFeatured,
                    is_active AS IsActive,
                    created_by AS CreatedBy,
                    updated_by AS UpdatedBy,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt,
                    deleted_at AS DeletedAt
                FROM events
                WHERE 
                    id = @Id 
                    AND is_active = true 
                    AND deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);

            // QueryFirstOrDefaultAsync devuelve null si no encuentra nada
            var evento = await connection.QueryFirstOrDefaultAsync<Event>(sql, new { Id = id });

            if (evento != null)
            {
                _logger.LogInformation("Evento encontrado: {Title}", evento.Title);
            }
            else
            {
                _logger.LogWarning("No se encontró evento con ID: {EventId}", id);
            }

            return evento;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener evento por ID: {EventId}", id);
            return null;
        }
    }

    /// <summary>
    /// Verifica la conexión a la base de datos.
    /// Útil para health checks y troubleshooting.
    /// </summary>
    /// <returns>True si la conexión es exitosa, False si falla</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Probando conexión a PostgreSQL...");

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query simple para verificar conexión
            var result = await connection.ExecuteScalarAsync<int>("SELECT 1");

            _logger.LogInformation("? Conexión a PostgreSQL exitosa");
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al conectar a PostgreSQL: {Message}", ex.Message);
            return false;
        }
    }
}
