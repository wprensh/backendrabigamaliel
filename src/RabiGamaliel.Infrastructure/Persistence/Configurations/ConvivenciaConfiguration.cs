using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

internal sealed class AnotacionConfiguration : IEntityTypeConfiguration<Anotacion>
{
    public void Configure(EntityTypeBuilder<Anotacion> builder)
    {
        builder.ToTable("anotaciones");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(a => a.Descripcion).HasMaxLength(Anotacion.DescripcionMaxLength).IsRequired();
        builder.Property(a => a.Compromisos).HasMaxLength(Anotacion.CompromisosMaxLength);
        builder.Property(a => a.AutorNombre).HasMaxLength(Anotacion.AutorMaxLength).IsRequired();
        builder.Property(a => a.EnteradoPor).HasMaxLength(Anotacion.AutorMaxLength);
        builder.Property(a => a.CreadoEn).IsRequired();
        builder.Ignore(a => a.EsSituacion);

        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(a => a.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(a => a.Seguimientos).WithOne().HasForeignKey(s => s.AnotacionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Seguimientos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(a => a.EstudianteId).HasDatabaseName("ix_anotaciones_estudiante");
        builder.HasIndex(a => new { a.Estado, a.Tipo }).HasDatabaseName("ix_anotaciones_estado_tipo");
        builder.HasIndex(a => a.Fecha).HasDatabaseName("ix_anotaciones_fecha");
    }
}

internal sealed class SeguimientoAnotacionConfiguration : IEntityTypeConfiguration<SeguimientoAnotacion>
{
    public void Configure(EntityTypeBuilder<SeguimientoAnotacion> builder)
    {
        builder.ToTable("seguimientos_anotacion");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Texto).HasMaxLength(SeguimientoAnotacion.TextoMaxLength).IsRequired();
        builder.Property(s => s.AutorNombre).HasMaxLength(Anotacion.AutorMaxLength).IsRequired();
        builder.Property(s => s.CreadoEn).IsRequired();
    }
}

internal sealed class AvisoConfiguration : IEntityTypeConfiguration<Aviso>
{
    public void Configure(EntityTypeBuilder<Aviso> builder)
    {
        builder.ToTable("avisos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Titulo).HasMaxLength(Aviso.TituloMaxLength).IsRequired();
        builder.Property(a => a.Mensaje).HasMaxLength(Aviso.MensajeMaxLength).IsRequired();
        builder.Property(a => a.Enlace).HasMaxLength(Aviso.EnlaceMaxLength);
        builder.Property(a => a.CreadoEn).IsRequired();

        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(a => a.DestinatarioId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.DestinatarioId, a.CreadoEn }).HasDatabaseName("ix_avisos_destinatario_creado");
    }
}
