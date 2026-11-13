using RabiGamaliel.Domain.Certificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo Fluent API de <see cref="Certificado"/> a la tabla <c>colegio.certificados</c>.</summary>
internal sealed class CertificadoConfiguration : IEntityTypeConfiguration<Certificado>
{
    public void Configure(EntityTypeBuilder<Certificado> builder)
    {
        builder.ToTable("certificados");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Codigo).HasMaxLength(Certificado.CodigoMaxLength).IsRequired();
        builder.HasIndex(c => c.Codigo).IsUnique().HasDatabaseName("ux_certificados_codigo");

        builder.Property(c => c.Titulo).HasMaxLength(Certificado.TituloMaxLength).IsRequired();
        builder.Property(c => c.NombreBeneficiario).HasMaxLength(Certificado.NombreMaxLength).IsRequired();
        builder.Property(c => c.NumeroDocumento).HasMaxLength(Certificado.NumeroDocumentoMaxLength).IsRequired();
        builder.Property(c => c.Grado).HasMaxLength(Certificado.GradoMaxLength);
        builder.Property(c => c.Contenido).HasMaxLength(Certificado.ContenidoMaxLength).IsRequired();
        builder.Property(c => c.FechaExpedicion).IsRequired();
        builder.Property(c => c.FirmanteNombre).HasMaxLength(Certificado.FirmanteNombreMaxLength).IsRequired();
        builder.Property(c => c.FirmanteCargo).HasMaxLength(Certificado.FirmanteCargoMaxLength).IsRequired();
        builder.Property(c => c.MotivoAnulacion).HasMaxLength(Certificado.MotivoAnulacionMaxLength);
        builder.Property(c => c.CreadoEn).IsRequired();

        // Historial de un estudiante: todos sus certificados por número de documento.
        builder.HasIndex(c => c.NumeroDocumento).HasDatabaseName("ix_certificados_numero_documento");
        builder.HasIndex(c => c.CreadoEn).IsDescending().HasDatabaseName("ix_certificados_creado");
    }
}
