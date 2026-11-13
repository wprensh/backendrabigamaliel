using System.Security.Cryptography;

namespace RabiGamaliel.Application.Campus;

/// <summary>Claves de acceso al campus: 8 caracteres sin letras ni números que se confundan (O/0, I/1, L).</summary>
public static class CodigosAcceso
{
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int Longitud = 8;

    /// <summary>Clave nueva con formato "ABCD-2345" para dictarla o imprimirla sin errores.</summary>
    public static string Generar()
    {
        var caracteres = RandomNumberGenerator.GetItems<char>(Alfabeto, Longitud);
        return $"{new string(caracteres, 0, 4)}-{new string(caracteres, 4, 4)}";
    }

    /// <summary>Acepta la clave con o sin guion, espacios o minúsculas.</summary>
    public static string Normalizar(string clave) =>
        new([.. clave.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);
}
