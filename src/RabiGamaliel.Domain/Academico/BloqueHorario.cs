using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Academico;

/// <summary>
/// Una clase en el horario semanal: el día y la franja en que se dicta una asignación de la carga docente.
/// Una asignación de 2 horas semanales suele tener dos bloques de una hora o uno de dos.
/// </summary>
public sealed class BloqueHorario : Entity
{
    /// <summary>Lunes = 1 … sábado = 6.</summary>
    public const int PrimerDia = 1;
    public const int UltimoDia = 6;

    private BloqueHorario()
    {
    }

    public Guid AsignacionId { get; private set; }

    public int Dia { get; private set; }

    public TimeOnly Inicio { get; private set; }

    public TimeOnly Fin { get; private set; }

    public static BloqueHorario Crear(Guid asignacionId, int dia, TimeOnly inicio, TimeOnly fin) => new()
    {
        AsignacionId = asignacionId,
        Dia = dia,
        Inicio = inicio,
        Fin = fin,
    };

    public void Mover(int dia, TimeOnly inicio, TimeOnly fin)
    {
        Dia = dia;
        Inicio = inicio;
        Fin = fin;
    }

    public double Horas => (Fin.ToTimeSpan() - Inicio.ToTimeSpan()).TotalHours;

    public bool SeCruzaCon(int dia, TimeOnly inicio, TimeOnly fin) => Dia == dia && Inicio < fin && inicio < Fin;
}
