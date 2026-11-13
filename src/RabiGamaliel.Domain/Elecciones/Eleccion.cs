using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Elecciones;

public enum EstadoEleccion
{
    /// <summary>Aún no empieza: se pueden ver los candidatos, pero no votar.</summary>
    Programada = 1,
    Abierta,
    Cerrada,
}

/// <summary>Jornada electoral (p. ej. "Gobierno escolar 2027") con los cargos que se eligen y su horario de votación.</summary>
public sealed class Eleccion : Entity
{
    public const int NombreMaxLength = 120;
    public const int DescripcionMaxLength = 1000;

    private List<string> _cargos = [];

    private Eleccion()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    /// <summary>Desde cuándo se puede votar.</summary>
    public DateTimeOffset Inicio { get; private set; }

    /// <summary>Hasta cuándo se puede votar. Después se publican los resultados en el campus.</summary>
    public DateTimeOffset Fin { get; private set; }

    public IReadOnlyList<CargoEleccion> Cargos =>
        [.. _cargos.Select(c => Enum.TryParse<CargoEleccion>(c, out var cargo) ? cargo : (CargoEleccion?)null)
                   .Where(c => c is not null)
                   .Select(c => c!.Value)];

    public static Eleccion Crear(
        string nombre, string? descripcion, DateTimeOffset inicio, DateTimeOffset fin, IEnumerable<CargoEleccion> cargos)
    {
        var eleccion = new Eleccion();
        eleccion.Actualizar(nombre, descripcion, inicio, fin);
        eleccion.CambiarCargos(cargos);
        return eleccion;
    }

    public void Actualizar(string nombre, string? descripcion, DateTimeOffset inicio, DateTimeOffset fin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (fin <= inicio)
        {
            throw new ArgumentException("El cierre debe ser posterior a la apertura.", nameof(fin));
        }

        Nombre = nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        Inicio = inicio;
        Fin = fin;
    }

    public void CambiarCargos(IEnumerable<CargoEleccion> cargos)
    {
        _cargos = [.. cargos.Where(Enum.IsDefined).Distinct().Order().Select(c => c.ToString())];
        if (_cargos.Count == 0)
        {
            throw new ArgumentException("Una elección necesita al menos un cargo.", nameof(cargos));
        }
    }

    public bool Incluye(CargoEleccion cargo) => _cargos.Contains(cargo.ToString());

    public EstadoEleccion Estado(DateTimeOffset ahora) =>
        ahora < Inicio ? EstadoEleccion.Programada
        : ahora < Fin ? EstadoEleccion.Abierta
        : EstadoEleccion.Cerrada;
}
