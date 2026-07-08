using IASD_Magnolia.Components;
using IASD_Magnolia.Services;
using IASDMagnolia.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);


// Configuración de Localización (Español / Inglés)
builder.Services.AddLocalization();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registrar HttpClient y el servicio de Versículo del Día
builder.Services.AddHttpClient<VerseOfTheDayService>();

// Registrar servicios de Eventos (PostgreSQL)
// Scoped: Una instancia por request/conexión de usuario
builder.Services.AddScoped<EventsService>();
builder.Services.AddScoped<EventTypesService>();

// Registrar servicio de Recursos (Departamentos y Liderazgo)
builder.Services.AddScoped<ResourcesService>();
builder.Services.AddScoped<DepartmentsService>();
builder.Services.AddScoped<LeadershipService>();

// Registrar servicio de autenticación
builder.Services.AddScoped<AuthService>();

// Registrar servicio de estado de autenticación como Singleton para mantener el estado entre circuitos
// NOTA: En producción, esto solo funciona para un único usuario por servidor.
// Para multi-usuario, se debe usar una solución diferente como cookies o JWT
builder.Services.AddScoped<AuthStateService>();

builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<TestService>();
// Agregar servicios de sesión protegida
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configuración de idiomas soportados
var supportedCultures = new[]
{
    new CultureInfo("es"),
    new CultureInfo("en")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

app.UseRequestLocalization(localizationOptions);

app.MapGet("/set-culture", (string culture, string redirectUri, HttpContext context) =>
{
    if (culture != "es" && culture != "en")
    {
        culture = "es";
    }

    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });

    if (string.IsNullOrWhiteSpace(redirectUri) || !redirectUri.StartsWith("/"))
    {
        redirectUri = "/";
    }

    return Results.LocalRedirect(redirectUri);
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
