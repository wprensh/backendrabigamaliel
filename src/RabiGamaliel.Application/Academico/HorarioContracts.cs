using RabiGamaliel.Domain.Academico;

namespace RabiGamaliel.Application.Academico;

public sealed record BloqueHorarioResponse(
    Guid Id, Guid AsignacionId, int Dia, TimeOnly Inicio, TimeOnly Fin,
    string Asignatura, int Grado, string? Grupo, Guid DocenteId, string DocenteNombre);

/// <summary>Horario semanal de un docente o de un curso: su carga (con horas programadas) y sus bloques.</summary>
public sealed record HorarioResponse(IReadOnlyList<AsignacionHorarioResponse> Asignaciones, IReadOnlyList<BloqueHorarioResponse> Bloques);

public sealed record AsignacionHorarioResponse(AsignacionResponse Asignacion, double HorasProgramadas);

public sealed record GuardarBloqueRequest(Guid AsignacionId, int Dia, TimeOnly Inicio, TimeOnly Fin);

public sealed record MoverBloqueRequest(int Dia, TimeOnly Inicio, TimeOnly Fin);

internal static class ReglasHorario
{
    public static readonly TimeOnly Apertura = new(5, 0);
    public static readonly TimeOnly Cierre = new(22, 0);
    public const int MinutosMinimos = 15;

    public static bool FranjaValida(int dia, TimeOnly inicio, TimeOnly fin) =>
        dia is >= BloqueHorario.PrimerDia and <= BloqueHorario.UltimoDia
        && inicio >= Apertura && fin <= Cierre && inicio < fin
        && (fin.ToTimeSpan() - inicio.ToTimeSpan()).TotalMinutes >= MinutosMinimos;
}
