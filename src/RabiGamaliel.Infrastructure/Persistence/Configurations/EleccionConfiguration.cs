using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Elecciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Eleccion"/> a la tabla <c>colegio.elecciones</c>.</summary>
internal sealed class EleccionConfiguration : IEntityTypeConfiguration<Eleccion>
{
    public void Configure(EntityTypeBuilder<Eleccion> builder)
    {
        builder.ToTable("elecciones");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Nombre).HasMaxLength(Eleccion.NombreMaxLength).IsRequired();
        builder.Property(e => e.Descripcion).HasMaxLength(Eleccion.DescripcionMaxLength);
        builder.Property(e => e.CreadoEn).IsRequired();

        // Cargos que se eligen, como arreglo text[] ("Personero", "VoceroGrado"...).
        builder.Ignore(e => e.Cargos);
        builder.Property<List<string>>("_cargos").HasColumnName("cargos").IsRequired();

        builder.HasIndex(e => e.Inicio).HasDatabaseName("ix_elecciones_inicio");
    }
}

/// <summary>Mapeo de <see cref="Candidato"/> a la tabla <c>colegio.candidatos</c>.</summary>
internal sealed class CandidatoConfiguration : IEntityTypeConfiguration<Candidato>
{
    public void Configure(EntityTypeBuilder<Candidato> builder)
    {
        builder.ToTable("candidatos");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.NombreCompleto).HasMaxLength(MiembroCampus.NombreMaxLength).IsRequired();
        builder.Property(c => c.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(c => c.Ambito).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Lema).HasMaxLength(Candidato.LemaMaxLength);
        builder.Property(c => c.Propuestas).HasMaxLength(Candidato.PropuestasMaxLength);
        builder.Property(c => c.FotoUrl).HasMaxLength(Candidato.FotoUrlMaxLength);
        builder.Property(c => c.CreadoEn).IsRequired();

        // Al eliminar una elección (solo posible sin votos) se van sus candidatos.
        builder.HasOne<Eleccion>().WithMany().HasForeignKey(c => c.EleccionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(c => c.EstudianteId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.EleccionId, c.Cargo, c.EstudianteId }).IsUnique()
            .HasDatabaseName("ux_candidatos_eleccion_cargo_estudiante");
        builder.HasIndex(c => new { c.EleccionId, c.Cargo, c.Ambito, c.Numero }).IsUnique()
            .HasDatabaseName("ux_candidatos_eleccion_contienda_numero");
    }
}

/// <summary>Mapeo de <see cref="ParticipacionVoto"/> a la tabla <c>colegio.participaciones_voto</c>.</summary>
internal sealed class ParticipacionVotoConfiguration : IEntityTypeConfiguration<ParticipacionVoto>
{
    public void Configure(EntityTypeBuilder<ParticipacionVoto> builder)
    {
        builder.ToTable("participaciones_voto");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.CreadoEn).IsRequired();

        builder.HasOne<Eleccion>().WithMany().HasForeignKey(p => p.EleccionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(p => p.EstudianteId).OnDelete(DeleteBehavior.Restrict);

        // Un estudiante vota una sola vez por elección, aunque envíe el tarjetón dos veces a la vez.
        builder.HasIndex(p => new { p.EleccionId, p.EstudianteId }).IsUnique()
            .HasDatabaseName("ux_participaciones_voto_eleccion_estudiante");
    }
}

/// <summary>Mapeo de <see cref="Voto"/> a la tabla <c>colegio.votos</c>: sin fecha ni relación con el votante.</summary>
internal sealed class VotoConfiguration : IEntityTypeConfiguration<Voto>
{
    public void Configure(EntityTypeBuilder<Voto> builder)
    {
        builder.ToTable("votos");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Ambito).HasMaxLength(20).IsRequired();

        builder.HasOne<Eleccion>().WithMany().HasForeignKey(v => v.EleccionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Candidato>().WithMany().HasForeignKey(v => v.CandidatoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.EleccionId, v.Cargo, v.Ambito }).HasDatabaseName("ix_votos_eleccion_contienda");
    }
}
