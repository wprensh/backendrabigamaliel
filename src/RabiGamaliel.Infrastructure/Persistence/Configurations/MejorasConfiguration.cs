using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Aula;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Evaluacion;
using RabiGamaliel.Domain.Mensajeria;
using RabiGamaliel.Domain.Planeacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

internal sealed class SuscripcionPushConfiguration : IEntityTypeConfiguration<SuscripcionPush>
{
    public void Configure(EntityTypeBuilder<SuscripcionPush> builder)
    {
        builder.ToTable("suscripciones_push");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Endpoint).HasMaxLength(SuscripcionPush.EndpointMaxLength).IsRequired();
        builder.Property(s => s.P256dh).HasMaxLength(SuscripcionPush.ClaveMaxLength).IsRequired();
        builder.Property(s => s.Auth).HasMaxLength(SuscripcionPush.ClaveMaxLength).IsRequired();
        builder.Property(s => s.CreadoEn).IsRequired();
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(s => s.MiembroId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.Endpoint).IsUnique().HasDatabaseName("ux_suscripciones_push_endpoint");
        builder.HasIndex(s => s.MiembroId).HasDatabaseName("ix_suscripciones_push_miembro");
    }
}

internal sealed class ActividadAulaConfiguration : IEntityTypeConfiguration<ActividadAula>
{
    public void Configure(EntityTypeBuilder<ActividadAula> builder)
    {
        builder.ToTable("actividades_aula");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Titulo).HasMaxLength(ActividadAula.TituloMaxLength).IsRequired();
        builder.Property(a => a.Descripcion).HasMaxLength(ActividadAula.DescripcionMaxLength);
        builder.Property(a => a.Enlace).HasMaxLength(ActividadAula.EnlaceMaxLength);
        builder.Property(a => a.CreadoEn).IsRequired();
        builder.HasOne<AsignacionDocente>().WithMany().HasForeignKey(a => a.AsignacionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PeriodoAcademico>().WithMany().HasForeignKey(a => a.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(a => a.DocenteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ArchivoAula>().WithMany().HasForeignKey(a => a.ArchivoId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(a => a.AsignacionId).HasDatabaseName("ix_actividades_aula_asignacion");
    }
}

internal sealed class EntregaAulaConfiguration : IEntityTypeConfiguration<EntregaAula>
{
    public void Configure(EntityTypeBuilder<EntregaAula> builder)
    {
        builder.ToTable("entregas_aula");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Texto).HasMaxLength(EntregaAula.TextoMaxLength);
        builder.Property(e => e.Enlace).HasMaxLength(ActividadAula.EnlaceMaxLength);
        builder.Property(e => e.Comentario).HasMaxLength(EntregaAula.ComentarioMaxLength);
        builder.Property(e => e.Nota).HasPrecision(3, 1);
        builder.Property(e => e.CreadoEn).IsRequired();
        builder.HasOne<ActividadAula>().WithMany().HasForeignKey(e => e.ActividadId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(e => e.EstudianteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ArchivoAula>().WithMany().HasForeignKey(e => e.ArchivoId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.ActividadId, e.EstudianteId }).IsUnique().HasDatabaseName("ux_entregas_aula_actividad_estudiante");
        builder.HasIndex(e => e.EstudianteId).HasDatabaseName("ix_entregas_aula_estudiante");
    }
}

internal sealed class ArchivoAulaConfiguration : IEntityTypeConfiguration<ArchivoAula>
{
    public void Configure(EntityTypeBuilder<ArchivoAula> builder)
    {
        builder.ToTable("archivos_aula");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.NombreArchivo).HasMaxLength(ArchivoAula.NombreArchivoMaxLength).IsRequired();
        builder.Property(a => a.TipoContenido).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Contenido).IsRequired();
        builder.Property(a => a.CreadoEn).IsRequired();
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(a => a.SubidoPorId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PlanEstudiosConfiguration : IEntityTypeConfiguration<PlanEstudios>
{
    public void Configure(EntityTypeBuilder<PlanEstudios> builder)
    {
        builder.ToTable("planes_estudio");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Estandares).HasMaxLength(PlanEstudios.TextoMaxLength);
        builder.Property(p => p.Dba).HasMaxLength(PlanEstudios.TextoMaxLength);
        builder.Property(p => p.Competencias).HasMaxLength(PlanEstudios.TextoMaxLength);
        builder.Property(p => p.Contenidos).HasMaxLength(PlanEstudios.TextoMaxLength);
        builder.Property(p => p.Indicadores).HasMaxLength(PlanEstudios.TextoMaxLength);
        builder.Property(p => p.ActualizadoPor).HasMaxLength(PlanEstudios.AutorMaxLength).IsRequired();
        builder.Property(p => p.CreadoEn).IsRequired();
        builder.HasOne<Asignatura>().WithMany().HasForeignKey(p => p.AsignaturaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.AsignaturaId, p.Grado, p.Periodo }).IsUnique().HasDatabaseName("ux_planes_estudio_asignatura_grado_periodo");
    }
}

internal sealed class PlaneacionClaseConfiguration : IEntityTypeConfiguration<PlaneacionClase>
{
    public void Configure(EntityTypeBuilder<PlaneacionClase> builder)
    {
        builder.ToTable("planeaciones_clase");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Tema).HasMaxLength(PlaneacionClase.TemaMaxLength).IsRequired();
        builder.Property(p => p.Aprendizajes).HasMaxLength(PlaneacionClase.TextoMaxLength);
        builder.Property(p => p.Actividades).HasMaxLength(PlaneacionClase.TextoMaxLength);
        builder.Property(p => p.Recursos).HasMaxLength(PlaneacionClase.TextoMaxLength);
        builder.Property(p => p.Evaluacion).HasMaxLength(PlaneacionClase.TextoMaxLength);
        builder.Property(p => p.Observacion).HasMaxLength(PlaneacionClase.ObservacionMaxLength);
        builder.Property(p => p.RevisadaPor).HasMaxLength(PlaneacionClase.RevisorMaxLength);
        builder.Property(p => p.CreadoEn).IsRequired();
        builder.Ignore(p => p.Editable);
        builder.HasOne<AsignacionDocente>().WithMany().HasForeignKey(p => p.AsignacionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(p => p.DocenteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PeriodoAcademico>().WithMany().HasForeignKey(p => p.PeriodoId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(p => new { p.DocenteId, p.Semana }).HasDatabaseName("ix_planeaciones_clase_docente_semana");
        builder.HasIndex(p => p.Estado).HasDatabaseName("ix_planeaciones_clase_estado");
    }
}

internal sealed class RecuperacionConfiguration : IEntityTypeConfiguration<Recuperacion>
{
    public void Configure(EntityTypeBuilder<Recuperacion> builder)
    {
        builder.ToTable("recuperaciones");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.NotaOriginal).HasPrecision(3, 1);
        builder.Property(r => r.NotaRecuperacion).HasPrecision(3, 1);
        builder.Property(r => r.NotaDefinitiva).HasPrecision(3, 1);
        builder.Property(r => r.Plan).HasMaxLength(Recuperacion.PlanMaxLength).IsRequired();
        builder.Property(r => r.Observacion).HasMaxLength(Recuperacion.ObservacionMaxLength);
        builder.Property(r => r.RegistradaPor).HasMaxLength(Recuperacion.AutorMaxLength).IsRequired();
        builder.Property(r => r.CreadoEn).IsRequired();
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(r => r.EstudianteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Asignatura>().WithMany().HasForeignKey(r => r.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PeriodoAcademico>().WithMany().HasForeignKey(r => r.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.EstudianteId, r.AsignaturaId, r.PeriodoId }).IsUnique().HasDatabaseName("ux_recuperaciones_estudiante_asignatura_periodo");
        builder.HasIndex(r => r.PeriodoId).HasDatabaseName("ix_recuperaciones_periodo");
    }
}

internal sealed class ActaComisionConfiguration : IEntityTypeConfiguration<ActaComision>
{
    public void Configure(EntityTypeBuilder<ActaComision> builder)
    {
        builder.ToTable("actas_comision");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(a => a.Integrantes).HasMaxLength(ActaComision.IntegrantesMaxLength);
        builder.Property(a => a.Observaciones).HasMaxLength(ActaComision.ObservacionesMaxLength);
        builder.Property(a => a.CreadoEn).IsRequired();
        builder.HasOne<PeriodoAcademico>().WithMany().HasForeignKey(a => a.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.Anio, a.Numero }).IsUnique().HasDatabaseName("ux_actas_comision_anio_numero");
    }
}

internal sealed class DecisionEstudianteConfiguration : IEntityTypeConfiguration<DecisionEstudiante>
{
    public void Configure(EntityTypeBuilder<DecisionEstudiante> builder)
    {
        builder.ToTable("decisiones_comision");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.NombreEstudiante).HasMaxLength(DecisionEstudiante.NombreMaxLength).IsRequired();
        builder.Property(d => d.Situacion).HasMaxLength(DecisionEstudiante.DetalleMaxLength);
        builder.Property(d => d.Observacion).HasMaxLength(DecisionEstudiante.ObservacionMaxLength);
        builder.Property(d => d.CreadoEn).IsRequired();
        builder.HasOne<ActaComision>().WithMany().HasForeignKey(d => d.ActaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(d => d.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.ActaId).HasDatabaseName("ix_decisiones_comision_acta");
    }
}

internal sealed class ConversacionConfiguration : IEntityTypeConfiguration<Conversacion>
{
    public void Configure(EntityTypeBuilder<Conversacion> builder)
    {
        builder.ToTable("conversaciones");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Asunto).HasMaxLength(Conversacion.AsuntoMaxLength).IsRequired();
        builder.Property(c => c.CreadoEn).IsRequired();
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(c => c.DocenteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(c => c.FamiliaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(c => c.EstudianteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.DocenteId).HasDatabaseName("ix_conversaciones_docente");
        builder.HasIndex(c => c.FamiliaId).HasDatabaseName("ix_conversaciones_familia");
    }
}

internal sealed class MensajeConfiguration : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> builder)
    {
        builder.ToTable("mensajes");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Texto).HasMaxLength(Mensaje.TextoMaxLength).IsRequired();
        builder.Property(m => m.CreadoEn).IsRequired();
        builder.HasOne<Conversacion>().WithMany().HasForeignKey(m => m.ConversacionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(m => m.AutorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.ConversacionId, m.CreadoEn }).HasDatabaseName("ix_mensajes_conversacion_fecha");
    }
}
