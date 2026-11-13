using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Aula;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Certificaciones;
using RabiGamaliel.Domain.Comunicaciones;
using RabiGamaliel.Domain.Convivencia;
using RabiGamaliel.Domain.Elecciones;
using RabiGamaliel.Domain.Evaluacion;
using RabiGamaliel.Domain.Mensajeria;
using RabiGamaliel.Domain.Noticias;
using RabiGamaliel.Domain.Planeacion;
using RabiGamaliel.Domain.Pqrs;
using RabiGamaliel.Domain.Simulacros;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence;

/// <summary>
/// DbContext principal (Code-First). Implementa <see cref="IUnitOfWork"/> para que la capa
/// de aplicación confirme transacciones sin depender de EF Core.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "colegio";

    public DbSet<Noticia> Noticias => Set<Noticia>();

    public DbSet<SolicitudPqrs> SolicitudesPqrs => Set<SolicitudPqrs>();

    public DbSet<Certificado> Certificados => Set<Certificado>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<MiembroCampus> MiembrosCampus => Set<MiembroCampus>();

    public DbSet<Eleccion> Elecciones => Set<Eleccion>();

    public DbSet<Candidato> Candidatos => Set<Candidato>();

    public DbSet<ParticipacionVoto> ParticipacionesVoto => Set<ParticipacionVoto>();

    public DbSet<Voto> Votos => Set<Voto>();

    public DbSet<VinculoAcudiente> VinculosAcudiente => Set<VinculoAcudiente>();

    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();

    public DbSet<PeriodoAcademico> PeriodosAcademicos => Set<PeriodoAcademico>();

    public DbSet<AsignacionDocente> AsignacionesDocentes => Set<AsignacionDocente>();

    public DbSet<BloqueHorario> BloquesHorario => Set<BloqueHorario>();

    public DbSet<Calificacion> Calificaciones => Set<Calificacion>();

    public DbSet<TomaAsistencia> TomasAsistencia => Set<TomaAsistencia>();

    public DbSet<NovedadAsistencia> NovedadesAsistencia => Set<NovedadAsistencia>();

    public DbSet<Anotacion> Anotaciones => Set<Anotacion>();

    public DbSet<SeguimientoAnotacion> SeguimientosAnotacion => Set<SeguimientoAnotacion>();

    public DbSet<Aviso> Avisos => Set<Aviso>();

    public DbSet<Simulacro> Simulacros => Set<Simulacro>();

    public DbSet<PreguntaSimulacro> PreguntasSimulacro => Set<PreguntaSimulacro>();

    public DbSet<IntentoSimulacro> IntentosSimulacro => Set<IntentoSimulacro>();

    public DbSet<PreguntaBanco> PreguntasBanco => Set<PreguntaBanco>();

    public DbSet<Circular> Circulares => Set<Circular>();

    public DbSet<LecturaCircular> LecturasCircular => Set<LecturaCircular>();

    public DbSet<EventoCalendario> EventosCalendario => Set<EventoCalendario>();

    public DbSet<SolicitudAdmision> SolicitudesAdmision => Set<SolicitudAdmision>();

    public DbSet<DocumentoAdmision> DocumentosAdmision => Set<DocumentoAdmision>();

    public DbSet<SuscripcionPush> SuscripcionesPush => Set<SuscripcionPush>();

    public DbSet<ActividadAula> ActividadesAula => Set<ActividadAula>();

    public DbSet<EntregaAula> EntregasAula => Set<EntregaAula>();

    public DbSet<ArchivoAula> ArchivosAula => Set<ArchivoAula>();

    public DbSet<PlanEstudios> PlanesEstudio => Set<PlanEstudios>();

    public DbSet<PlaneacionClase> PlaneacionesClase => Set<PlaneacionClase>();

    public DbSet<Recuperacion> Recuperaciones => Set<Recuperacion>();

    public DbSet<ActaComision> ActasComision => Set<ActaComision>();

    public DbSet<DecisionEstudiante> DecisionesComision => Set<DecisionEstudiante>();

    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();

    public DbSet<Mensaje> Mensajes => Set<Mensaje>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Aplica todas las clases IEntityTypeConfiguration<T> del ensamblado (Fluent API).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Los enums se guardan como texto: la base de datos queda legible y estable ante reordenamientos.
        configurationBuilder.Properties<CategoriaNoticia>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<TipoPqrs>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<EstadoPqrs>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TipoCertificado>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<TipoDocumento>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<RolCampus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<CargoEleccion>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<EstadoAsistencia>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TipoAnotacion>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<EstadoCaso>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TipoAviso>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TipoPrueba>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AreaEvaluada>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DificultadPregunta>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<PublicoCircular>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TipoEvento>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<EstadoAdmision>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<TipoDocumentoAdmision>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<TipoActividad>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<EstadoPlaneacion>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<EstadoRecuperacion>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<DecisionComision>().HaveConversion<string>().HaveMaxLength(30);
    }
}
