using System.Text.Json;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Simulacros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RabiGamaliel.Infrastructure.Persistence.Configurations;

internal sealed class SimulacroConfiguration : IEntityTypeConfiguration<Simulacro>
{
    public void Configure(EntityTypeBuilder<Simulacro> builder)
    {
        builder.ToTable("simulacros");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Titulo).HasMaxLength(Simulacro.TituloMaxLength).IsRequired();
        builder.Property(s => s.Instrucciones).HasMaxLength(Simulacro.InstruccionesMaxLength);
        builder.Property(s => s.CreadoEn).IsRequired();
        builder.Ignore(s => s.Grados);
        builder.Property<List<int>>("_grados").HasColumnName("grados").IsRequired();
        builder.HasIndex(s => s.Inicio).HasDatabaseName("ix_simulacros_inicio");
    }
}

internal sealed class PreguntaSimulacroConfiguration : IEntityTypeConfiguration<PreguntaSimulacro>
{
    public void Configure(EntityTypeBuilder<PreguntaSimulacro> builder)
    {
        builder.ToTable("preguntas_simulacro");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Competencia).HasMaxLength(PreguntaSimulacro.CompetenciaMaxLength);
        builder.Property(p => p.Contexto).HasMaxLength(PreguntaSimulacro.ContextoMaxLength);
        builder.Property(p => p.Enunciado).HasMaxLength(PreguntaSimulacro.EnunciadoMaxLength).IsRequired();
        builder.Property(p => p.ImagenUrl).HasMaxLength(PreguntaSimulacro.ImagenUrlMaxLength);
        builder.Property(p => p.Explicacion).HasMaxLength(PreguntaSimulacro.ExplicacionMaxLength);
        builder.Property(p => p.CreadoEn).IsRequired();
        builder.Ignore(p => p.Opciones);
        builder.Property<List<string>>("_opciones").HasColumnName("opciones").IsRequired();

        builder.HasOne<Simulacro>().WithMany().HasForeignKey(p => p.SimulacroId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.SimulacroId, p.Orden }).HasDatabaseName("ix_preguntas_simulacro_orden");

        // Sin llave foránea: borrar la pregunta del banco no toca los simulacros que la usaron.
        builder.HasIndex(p => p.PreguntaBancoId).HasDatabaseName("ix_preguntas_simulacro_pregunta_banco");
    }
}

internal sealed class PreguntaBancoConfiguration : IEntityTypeConfiguration<PreguntaBanco>
{
    public void Configure(EntityTypeBuilder<PreguntaBanco> builder)
    {
        builder.ToTable("preguntas_banco");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Competencia).HasMaxLength(PreguntaSimulacro.CompetenciaMaxLength);
        builder.Property(p => p.Contexto).HasMaxLength(PreguntaSimulacro.ContextoMaxLength);
        builder.Property(p => p.Enunciado).HasMaxLength(PreguntaSimulacro.EnunciadoMaxLength).IsRequired();
        builder.Property(p => p.ImagenUrl).HasMaxLength(PreguntaSimulacro.ImagenUrlMaxLength);
        builder.Property(p => p.Explicacion).HasMaxLength(PreguntaSimulacro.ExplicacionMaxLength);
        builder.Property(p => p.CreadoEn).IsRequired();
        builder.Ignore(p => p.Opciones);
        builder.Property<List<string>>("_opciones").HasColumnName("opciones").IsRequired();
        builder.Ignore(p => p.Grados);
        builder.Property<List<int>>("_grados").HasColumnName("grados").IsRequired();
        builder.HasIndex(p => p.Area).HasDatabaseName("ix_preguntas_banco_area");
    }
}

internal sealed class IntentoSimulacroConfiguration : IEntityTypeConfiguration<IntentoSimulacro>
{
    public void Configure(EntityTypeBuilder<IntentoSimulacro> builder)
    {
        builder.ToTable("intentos_simulacro");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Grupo).HasMaxLength(MiembroCampus.GrupoMaxLength);
        builder.Property(i => i.CreadoEn).IsRequired();
        builder.Ignore(i => i.Respuestas);
        builder.Ignore(i => i.Entregado);

        // Respuestas como jsonb {"idPregunta": opcion}. El dominio reemplaza el diccionario en cada cambio.
        builder.Property<Dictionary<Guid, int>>("_respuestas")
            .HasColumnName("respuestas")
            .HasColumnType("jsonb")
            .HasConversion(
                d => JsonSerializer.Serialize(d, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<Guid, int>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<Guid, int>(),
                new ValueComparer<Dictionary<Guid, int>>(
                    (a, b) => ReferenceEquals(a, b),
                    d => d.Count,
                    d => new Dictionary<Guid, int>(d)))
            .IsRequired();

        builder.HasOne<Simulacro>().WithMany().HasForeignKey(i => i.SimulacroId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MiembroCampus>().WithMany().HasForeignKey(i => i.EstudianteId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.SimulacroId, i.EstudianteId }).IsUnique().HasDatabaseName("ux_intentos_simulacro_simulacro_estudiante");
        builder.HasIndex(i => i.EstudianteId).HasDatabaseName("ix_intentos_simulacro_estudiante");
    }
}
