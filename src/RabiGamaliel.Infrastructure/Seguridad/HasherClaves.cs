using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace RabiGamaliel.Infrastructure.Seguridad;

/// <summary>
/// Contraseñas con el hasher de ASP.NET Core Identity (PBKDF2 con HMAC-SHA512, sal aleatoria
/// y 100 000 iteraciones). Nunca se guarda la contraseña, solo su hash.
/// </summary>
internal class HasherClaves(int iteraciones = 100_000) : IHasherClaves
{
    private readonly PasswordHasher<Usuario> _hasher =
        new(Options.Create(new PasswordHasherOptions { IterationCount = iteraciones }));

    public string Hashear(string clave) => _hasher.HashPassword(null!, clave);

    public VerificacionClave Verificar(string hash, string clave) =>
        _hasher.VerifyHashedPassword(null!, hash, clave) switch
        {
            PasswordVerificationResult.Success => VerificacionClave.Correcta,
            PasswordVerificationResult.SuccessRehashNeeded => VerificacionClave.CorrectaRehashear,
            _ => VerificacionClave.Incorrecta,
        };
}

/// <summary>
/// Claves de acceso al campus: 8 caracteres aleatorios (unos 40 bits), así que 10 000 iteraciones bastan
/// y permiten generar las de cientos de estudiantes en pocos segundos.
/// </summary>
internal sealed class HasherCodigosAcceso() : HasherClaves(10_000), IHasherCodigosAcceso;
