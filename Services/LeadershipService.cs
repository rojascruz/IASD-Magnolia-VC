using Dapper;
using IASD_Magnolia.Models;
using Npgsql;
using Microsoft.AspNetCore.Components.Forms;
using SkiaSharp;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para gestionar líderes/recursos
/// </summary>
public class LeadershipService
{
    private readonly string _connectionString;
    private readonly ILogger<LeadershipService> _logger;
    private readonly IWebHostEnvironment _environment;

    public LeadershipService(
        IConfiguration configuration, 
        ILogger<LeadershipService> logger,
        IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada");
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Obtiene todos los líderes (activos y no eliminados)
    /// </summary>
    public async Task<List<Leadership>> GetAllLeadershipAsync()
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
                    l.start_date AS StartDate,
                    l.end_date AS EndDate,
                    l.display_order AS DisplayOrder,
                    l.is_active AS IsActive,
                    l.created_at AS CreatedAt,
                    l.updated_at AS UpdatedAt,
                    d.name AS DepartmentName
                FROM leadership l
                INNER JOIN departments d ON l.department_id = d.id
                WHERE l.deleted_at IS NULL
                ORDER BY 
                    d.display_order ASC,
                    l.display_order ASC,
                    l.full_name ASC";

            using var connection = new NpgsqlConnection(_connectionString);
            var leadership = await connection.QueryAsync<Leadership>(sql);

            _logger.LogInformation("Se obtuvieron {Count} líderes", leadership.Count());
            return leadership.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los líderes");
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
                    l.start_date AS StartDate,
                    l.end_date AS EndDate,
                    l.display_order AS DisplayOrder,
                    l.is_active AS IsActive,
                    l.created_at AS CreatedAt,
                    l.updated_at AS UpdatedAt,
                    d.name AS DepartmentName
                FROM leadership l
                INNER JOIN departments d ON l.department_id = d.id
                WHERE l.id = @Id AND l.deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Leadership>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener líder con ID {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Crea un nuevo líder
    /// </summary>
    public async Task<Guid> CreateLeadershipAsync(Leadership leadership)
    {
        try
        {
            const string sql = @"
                INSERT INTO leadership (
                    department_id, full_name, position, photo_url, biography,
                    public_email, public_phone, start_date, end_date, display_order, is_active
                )
                VALUES (
                    @DepartmentId, @FullName, @Position, @PhotoUrl, @Biography,
                    @PublicEmail, @PublicPhone, @StartDate, @EndDate, @DisplayOrder, @IsActive
                )
                RETURNING id";

            using var connection = new NpgsqlConnection(_connectionString);
            var id = await connection.ExecuteScalarAsync<Guid>(sql, leadership);

            _logger.LogInformation("Líder creado con ID {Id}: {Name}", id, leadership.FullName);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear líder {Name}", leadership.FullName);
            throw;
        }
    }

    /// <summary>
    /// Actualiza un líder existente
    /// </summary>
    public async Task<bool> UpdateLeadershipAsync(Leadership leadership)
    {
        try
        {
            const string sql = @"
                UPDATE leadership
                SET 
                    department_id = @DepartmentId,
                    full_name = @FullName,
                    position = @Position,
                    photo_url = @PhotoUrl,
                    biography = @Biography,
                    public_email = @PublicEmail,
                    public_phone = @PublicPhone,
                    start_date = @StartDate,
                    end_date = @EndDate,
                    display_order = @DisplayOrder,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @Id AND deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(sql, leadership);

            _logger.LogInformation("Líder {Id} actualizado: {Name}", leadership.Id, leadership.FullName);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar líder {Id}", leadership.Id);
            throw;
        }
    }

    /// <summary>
    /// Elimina un líder (soft delete)
    /// </summary>
    public async Task<bool> DeleteLeadershipAsync(Guid id)
    {
        try
        {
            // Obtener la foto antes de eliminar
            var leader = await GetLeadershipByIdAsync(id);

            const string sql = @"
                UPDATE leadership
                SET deleted_at = NOW()
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

            // Eliminar foto si existe
            if (leader != null && !string.IsNullOrEmpty(leader.PhotoUrl))
            {
                DeletePhotoFromFileSystem(leader.PhotoUrl);
            }

            _logger.LogInformation("Líder {Id} eliminado (soft delete)", id);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar líder {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Guarda una foto de líder y la redimensiona automáticamente
    /// </summary>
    public async Task<string> SavePhotoAsync(IBrowserFile file)
    {
        try
        {
            // Validar tamaño (máximo 10MB para archivos grandes)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Size > maxFileSize)
            {
                throw new InvalidOperationException("La imagen no puede superar los 10MB");
            }

            // Validar extensión
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Solo se permiten imágenes JPG, PNG o WEBP");
            }

            // Generar nombre único - siempre guardar como JPG optimizado
            var fileName = $"{Guid.NewGuid()}.jpg";
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "leadership");

            // Crear directorio si no existe
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fullPath = Path.Combine(uploadPath, fileName);

            // IMPORTANTE: Leer todo el archivo en memoria primero (soluciona el problema de lectura síncrona)
            byte[] imageData;
            using (var stream = file.OpenReadStream(maxFileSize))
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageData = memoryStream.ToArray();
            }

            // Ahora procesar la imagen con SkiaSharp desde los bytes en memoria
            using (var inputStream = new MemoryStream(imageData))
            using (var original = SKBitmap.Decode(inputStream))
            {
                if (original == null)
                {
                    throw new InvalidOperationException("No se pudo decodificar la imagen");
                }

                var targetSize = 400;

                // Calcular el área de recorte (crop) desde el centro
                int sourceX, sourceY, sourceSize;

                if (original.Width > original.Height)
                {
                    // Imagen horizontal - recortar los lados
                    sourceSize = original.Height;
                    sourceX = (original.Width - original.Height) / 2;
                    sourceY = 0;
                }
                else
                {
                    // Imagen vertical o cuadrada - recortar arriba/abajo
                    sourceSize = original.Width;
                    sourceX = 0;
                    sourceY = (original.Height - original.Width) / 2;
                }

                // Crear un bitmap cuadrado recortado
                using (var cropped = new SKBitmap(sourceSize, sourceSize))
                {
                    using (var canvas = new SKCanvas(cropped))
                    {
                        var sourceRect = new SKRect(sourceX, sourceY, sourceX + sourceSize, sourceY + sourceSize);
                        var destRect = new SKRect(0, 0, sourceSize, sourceSize);
                        canvas.DrawBitmap(original, sourceRect, destRect);
                    }

                    // Redimensionar a 400x400
                    using (var resized = cropped.Resize(new SKImageInfo(targetSize, targetSize), SKFilterQuality.High))
                    {
                        if (resized == null)
                        {
                            throw new InvalidOperationException("Error al redimensionar la imagen");
                        }

                        // Codificar y guardar como JPEG
                        using (var image = SKImage.FromBitmap(resized))
                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 85))
                        {
                            using (var fileStream = File.OpenWrite(fullPath))
                            {
                                data.SaveTo(fileStream);
                            }
                        }
                    }
                }
            }

            var relativePath = $"/uploads/leadership/{fileName}";
            _logger.LogInformation("Foto redimensionada y guardada: {Path}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar foto");
            throw;
        }
    }

    /// <summary>
    /// Elimina una foto del sistema de archivos
    /// </summary>
    public void DeletePhotoFromFileSystem(string photoPath)
    {
        try
        {
            if (string.IsNullOrEmpty(photoPath)) return;

            var fullPath = Path.Combine(_environment.WebRootPath, photoPath.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Foto eliminada del sistema de archivos: {Path}", photoPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar la foto: {Path}", photoPath);
        }
    }

    /// <summary>
    /// Obtiene el siguiente número de orden disponible para un departamento
    /// </summary>
    public async Task<int> GetNextDisplayOrderAsync(int departmentId)
    {
        try
        {
            const string sql = @"
                SELECT COALESCE(MAX(display_order), 0) + 10 
                FROM leadership 
                WHERE department_id = @DepartmentId AND deleted_at IS NULL";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { DepartmentId = departmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener siguiente orden de visualización");
            return 10;
        }
    }
}
