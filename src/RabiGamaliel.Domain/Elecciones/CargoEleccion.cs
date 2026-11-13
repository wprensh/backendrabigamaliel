using System.Globalization;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Domain.Elecciones;

/// <summary>
/// Cargos del gobierno escolar que se eligen por voto de los estudiantes (Ley 115 de 1994, Decreto 1075 de 2015).
/// El representante ante el Consejo Directivo no está aquí: lo elige el Consejo de Estudiantes, no el estudiantado.
/// </summary>
public enum CargoEleccion
{
    /// <summary>Candidatos de 11°; votan todos los estudiantes.</summary>
    Personero = 1,

    /// <summary>Candidatos de 10° u 11°; votan todos los estudiantes.</summary>
    Contralor,

    /// <summary>Un vocero por grado al Consejo de Estudiantes; transición a 3° eligen uno solo entre los de 3°.</summary>
    VoceroGrado,

    /// <summary>Representante o monitor de cada curso; votan los estudiantes del mismo grado y grupo.</summary>
    RepresentanteCurso,
}

/// <summary>
/// Quién puede ser candidato a cada cargo y quiénes votan por él. El "ámbito" agrupa a los candidatos
/// y a los votantes de una misma contienda: "" es toda la institución, "7" es el grado 7°, "0-3" es
/// transición a 3° y "7-A" es el curso 7° A.
/// </summary>
public static class ReglasCargo
{
    public const string AmbitoInstitucion = "";
    public const string AmbitoPrimerosGrados = "0-3";

    /// <summary>Último grado de los que eligen un vocero único (transición, 1°, 2° y 3°).</summary>
    public const int GradoVoceroUnico = 3;

    public static string Nombre(CargoEleccion cargo) => cargo switch
    {
        CargoEleccion.Personero => "Personero(a) estudiantil",
        CargoEleccion.Contralor => "Contralor(a) estudiantil",
        CargoEleccion.VoceroGrado => "Vocero(a) de grado al Consejo de Estudiantes",
        _ => "Representante de curso",
    };

    /// <summary>Por qué un estudiante de ese grado no puede aspirar al cargo; null si puede.</summary>
    public static string? MotivoNoElegible(CargoEleccion cargo, int grado, string? grupo) => cargo switch
    {
        CargoEleccion.Personero when grado != GradosEscolares.Ultimo =>
            "El personero debe ser un estudiante de 11°.",
        CargoEleccion.Contralor when grado < GradosEscolares.Ultimo - 1 =>
            "El contralor debe ser un estudiante de 10° u 11°.",
        CargoEleccion.VoceroGrado when grado < GradoVoceroUnico =>
            "Transición, 1°, 2° y 3° eligen un vocero único entre los estudiantes de 3°.",
        CargoEleccion.RepresentanteCurso when string.IsNullOrWhiteSpace(grupo) =>
            "Para representante de curso el estudiante debe tener grupo asignado.",
        _ => null,
    };

    /// <summary>Contienda en la que participa (como candidato o como votante) un estudiante de ese grado y grupo.</summary>
    public static string Ambito(CargoEleccion cargo, int grado, string? grupo) => cargo switch
    {
        CargoEleccion.Personero or CargoEleccion.Contralor => AmbitoInstitucion,
        CargoEleccion.VoceroGrado => grado <= GradoVoceroUnico ? AmbitoPrimerosGrados : grado.ToString(CultureInfo.InvariantCulture),
        _ => string.IsNullOrWhiteSpace(grupo) ? $"{grado}-" : $"{grado}-{grupo}",
    };

    /// <summary>Texto legible del ámbito: "Toda la institución", "Transición a 3°", "7°", "7° A".</summary>
    public static string NombreAmbito(string ambito)
    {
        if (ambito == AmbitoInstitucion)
        {
            return "Toda la institución";
        }

        if (ambito == AmbitoPrimerosGrados)
        {
            return $"{GradosEscolares.Nombre(GradosEscolares.Transicion)} a {GradosEscolares.Nombre(GradoVoceroUnico)}";
        }

        var partes = ambito.Split('-', 2);
        var grado = int.TryParse(partes[0], out var g) ? GradosEscolares.Nombre(g) : partes[0];
        return partes.Length == 2 && partes[1].Length > 0 ? $"{grado} {partes[1]}" : grado;
    }
}
