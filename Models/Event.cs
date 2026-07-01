namespace IASD_Magnolia.Models;

/// <summary>
/// Modelo que representa un evento de la iglesia obtenido desde la base de datos PostgreSQL.
/// Mapea directamente con la tabla 'events' de la base de datos.
/// </summary>
/// <remarks>
/// MANTENIMIENTO:
/// - Este modelo refleja la estructura de la tabla 'events' en PostgreSQL
/// - Si se agregan columnas a la BD, agregar propiedades aquí
/// - Las propiedades nullable (?) permiten valores NULL de la BD
/// - Los nombres coinciden con snake_case de PostgreSQL (Dapper los mapea automáticamente)
/// 
/// USO:
/// - EventsService obtiene lista de eventos desde la BD
/// - Home.razor los muestra en la sección de eventos
/// 
/// CREADO: 2025-01-XX | AUTOR: Equipo IASD Magnolia
/// </remarks>
public class Event
{
    /// <summary>
    /// ID único del evento (UUID generado por PostgreSQL)
    /// PRIMARY KEY en la base de datos
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Título del evento
    /// Ejemplo: "Culto de Jóvenes", "Concierto de Alabanza"
    /// Máximo: 150 caracteres (definido en BD)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada del evento
    /// Puede contener múltiples líneas de texto
    /// Campo TEXT en PostgreSQL (sin límite)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha del evento (sin hora)
    /// Formato: YYYY-MM-DD
    /// Tipo DATE en PostgreSQL
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Hora de inicio del evento
    /// Ejemplo: 18:00:00
    /// Tipo TIME en PostgreSQL
    /// Nullable: algunos eventos pueden no tener hora específica
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Hora de finalización del evento
    /// Ejemplo: 20:00:00
    /// Tipo TIME en PostgreSQL
    /// Nullable: opcional
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Ubicación/lugar del evento
    /// Ejemplo: "Salón Principal", "Auditorio", "Patio"
    /// Máximo: 200 caracteres
    /// Nullable: puede realizarse en ubicación principal sin especificar
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// URL de la imagen del evento
    /// Puede ser ruta relativa (/img/eventos/foto.jpg) o URL completa
    /// MANTENIMIENTO: Si es NULL, se debe usar una imagen por defecto
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Indica si el evento es destacado (aparece primero)
    /// TRUE = evento importante/destacado
    /// FALSE = evento normal
    /// Default en BD: FALSE
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Indica si el evento está activo/visible
    /// TRUE = se muestra en el sitio
    /// FALSE = oculto (sin eliminar de BD)
    /// Default en BD: TRUE
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// UUID del usuario que creó el evento
    /// FOREIGN KEY a app_users(id)
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// UUID del usuario que actualizó el evento por última vez
    /// FOREIGN KEY a app_users(id)
    /// Nullable: NULL si nunca se ha actualizado
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Fecha y hora de creación del registro
    /// Default en BD: NOW()
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha y hora de última actualización
    /// Nullable: NULL si nunca se ha actualizado
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Fecha y hora de eliminación lógica (soft delete)
    /// NULL = registro activo
    /// NOT NULL = registro eliminado (pero no borrado físicamente)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // ============================================
    // PROPIEDADES CALCULADAS (No están en la BD)
    // ============================================

    /// <summary>
    /// Devuelve el día del mes para mostrar en el UI
    /// Ejemplo: Si EventDate = 2025-01-25, devuelve "25"
    /// </summary>
    public string Day => EventDate.Day.ToString("00");

    /// <summary>
    /// Devuelve el mes abreviado en mayúsculas para el UI
    /// Ejemplo: Si EventDate = 2025-01-25, devuelve "ENE"
    /// </summary>
    public string Month => EventDate.ToString("MMM").ToUpper();

    /// <summary>
    /// Devuelve la hora formateada para mostrar
    /// Ejemplo: "?? 6:00 PM" o "?? 18:00"
    /// Si no hay StartTime, devuelve string vacío
    /// </summary>
    public string FormattedTime
    {
        get
        {
            if (StartTime.HasValue)
            {
                var time = DateTime.Today.Add(StartTime.Value);
                return $"?? {time:h:mm tt}";
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Devuelve la URL de la imagen o una imagen por defecto
    /// Si ImageUrl es null/vacío, devuelve ruta a imagen placeholder
    /// </summary>
    public string ImageUrlOrDefault =>
        string.IsNullOrWhiteSpace(ImageUrl)
            ? "/img/eventos/default-event.jpg"
            : ImageUrl;
}
