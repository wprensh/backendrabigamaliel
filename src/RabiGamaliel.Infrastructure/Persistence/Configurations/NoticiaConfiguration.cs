using RabiGamaliel.Domain.Noticias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo Fluent API de <see cref="Noticia"/> a la tabla <c>colegio.noticias</c>.</summary>
internal sealed class NoticiaConfiguration : IEntityTypeConfiguration<Noticia>
{
    public void Configure(EntityTypeBuilder<Noticia> builder)
    {
        builder.ToTable("noticias");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever(); // Guid v7 generado en el dominio.

        builder.Property(n => n.Titulo)
            .HasMaxLength(Noticia.TituloMaxLength)
            .IsRequired();

        builder.Property(n => n.Resumen)
            .HasMaxLength(Noticia.ResumenMaxLength)
            .IsRequired();

        builder.Property(n => n.Contenido)
            .HasMaxLength(Noticia.ContenidoMaxLength)
            .IsRequired();

        builder.Property(n => n.ImagenUrl)
            .HasMaxLength(Noticia.ImagenUrlMaxLength);

        builder.Property(n => n.FechaPublicacion).IsRequired();
        builder.Property(n => n.Publicada).IsRequired();
        builder.Property(n => n.CreadoEn).IsRequired();

        // Título único. El servicio además valida duplicados sin distinguir mayúsculas.
        builder.HasIndex(n => n.Titulo)
            .IsUnique()
            .HasDatabaseName("ux_noticias_titulo");

        // Índice compuesto para el listado público: publicadas, más recientes primero.
        builder.HasIndex(n => new { n.Publicada, n.FechaPublicacion })
            .IsDescending(false, true)
            .HasDatabaseName("ix_noticias_publicada_fecha");
    }
}
