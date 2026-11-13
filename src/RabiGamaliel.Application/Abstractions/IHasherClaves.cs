namespace RabiGamaliel.Application.Abstractions;

public enum VerificacionClave
{
    Incorrecta,
    Correcta,

    /// <summary>Correcta, pero el hash usa parámetros antiguos y conviene regenerarlo.</summary>
    CorrectaRehashear,
}

/// <summary>Hash de contraseñas (lo implementa Infrastructure con el algoritmo de ASP.NET Core Identity).</summary>
public interface IHasherClaves
{
    string Hashear(string clave);

    VerificacionClave Verificar(string hash, string clave);
}

/// <summary>
/// Hash de las claves de acceso al campus. Son aleatorias y largas, así que bastan menos iteraciones,
/// lo que permite generar las de todo un colegio en una sola petición.
/// </summary>
public interface IHasherCodigosAcceso : IHasherClaves;
