using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Comunicaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

internal sealed class CircularConfiguration : IEntityTypeConfiguration<Circular>
{
    public void Configure(EntityTypeBuilder<Circular> builder)
    {
        builder.ToTable("circulares");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Titulo).HasMaxLength(Circular.TituloMaxLength).IsRequired();
        builder.Property(c => c.Contenido).HasMaxLength(Circular.ContenidoMaxLength).IsRequired();
        builder.Property(c => c.EnlaceAdjunto).HasMaxLength(Circular.EnlaceMaxLength);
        builder.Property(c => c.CreadoEn).IsRequired();
        builder.Ignore(c => c.Codigo);
        builder.Ignore(c => c.Grados);
        builder.Property<List<int>>("_grados").HasColumnName("grados").IsRequired();
        builder.HasIndex(c => new { c.Anio, c.Numero }).IsUnique().HasDatabaseName("ux_circulares_anio_numero");
    }
}

internal sealed class LecturaCircularConfiguration : IEntityTypeConfiguration<LecturaCircular>
{
    public void Configure(EntityTypeBuilder<LecturaCircular> builder)
    {
        builder.ToTable("lecturas_circular");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.CreadoEn).IsRequired();
        builder.HasOne<Circular>().WithMany().HasForeignKey(l => l.CircularId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(l => l.MiembroId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => new { l.CircularId, l.MiembroId }).IsUnique().HasDatabaseName("ux_lecturas_circular_circular_miembro");
        builder.HasIndex(l => l.MiembroId).HasDatabaseName("ix_lecturas_circular_miembro");
    }
}

internal sealed class EventoCalendarioConfiguration : IEntityTypeConfiguration<EventoCalendario>
{
    public void Configure(EntityTypeBuilder<EventoCalendario> builder)
    {
        builder.ToTable("eventos_calendario");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Titulo).HasMaxLength(EventoCalendario.TituloMaxLength).IsRequired();
        builder.Property(e => e.Descripcion).HasMaxLength(EventoCalendario.DescripcionMaxLength);
        builder.Property(e => e.Hora).HasMaxLength(EventoCalendario.HoraMaxLength);
        builder.Property(e => e.Lugar).HasMaxLength(EventoCalendario.LugarMaxLength);
        builder.Property(e => e.CreadoEn).IsRequired();
        builder.HasIndex(e => new { e.Inicio, e.Fin }).HasDatabaseName("ix_eventos_calendario_fechas");
    }
}

internal sealed class SolicitudAdmisionConfiguration : IEntityTypeConfiguration<SolicitudAdmision>
{
    public void Configure(EntityTypeBuilder<SolicitudAdmision> builder)
    {
        builder.ToTable("solicitudes_admision");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Radicado).HasMaxLength(SolicitudAdmision.RadicadoLength).IsRequired();
        builder.Property(s => s.AspiranteDocumento).HasMaxLength(SolicitudAdmision.DocumentoMaxLength).IsRequired();
        builder.Property(s => s.AspiranteNombres).HasMaxLength(SolicitudAdmision.NombreMaxLength).IsRequired();
        builder.Property(s => s.AspiranteApellidos).HasMaxLength(SolicitudAdmision.NombreMaxLength).IsRequired();
        builder.Property(s => s.ColegioProcedencia).HasMaxLength(SolicitudAdmision.TextoCortoMaxLength);
        builder.Property(s => s.Direccion).HasMaxLength(SolicitudAdmision.TextoCortoMaxLength);
        builder.Property(s => s.Eps).HasMaxLength(SolicitudAdmision.TextoCortoMaxLength);
        builder.Property(s => s.AcudienteNombre).HasMaxLength(MiembroCampus.NombreMaxLength).IsRequired();
        builder.Property(s => s.AcudienteDocumento).HasMaxLength(SolicitudAdmision.DocumentoMaxLength).IsRequired();
        builder.Property(s => s.AcudienteParentesco).HasMaxLength(VinculoAcudiente.ParentescoMaxLength);
        builder.Property(s => s.AcudienteTelefono).HasMaxLength(SolicitudAdmision.TelefonoMaxLength).IsRequired();
        builder.Property(s => s.AcudienteCorreo).HasMaxLength(SolicitudAdmision.CorreoMaxLength);
        builder.Property(s => s.Observaciones).HasMaxLength(SolicitudAdmision.ObservacionesMaxLength);
        builder.Property(s => s.MensajeFamilia).HasMaxLength(SolicitudAdmision.MensajeMaxLength);
        builder.Property(s => s.NotaInterna).HasMaxLength(SolicitudAdmision.MensajeMaxLength);
        builder.Property(s => s.CreadoEn).IsRequired();
        builder.Ignore(s => s.NombreAspirante);
        builder.Ignore(s => s.AdmiteDocumentos);

        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(s => s.EstudianteId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.Radicado).IsUnique().HasDatabaseName("ux_solicitudes_admision_radicado");
        builder.HasIndex(s => new { s.AspiranteDocumento, s.AnioLectivo }).HasDatabaseName("ix_solicitudes_admision_aspirante");
        builder.HasIndex(s => new { s.AnioLectivo, s.Estado }).HasDatabaseName("ix_solicitudes_admision_anio_estado");
    }
}

internal sealed class DocumentoAdmisionConfiguration : IEntityTypeConfiguration<DocumentoAdmision>
{
    public void Configure(EntityTypeBuilder<DocumentoAdmision> builder)
    {
        builder.ToTable("documentos_admision");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.NombreArchivo).HasMaxLength(DocumentoAdmision.NombreArchivoMaxLength).IsRequired();
        builder.Property(d => d.TipoContenido).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Contenido).IsRequired();
        builder.Property(d => d.CreadoEn).IsRequired();
        builder.HasOne<SolicitudAdmision>().WithMany().HasForeignKey(d => d.SolicitudId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.SolicitudId).HasDatabaseName("ix_documentos_admision_solicitud");
    }
}
