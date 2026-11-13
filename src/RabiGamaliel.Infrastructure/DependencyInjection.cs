using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Application.Aula;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Certificaciones;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Application.Elecciones;
using RabiGamaliel.Application.Evaluacion;
using RabiGamaliel.Application.Mensajeria;
using RabiGamaliel.Application.Noticias;
using RabiGamaliel.Application.Indicadores;
using RabiGamaliel.Application.Planeacion;
using RabiGamaliel.Application.Pqrs;
using RabiGamaliel.Application.Reportes;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Application.Usuarios;
using RabiGamaliel.Infrastructure.Correo;
using RabiGamaliel.Infrastructure.Persistence;
using RabiGamaliel.Infrastructure.Persistence.Interceptors;
using RabiGamaliel.Infrastructure.Persistence.Repositories;
using RabiGamaliel.Infrastructure.Persistence.Seed;
using RabiGamaliel.Infrastructure.Push;
using RabiGamaliel.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RabiGamaliel.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>Registra EF Core + PostgreSQL, repositorios e interceptores.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = CadenaConexion.Normalizar(
            configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Falta la cadena de conexión '{ConnectionStringName}'."));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuditoriaInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", ApplicationDbContext.Schema);
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                })
                // Tablas y columnas en snake_case, la convención habitual en PostgreSQL.
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>())
                // EF Core 9: siembra de datos integrada en el flujo de migraciones.
                .UseSeeding((ctx, _) => DatosIniciales.Sembrar(ctx))
                .UseAsyncSeeding((ctx, _, ct) => DatosIniciales.SembrarAsync(ctx, ct));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<INoticiaRepository, NoticiaRepository>();
        services.AddScoped<IPqrsRepository, PqrsRepository>();
        services.AddScoped<ICertificadoRepository, CertificadoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IMiembroCampusRepository, MiembroCampusRepository>();
        services.AddScoped<IEleccionRepository, EleccionRepository>();
        services.AddScoped<IAcademicoRepository, AcademicoRepository>();
        services.AddScoped<IAsistenciaRepository, AsistenciaRepository>();
        services.AddScoped<IConvivenciaRepository, ConvivenciaRepository>();
        services.AddScoped<IAvisoRepository, AvisoRepository>();
        services.AddScoped<ISimulacroRepository, SimulacroRepository>();
        services.AddScoped<IBancoPreguntasRepository, BancoPreguntasRepository>();
        services.AddScoped<IAdmisionRepository, AdmisionRepository>();
        services.AddScoped<IComunicacionesRepository, ComunicacionesRepository>();
        services.AddScoped<IIndicadoresRepository, IndicadoresRepository>();
        services.AddScoped<IPushRepository, PushRepository>();
        services.AddScoped<IAulaRepository, AulaRepository>();
        services.AddScoped<IPlaneacionRepository, PlaneacionRepository>();
        services.AddScoped<IEvaluacionRepository, EvaluacionRepository>();
        services.AddScoped<IReportesRepository, ReportesRepository>();
        services.AddScoped<IMensajeriaRepository, MensajeriaRepository>();

        // Avisos por correo: cola en memoria y envío en segundo plano (sección "Correo").
        services.Configure<OpcionesCorreo>(configuration.GetSection(OpcionesCorreo.Seccion));
        services.AddSingleton<ColaCorreos>();
        services.AddSingleton<IColaCorreos>(sp => sp.GetRequiredService<ColaCorreos>());
        services.AddHostedService<EnvioCorreosService>();

        // Notificaciones push (Web Push con claves VAPID, sección "Push").
        services.Configure<OpcionesPush>(configuration.GetSection(OpcionesPush.Seccion));
        services.AddHttpClient("push", c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<ColaPush>();
        services.AddSingleton<IColaPush>(sp => sp.GetRequiredService<ColaPush>());
        services.AddHostedService<EnvioPushService>();
        services.AddSingleton<IHasherClaves>(new HasherClaves());
        services.AddSingleton<IHasherCodigosAcceso, HasherCodigosAcceso>();

        return services;
    }
}
