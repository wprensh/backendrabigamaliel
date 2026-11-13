using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using RabiGamaliel.Api.Endpoints;
using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Infrastructure;
using RabiGamaliel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Capas de la arquitectura limpia ----------
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ---------- Serialización JSON ----------
// Los enums viajan como texto ("Cultural", "Academica"...), más legible para el frontend.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- Errores: ProblemDetails (RFC 9457) + manejador global ----------
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- OpenAPI nativo de .NET 9 (sin Swashbuckle) ----------
builder.Services.AddOpenApi();

// ---------- Clave de administración (llave de emergencia y configuración inicial) ----------
builder.Services.Configure<OpcionesAdministracion>(builder.Configuration.GetSection(OpcionesAdministracion.Seccion));

// ---------- Inicio de sesión del panel: tokens JWT y permisos por módulo ----------
builder.Services.Configure<OpcionesAutenticacion>(builder.Configuration.GetSection(OpcionesAutenticacion.Seccion));
var claveFirma = OpcionesAutenticacion.ClaveDeFirma(builder.Configuration);
builder.Services.AddSingleton(claveFirma);
builder.Services.AddSingleton<IEmisorTokens, EmisorTokensJwt>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UsuarioActual>();
builder.Services.AddScoped<MiembroActual>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Conserva "sub" y "sello" tal como se emitieron.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = OpcionesAutenticacion.Emisor,
            ValidAudiences = [OpcionesAutenticacion.Audiencia, OpcionesAutenticacion.AudienciaCampus],
            IssuerSigningKey = claveFirma,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// ---------- Hosting detrás de un proxy (Render, Azure, etc.) ----------
// El proxy termina el HTTPS y reenvía la IP real del visitante en X-Forwarded-For.
// Sin esto, todas las visitas parecerían venir de la misma IP y compartirían el límite del formulario PQRS.
var detrasDeProxy = builder.Configuration.GetValue<bool>("DetrasDeProxy");
if (detrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1; // Solo se confía en el último salto, el que agrega el proxy.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// ---------- CORS para el frontend Angular ----------
const string FrontendCorsPolicy = "frontend";
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ---------- Rate limiting de los endpoints públicos (PQRS, verificación de certificados e inicios de sesión) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(PqrsEndpoints.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
            }));
    options.AddPolicy(CertificadosEndpoints.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
            }));
    // Además del bloqueo de la cuenta tras 5 claves erradas: frena a quien prueba muchos usuarios.
    options.AddPolicy(AutenticacionEndpoints.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
            }));
    // En la sala de informática todos los equipos salen con la misma IP pública: el límite es más amplio
    // y la protección principal es el bloqueo de cada cuenta tras 5 claves erradas.
    options.AddPolicy(CampusEndpoints.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
            }));
});

// ---------- Health checks (incluye la conexión a PostgreSQL) ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql");

var app = builder.Build();

if (detrasDeProxy)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                       // /openapi/v1.json
    app.MapScalarApiReference(options =>    // /scalar/v1 : documentación interactiva
        options.WithTitle("API Fundación Rabí Gamaliel"));
}
else
{
    app.UseHsts();
}

// Migraciones pendientes (y siembra de EF Core 9) al arrancar: siempre en desarrollo,
// y en producción cuando se activa Database__AplicarMigraciones=true (servidor único, como en Render).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:AplicarMigraciones"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Detrás de un proxy el HTTPS ya lo resuelve el proxy; redirigir aquí rompería sus chequeos de salud internos.
if (!detrasDeProxy)
{
    app.UseHttpsRedirection();
}
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();

app.MapHealthChecks("/health");
app.MapNoticiasEndpoints();
app.MapPqrsEndpoints();
app.MapCertificadosEndpoints();
app.MapAutenticacionEndpoints();
app.MapUsuariosEndpoints();
app.MapComunidadEndpoints();
app.MapEleccionesEndpoints();
app.MapCampusEndpoints();
app.MapCampusAcademicoEndpoints();
app.MapPanelAcademicoEndpoints();
app.MapComunicacionesEndpoints();
app.MapMejorasEndpoints();

await app.RunAsync();

/// <summary>Expuesta para pruebas de integración con WebApplicationFactory.</summary>
public partial class Program;
