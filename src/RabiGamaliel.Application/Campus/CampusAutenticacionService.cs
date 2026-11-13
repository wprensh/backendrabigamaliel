using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

public interface ICampusAutenticacionService
{
    Task<Result<SesionCampusResponse>> IniciarSesionAsync(IniciarSesionCampusRequest request, CancellationToken ct);
}

/// <summary>Ingreso de estudiantes y docentes al campus con su documento y su clave de acceso.</summary>
public sealed class CampusAutenticacionService(
    IMiembroCampusRepository repository,
    IUnitOfWork unitOfWork,
    IHasherCodigosAcceso hasher,
    IEmisorTokens emisor,
    TimeProvider reloj) : ICampusAutenticacionService
{
    // Mismo mensaje si el documento no existe, no tiene clave o la clave es errada.
    private static readonly Error CredencialesInvalidas =
        Error.NoAutorizado("Campus.CredencialesInvalidas", "Documento o clave de acceso incorrectos.");

    private readonly Lazy<string> _hashSenuelo = new(() => hasher.Hashear(CodigosAcceso.Generar()));

    public async Task<Result<SesionCampusResponse>> IniciarSesionAsync(IniciarSesionCampusRequest request, CancellationToken ct)
    {
        var documento = MiembroCampus.NormalizarDocumento(request.Documento ?? string.Empty);
        var clave = CodigosAcceso.Normalizar(request.Clave ?? string.Empty);
        if (documento.Length is 0 or > MiembroCampus.DocumentoMaxLength || clave.Length is 0 or > 64)
        {
            return CredencialesInvalidas;
        }

        var ahora = reloj.GetUtcNow();
        var miembro = await repository.ObtenerPorDocumentoAsync(documento, ct);
        if (miembro?.HashClave is null)
        {
            hasher.Verificar(_hashSenuelo.Value, clave);
            return CredencialesInvalidas;
        }

        if (miembro.EstaBloqueado(ahora))
        {
            return Error.NoAutorizado(
                "Campus.Bloqueado",
                $"Demasiados intentos fallidos. Intente de nuevo en {MiembroCampus.DuracionBloqueo.TotalMinutes:0} minutos o pida una clave nueva.");
        }

        if (hasher.Verificar(miembro.HashClave, clave) == VerificacionClave.Incorrecta)
        {
            miembro.RegistrarIntentoFallido(ahora);
            await unitOfWork.SaveChangesAsync(ct);
            return CredencialesInvalidas;
        }

        if (!miembro.Activo)
        {
            return Error.NoAutorizado("Campus.Inactivo", "Su acceso al campus está desactivado. Consulte en la secretaría.");
        }

        miembro.RegistrarIngreso(ahora);
        await unitOfWork.SaveChangesAsync(ct);

        var token = emisor.EmitirCampus(miembro);
        var estudiantes = miembro.Rol == RolCampus.Acudiente
            ? (await repository.VinculosAsync(miembro.Id, null, ct))
                .Select(v => new EstudianteVinculado(v.EstudianteId, v.EstudianteNombre, v.EstudianteDocumento, v.Grado, v.Grupo, v.Parentesco))
                .ToList()
            : null;
        return new SesionCampusResponse(token.Token, token.Expira, PerfilCampusResponse.Desde(miembro, estudiantes));
    }
}
