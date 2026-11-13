using RabiGamaliel.Domain.Pqrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

internal sealed class SolicitudPqrsConfiguration : IEntityTypeConfiguration<SolicitudPqrs>
{
    public void Configure(EntityTypeBuilder<SolicitudPqrs> builder)
    {
        builder.ToTable("solicitudes_pqrs");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Radicado)
            .HasMaxLength(SolicitudPqrs.RadicadoMaxLength)
            .IsRequired();
        builder.HasIndex(s => s.Radicado).IsUnique().HasDatabaseName("ux_pqrs_radicado");

        builder.Property(s => s.NombreCompleto).HasMaxLength(SolicitudPqrs.NombreMaxLength).IsRequired();
        builder.Property(s => s.Correo).HasMaxLength(SolicitudPqrs.CorreoMaxLength).IsRequired();
        builder.Property(s => s.Mensaje).HasMaxLength(SolicitudPqrs.MensajeMaxLength).IsRequired();
        builder.Property(s => s.Respuesta).HasMaxLength(SolicitudPqrs.RespuestaMaxLength);
        builder.Property(s => s.CreadoEn).IsRequired();

        builder.HasIndex(s => new { s.Estado, s.CreadoEn }).HasDatabaseName("ix_pqrs_estado_creado");
    }
}
