using IASD_Magnolia.Models;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace IASD_Magnolia.Services;

/// <summary>
/// Servicio para obtener el versículo bíblico del día desde verseoftheday.com
/// Incluye sistema de fallback con versículos locales en caso de error.
/// </summary>
/// <remarks>
/// MANTENIMIENTO:
/// 
/// CÓMO FUNCIONA:
/// 1. Hace un request HTTP a verseoftheday.com/es/
/// 2. Parsea el HTML usando HtmlAgilityPack
/// 3. Extrae el texto del versículo y la referencia bíblica
/// 4. Limpia el texto de referencias duplicadas
/// 5. Si falla, devuelve un versículo de respaldo local
/// 
/// CAMBIAR LA FUENTE:
/// - Modifica la constante TargetUrl
/// - Actualiza los selectores XPath en ParseHtmlContent()
/// 
/// AGREGAR MÁS VERSÍCULOS DE RESPALDO:
/// - Agrega más items a la lista FallbackVerses
/// - Mantén el formato: Text, Reference, Source = "versículo local"
/// 
/// DEPENDENCIAS:
/// - HtmlAgilityPack: Para parsear HTML
/// - HttpClient: Inyectado automáticamente por .NET
/// - ILogger: Para logging y debugging
/// 
/// CREADO: 2025-01-XX | AUTOR: Equipo IASD Magnolia
/// </remarks>
public class VerseOfTheDayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VerseOfTheDayService> _logger;

    /// <summary>
    /// URL del sitio web de donde se obtienen los versículos
    /// MANTENIMIENTO: Cambia esto si quieres usar otra fuente
    /// </summary>
    private const string TargetUrl = "https://www.verseoftheday.com/es/";

    /// <summary>
    /// Versículos de respaldo que se usan cuando falla la conexión al sitio web.
    /// Se rotan por día del mes (día 1,4,7... = versículo 0 | día 2,5,8... = versículo 1, etc)
    /// MANTENIMIENTO: Agrega más versículos aquí si lo deseas
    /// </summary>
    private static readonly List<DailyVerse> FallbackVerses = new()
    {
        new DailyVerse
        {
            Text = "¿Acaso no lo sabes? ¿Es que no lo has oído? El Dios eterno, el Señor, el creador de los confines de la tierra no se fatiga ni se cansa. Su entendimiento es inescrutable. El da fuerzas al fatigado, y al que no tiene fuerzas, aumenta el vigor.",
            Reference = "Isaías 40:28-29",
            Source = "versículo local"
        },
        new DailyVerse
        {
            Text = "Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito, para que todo aquel que en él cree, no se pierda, mas tenga vida eterna.",
            Reference = "Juan 3:16",
            Source = "versículo local"
        },
        new DailyVerse
        {
            Text = "Y sabemos que a los que aman a Dios, todas las cosas les ayudan a bien, esto es, a los que conforme a su propósito son llamados.",
            Reference = "Romanos 8:28",
            Source = "versículo local"
        }
    };

    /// <summary>
    /// Constructor - Inyección de dependencias automática por .NET
    /// </summary>
    public VerseOfTheDayService(HttpClient httpClient, ILogger<VerseOfTheDayService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configurar HttpClient para simular un navegador real
        // Esto ayuda a evitar bloqueos por parte del sitio web
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9,en;q=0.8");
    }

    /// <summary>
    /// Obtiene el versículo del día desde verseoftheday.com
    /// Si falla, devuelve automáticamente un versículo de respaldo
    /// </summary>
    /// <returns>DailyVerse con el versículo obtenido o uno de respaldo</returns>
    public async Task<DailyVerse> GetDailyVerseAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo versículo del día desde {Url}", TargetUrl);

            // Hacer request HTTP al sitio web
            var html = await _httpClient.GetStringAsync(TargetUrl);

            // Parsear el HTML y extraer el versículo
            var verse = ParseHtmlContent(html);

            // Verificar que se obtuvo un versículo válido
            if (verse != null && !string.IsNullOrEmpty(verse.Text))
            {
                _logger.LogInformation("Versículo obtenido exitosamente: {Reference}", verse.Reference);
                return verse;
            }
        }
        catch (Exception ex)
        {
            // Si hay cualquier error (sin internet, sitio caído, etc), usar fallback
            _logger.LogWarning(ex, "Error al obtener versículo del día. Usando versículo de respaldo.");
        }

        // Si llegamos aquí, algo falló - devolver versículo de respaldo
        return GetFallbackVerse();
    }

    /// <summary>
    /// Parsea el HTML del sitio web para extraer el versículo y su referencia
    /// Usa múltiples selectores XPath como fallback en caso de cambios en el sitio
    /// </summary>
    /// <param name="html">HTML del sitio web</param>
    /// <returns>DailyVerse parseado o null si no se encuentra</returns>
    /// <remarks>
    /// MANTENIMIENTO:
    /// Si el sitio web cambia su estructura, actualiza estos selectores XPath.
    /// Los selectores están ordenados por prioridad (los primeros son los más actuales)
    /// </remarks>
    private DailyVerse? ParseHtmlContent(string html)
    {
        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Selectores XPath para encontrar el TEXTO del versículo
            // MANTENIMIENTO: Si el sitio cambia, actualiza estos selectores
            var textSelectors = new[]
            {
                "//div[@class='bilingual-left']",      // Selector actual (2025)
                "//blockquote//p",                      // Alternativa común
                "//*[@class='verse-text']",             // Fallback genérico
                "//blockquote",                         // Fallback básico
                "//*[@class='daily-verse-text']"        // Fallback adicional
            };

            // Selectores XPath para encontrar la REFERENCIA (ej: Juan 3:16)
            // MANTENIMIENTO: Si el sitio cambia, actualiza estos selectores
            var refSelectors = new[]
            {
                "//div[@class='bilingual-left']//div[@class='reference']//a",  // Selector actual
                "//div[@class='reference']//a",                                 // Alternativa
                "//*[@class='verse-reference']",                                // Fallback genérico
                "//blockquote//cite",                                           // Fallback común
                "//cite"                                                        // Fallback básico
            };

            // Buscar el nodo HTML que contiene el texto del versículo
            HtmlNode? verseNode = null;
            foreach (var selector in textSelectors)
            {
                verseNode = htmlDoc.DocumentNode.SelectSingleNode(selector);
                if (verseNode != null && !string.IsNullOrWhiteSpace(verseNode.InnerText))
                    break; // Encontrado, salir del loop
            }

            // Buscar el nodo HTML que contiene la referencia bíblica
            HtmlNode? refNode = null;
            foreach (var selector in refSelectors)
            {
                refNode = htmlDoc.DocumentNode.SelectSingleNode(selector);
                if (refNode != null && !string.IsNullOrWhiteSpace(refNode.InnerText))
                    break; // Encontrado, salir del loop
            }

            // Si no se encontraron ambos elementos, no podemos continuar
            if (verseNode == null || refNode == null)
            {
                _logger.LogWarning("No se encontraron los elementos del versículo en el HTML");
                return null;
            }

            // Extraer el texto y decodificar entidades HTML (&amp; ? &, etc)
            var verseText = HtmlEntity.DeEntitize(verseNode.InnerText.Trim());
            var reference = HtmlEntity.DeEntitize(refNode.InnerText.Trim());

            // Limpiar el texto del versículo
            // A veces el sitio incluye la referencia al final del texto, la removemos
            // Ejemplos a limpiar: "...vida eterna. —Juan 3:16" o "...vida eterna (Juan 3:16)"
            verseText = Regex.Replace(verseText, @"[—\-]\s*([1-3]?\s*[A-Za-zÁ-ÿ]+\s+\d+:\d+(?:-\d+)?)\s*$", 
                "", RegexOptions.IgnoreCase).Trim();
            verseText = Regex.Replace(verseText, @"\s*\([1-3]?\s*[A-Za-zÁ-ÿ]+\s+\d+:\d+(?:-\d+)?\)\s*$", 
                "", RegexOptions.IgnoreCase).Trim();

            // Crear y devolver el modelo DailyVerse
            return new DailyVerse
            {
                Text = verseText,
                Reference = reference,
                Source = "verseoftheday.com",
                Timestamp = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear el HTML del versículo");
            return null;
        }
    }

    /// <summary>
    /// Devuelve un versículo de respaldo basado en el día actual del mes
    /// Los versículos rotan automáticamente (día 1,4,7... = v1 | día 2,5,8... = v2 | etc)
    /// </summary>
    /// <returns>DailyVerse de respaldo</returns>
    /// <remarks>
    /// MANTENIMIENTO: Si agregas más versículos a FallbackVerses, 
    /// esta rotación funcionará automáticamente
    /// </remarks>
    private DailyVerse GetFallbackVerse()
    {
        // Calcular índice basado en el día del mes
        // Esto garantiza que cada día tenga un versículo diferente
        var today = DateTime.Now.Day;
        var index = today % FallbackVerses.Count;

        _logger.LogInformation("Usando versículo de respaldo: {Reference}", FallbackVerses[index].Reference);

        return FallbackVerses[index];
    }
}
