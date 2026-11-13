using FluentValidation;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Application.Aula;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Application.Indicadores;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Application.Certificaciones;
using RabiGamaliel.Application.Elecciones;
using RabiGamaliel.Application.Evaluacion;
using RabiGamaliel.Application.Mensajeria;
using RabiGamaliel.Application.Noticias;
using RabiGamaliel.Application.Planeacion;
using RabiGamaliel.Application.Pqrs;
using RabiGamaliel.Application.Reportes;
using RabiGamaliel.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace RabiGamaliel.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso y validadores de la capa de aplicación.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INoticiaService, NoticiaService>();
        services.AddScoped<IPqrsService, PqrsService>();
        services.AddScoped<ICertificadoService, CertificadoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IAutenticacionService, AutenticacionService>();
        services.AddScoped<IMiembroCampusService, MiembroCampusService>();
        services.AddScoped<ICampusAutenticacionService, CampusAutenticacionService>();
        services.AddScoped<IEleccionService, EleccionService>();
        services.AddScoped<IVotacionService, VotacionService>();
        services.AddScoped<IAcudientesService, AcudientesService>();
        services.AddScoped<INotificador, Notificador>();
        services.AddScoped<IAvisosService, AvisosService>();
        services.AddScoped<IAccesoEstudiantes, AccesoEstudiantes>();
        services.AddScoped<IAcademicoService, AcademicoService>();
        services.AddScoped<IHorarioService, HorarioService>();
        services.AddScoped<IBoletinService, BoletinService>();
        services.AddScoped<ICalificacionesService, CalificacionesService>();
        services.AddScoped<IAsistenciaService, AsistenciaService>();
        services.AddScoped<IConvivenciaService, ConvivenciaService>();
        services.AddScoped<ISimulacroService, SimulacroService>();
        services.AddScoped<IBancoPreguntasService, BancoPreguntasService>();
        services.AddScoped<IPresentacionService, PresentacionService>();
        services.AddScoped<IAdmisionesService, AdmisionesService>();
        services.AddScoped<IComunicacionesService, ComunicacionesService>();
        services.AddScoped<IIndicadoresService, IndicadoresService>();
        services.AddScoped<IAlertasService, AlertasService>();
        services.AddScoped<IPushService, PushService>();
        services.AddScoped<IAulaService, AulaService>();
        services.AddScoped<IPlaneacionService, PlaneacionService>();
        services.AddScoped<IEvaluacionService, EvaluacionService>();
        services.AddScoped<IReportesService, ReportesService>();
        services.AddScoped<IMensajeriaService, MensajeriaService>();

        // Registra todos los AbstractValidator<T> del ensamblado.
        services.AddValidatorsFromAssemblyContaining<GuardarNoticiaValidator>(ServiceLifetime.Singleton);

        return services;
    }
}
