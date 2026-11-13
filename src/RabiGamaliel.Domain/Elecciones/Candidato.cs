using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Elecciones;

/// <summary>
/// Perfil de campaña de un estudiante que aspira a un cargo. Guarda el nombre, grado y grupo que tenía
/// al inscribirse, para que el tarjetón y los resultados no cambien si después se edita el estudiante.
/// </summary>
public sealed class Candidato : Entity
{
    public const int LemaMaxLength = 150;
    public const int PropuestasMaxLength = 4000;
    public const int FotoUrlMaxLength = 500;
    public const int NumeroMaximo = 99;

    private Candidato()
    {
    }

    public Guid EleccionId { get; private set; }

    public CargoEleccion Cargo { get; private set; }

    /// <summary>Estudiante del campus que aspira al cargo.</summary>
    public Guid EstudianteId { get; private set; }

    public string NombreCompleto { get; private set; } = string.Empty;

    public int Grado { get; private set; }

    public string? Grupo { get; private set; }

    /// <summary>Contienda en la que compite; ver <see cref="ReglasCargo.Ambito"/>.</summary>
    public string Ambito { get; private set; } = string.Empty;

    /// <summary>Número en el tarjetón, único dentro de la misma contienda.</summary>
    public int Numero { get; private set; }

    public string? Lema { get; private set; }

    public string? Propuestas { get; private set; }

    public string? FotoUrl { get; private set; }

    public static Candidato Crear(
        Guid eleccionId, CargoEleccion cargo, Guid estudianteId, string nombreCompleto, int grado, string? grupo, int numero)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreCompleto);
        if (ReglasCargo.MotivoNoElegible(cargo, grado, grupo) is { } motivo)
        {
            throw new InvalidOperationException(motivo);
        }

        return new Candidato
        {
            EleccionId = eleccionId,
            Cargo = cargo,
            EstudianteId = estudianteId,
            NombreCompleto = nombreCompleto.Trim(),
            Grado = grado,
            Grupo = grupo,
            Ambito = ReglasCargo.Ambito(cargo, grado, grupo),
            Numero = numero,
        };
    }

    public void ActualizarPerfil(int numero, string? lema, string? propuestas, string? fotoUrl)
    {
        Numero = numero;
        Lema = Limpiar(lema);
        Propuestas = Limpiar(propuestas);
        FotoUrl = Limpiar(fotoUrl);
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
