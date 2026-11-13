using RabiGamaliel.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo Fluent API de <see cref="Usuario"/> a la tabla <c>colegio.usuarios</c>.</summary>
internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.NombreUsuario).HasMaxLength(Usuario.NombreUsuarioMaxLength).IsRequired();
        builder.HasIndex(u => u.NombreUsuario).IsUnique().HasDatabaseName("ux_usuarios_nombre_usuario");

        builder.Property(u => u.NombreCompleto).HasMaxLength(Usuario.NombreCompletoMaxLength).IsRequired();
        builder.Property(u => u.Correo).HasMaxLength(Usuario.CorreoMaxLength);
        builder.Property(u => u.HashClave).HasMaxLength(200).IsRequired();
        builder.Property(u => u.SelloSeguridad).IsRequired();
        builder.Property(u => u.CreadoEn).IsRequired();

        // Lista de permisos "Modulo:Accion" como arreglo text[] de PostgreSQL.
        builder.Ignore(u => u.Permisos);
        builder.Property<List<string>>("_permisos").HasColumnName("permisos").IsRequired();
    }
}
