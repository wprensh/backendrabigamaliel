namespace RabiGamaliel.Application.Abstractions;

/// <summary>Correo de aviso en texto plano.</summary>
public sealed record CorreoSaliente(string Para, string Asunto, string Cuerpo);

/// <summary>
/// Cola de correos salientes: se envían en segundo plano para no demorar la respuesta.
/// Si el servidor de correo no está configurado, los correos se descartan sin error.
/// </summary>
public interface IColaCorreos
{
    void Encolar(CorreoSaliente correo);
}
