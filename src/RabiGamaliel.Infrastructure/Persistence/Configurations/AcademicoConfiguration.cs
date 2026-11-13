using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Campus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

internal sealed class AsignaturaConfiguration : IEntityTypeConfiguration<Asignatura>
{
    public void Configure(EntityTypeBuilder<Asignatura> builder)
    {
        builder.ToTable("asignaturas");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Nombre).HasMaxLength(Asignatura.NombreMaxLength).IsRequired();
        builder.Property(a => a.Area).HasMaxLength(Asignatura.AreaMaxLength).IsRequired();
        builder.Property(a => a.CreadoEn).IsRequired();
        builder.HasIndex(a => a.Nombre).IsUnique().HasDatabaseName("ux_asignaturas_nombre");
    }
}

internal sealed class PeriodoAcademicoConfiguration : IEntityTypeConfiguration<PeriodoAcademico>
{
    public void Configure(EntityTypeBuilder<PeriodoAcademico> builder)
    {
        builder.ToTable("periodos_academicos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Nombre).HasMaxLength(PeriodoAcademico.NombreMaxLength).IsRequired();
        builder.Property(p => p.CreadoEn).IsRequired();
        builder.HasIndex(p => new { p.Anio, p.Numero }).IsUnique().HasDatabaseName("ux_periodos_academicos_anio_numero");
    }
}

internal sealed class AsignacionDocenteConfiguration : IEntityTypeConfiguration<AsignacionDocente>
{
    public void Configure(EntityTypeBuilder<AsignacionDocente> builder)
    {
        builder.ToTable("asignaciones_docentes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(a => a.CreadoEn).IsRequired();

        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(a => a.DocenteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Asignatura>().WithMany().HasForeignKey(a => a.AsignaturaId).OnDelete(DeleteBehavior.Restrict);

        // Una asignatura tiene un solo docente por curso y año (el grupo vacío cuenta como un valor más).
        builder.HasIndex(a => new { a.Anio, a.AsignaturaId, a.Grado, a.Grupo }).IsUnique().AreNullsDistinct(false)
            .HasDatabaseName("ux_asignaciones_docentes_curso");
        builder.HasIndex(a => new { a.Anio, a.DocenteId }).HasDatabaseName("ix_asignaciones_docentes_docente");
    }
}

internal sealed class BloqueHorarioConfiguration : IEntityTypeConfiguration<BloqueHorario>
{
    public void Configure(EntityTypeBuilder<BloqueHorario> builder)
    {
        builder.ToTable("bloques_horario", t =>
            t.HasCheckConstraint("ck_bloques_horario_franja", "dia BETWEEN 1 AND 6 AND inicio < fin"));
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.CreadoEn).IsRequired();
        builder.Ignore(b => b.Horas);

        // Si se elimina la asignación de la carga, sus clases salen del horario.
        builder.HasOne<AsignacionDocente>().WithMany().HasForeignKey(b => b.AsignacionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(b => b.AsignacionId).HasDatabaseName("ix_bloques_horario_asignacion");
    }
}

internal sealed class CalificacionConfiguration : IEntityTypeConfiguration<Calificacion>
{
    public void Configure(EntityTypeBuilder<Calificacion> builder)
    {
        builder.ToTable("calificaciones");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Nota).HasPrecision(3, 1);
        builder.Property(c => c.Observacion).HasMaxLength(Calificacion.ObservacionMaxLength);
        builder.Property(c => c.RegistradaPor).HasMaxLength(Calificacion.RegistradaPorMaxLength).IsRequired();
        builder.Property(c => c.CreadoEn).IsRequired();
        builder.Ignore(c => c.EstaVacia);

        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(c => c.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Asignatura>().WithMany().HasForeignKey(c => c.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PeriodoAcademico>().WithMany().HasForeignKey(c => c.PeriodoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.EstudianteId, c.AsignaturaId, c.PeriodoId }).IsUnique().HasDatabaseName("ux_calificaciones_estudiante_asignatura_periodo");
        builder.HasIndex(c => c.PeriodoId).HasDatabaseName("ix_calificaciones_periodo");
    }
}

internal sealed class TomaAsistenciaConfiguration : IEntityTypeConfiguration<TomaAsistencia>
{
    public void Configure(EntityTypeBuilder<TomaAsistencia> builder)
    {
        builder.ToTable("tomas_asistencia");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(t => t.CreadoEn).IsRequired();

        builder.HasOne<AsignacionDocente>().WithMany().HasForeignKey(t => t.AsignacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(t => t.Novedades).WithOne().HasForeignKey(n => n.TomaId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Novedades).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => new { t.AsignacionId, t.Fecha }).IsUnique().HasDatabaseName("ux_tomas_asistencia_asignacion_fecha");
        builder.HasIndex(t => new { t.Fecha, t.Grado }).HasDatabaseName("ix_tomas_asistencia_fecha_grado");
    }
}

internal sealed class NovedadAsistenciaConfiguration : IEntityTypeConfiguration<NovedadAsistencia>
{
    public void Configure(EntityTypeBuilder<NovedadAsistencia> builder)
    {
        builder.ToTable("novedades_asistencia");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Observacion).HasMaxLength(NovedadAsistencia.ObservacionMaxLength);
        builder.Property(n => n.CreadoEn).IsRequired();

        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(n => n.EstudianteId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.TomaId, n.EstudianteId }).IsUnique().HasDatabaseName("ux_novedades_asistencia_toma_estudiante");
        builder.HasIndex(n => n.EstudianteId).HasDatabaseName("ix_novedades_asistencia_estudiante");
    }
}
