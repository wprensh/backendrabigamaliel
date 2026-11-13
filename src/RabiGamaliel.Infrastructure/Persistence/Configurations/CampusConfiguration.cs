using RabiGamaliel.Domain.Campus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="MiembroCampus"/> a la tabla <c>colegio.miembros_campus</c>.</summary>
internal sealed class MiembroCampusConfiguration : IEntityTypeConfiguration<MiembroCampus>
{
    public void Configure(EntityTypeBuilder<MiembroCampus> builder)
    {
        builder.ToTable("miembros_campus");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Documento).HasMaxLength(MiembroCampus.DocumentoMaxLength).IsRequired();
        builder.HasIndex(m => m.Documento).IsUnique().HasDatabaseName("ux_miembros_campus_documento");

        builder.Property(m => m.NombreCompleto).HasMaxLength(MiembroCampus.NombreMaxLength).IsRequired();
        builder.Property(m => m.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(m => m.HashClave).HasMaxLength(200);
        builder.Property(m => m.SelloSeguridad).IsRequired();
        builder.Property(m => m.CreadoEn).IsRequired();
        builder.Ignore(m => m.TieneClave);
        builder.Property(m => m.Correo).HasMaxLength(MiembroCampus.CorreoMaxLength);
        builder.Property(m => m.Telefono).HasMaxLength(MiembroCampus.TelefonoMaxLength);
        builder.Property(m => m.CodigoCarnet).HasMaxLength(MiembroCampus.CodigoCarnetLength).IsRequired();
        builder.HasIndex(m => m.CodigoCarnet).IsUnique().HasDatabaseName("ux_miembros_campus_codigo_carnet");

        // Listados y conteos por rol, grado y grupo.
        builder.HasIndex(m => new { m.Rol, m.Grado, m.Grupo }).HasDatabaseName("ix_miembros_campus_rol_grado_grupo");
    }
}

/// <summary>Mapeo de <see cref="VinculoAcudiente"/> a la tabla <c>colegio.vinculos_acudiente</c>.</summary>
internal sealed class VinculoAcudienteConfiguration : IEntityTypeConfiguration<VinculoAcudiente>
{
    public void Configure(EntityTypeBuilder<VinculoAcudiente> builder)
    {
        builder.ToTable("vinculos_acudiente");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Parentesco).HasMaxLength(VinculoAcudiente.ParentescoMaxLength);
        builder.Property(v => v.CreadoEn).IsRequired();

        // Al eliminar al acudiente o al estudiante desaparece el vínculo.
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(v => v.AcudienteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(v => v.EstudianteId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.AcudienteId, v.EstudianteId }).IsUnique().HasDatabaseName("ux_vinculos_acudiente_acudiente_estudiante");
        builder.HasIndex(v => v.EstudianteId).HasDatabaseName("ix_vinculos_acudiente_estudiante");
    }
}
