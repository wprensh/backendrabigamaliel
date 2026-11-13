using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Usuarios;

public interface IAutenticacionService
{
    Task<EstadoAutenticacionResponse> EstadoAsync(CancellationToken ct);

    Task<Result<SesionResponse>> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken ct);

    Task<Result<SesionResponse>> ConfigurarAsync(ConfiguracionInicialRequest request, CancellationToken ct);

    Task<Result<SesionResponse>> CambiarMiClaveAsync(Guid usuarioId, CambiarMiClaveRequest request, CancellationToken ct);
}

/// <summary>Inicio de sesión, primer superadministrador y cambio de la propia contraseña.</summary>
public sealed class AutenticacionService(
    IUsuarioRepository repository,
    IUnitOfWork unitOfWork,
    IHasherClaves hasher,
    IEmisorTokens emisor,
    TimeProvider reloj,
    IValidator<ConfiguracionInicialRequest> configuracionValidator,
    IValidator<CambiarMiClaveRequest> cambiarClaveValidator) : IAutenticacionService
{
    // Mismo mensaje si el usuario no existe o la clave es errada: no revela qué usuarios existen.
    private static readonly Error CredencialesInvalidas =
        Error.NoAutorizado("Autenticacion.CredencialesInvalidas", "Usuario o contraseña incorrectos.");

    private static readonly Error ClaveActualIncorrecta = Error.Validacion(KeyValuePair.Create(
        nameof(CambiarMiClaveRequest.ClaveActual), new[] { "La contraseña actual no es correcta." }));

    // Hash de una clave cualquiera para gastar el mismo tiempo cuando el usuario no existe.
    private readonly Lazy<string> _hashSenuelo = new(() => hasher.Hashear(Guid.NewGuid().ToString()));

    public async Task<EstadoAutenticacionResponse> EstadoAsync(CancellationToken ct) =>
        new(RequiereConfiguracion: !await repository.ExisteAlgunoAsync(ct));

    public async Task<Result<SesionResponse>> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrEmpty(request.Clave)
            || request.Clave.Length > Usuario.ClaveMaxLength)
        {
            return CredencialesInvalidas;
        }

        var ahora = reloj.GetUtcNow();
        var usuario = await repository.ObtenerPorNombreUsuarioAsync(Usuario.NormalizarNombreUsuario(request.Usuario), ct);
        if (usuario is null)
        {
            hasher.Verificar(_hashSenuelo.Value, request.Clave);
            return CredencialesInvalidas;
        }

        if (usuario.EstaBloqueado(ahora))
        {
            return Error.NoAutorizado(
                "Autenticacion.Bloqueado",
                $"Demasiados intentos fallidos. Intente de nuevo en {Usuario.DuracionBloqueo.TotalMinutes:0} minutos.");
        }

        var verificacion = hasher.Verificar(usuario.HashClave, request.Clave);
        if (verificacion == VerificacionClave.Incorrecta)
        {
            usuario.RegistrarIntentoFallido(ahora);
            await unitOfWork.SaveChangesAsync(ct);
            return CredencialesInvalidas;
        }

        if (!usuario.Activo)
        {
            return Error.NoAutorizado("Autenticacion.Inactivo", "Su usuario está desactivado. Consulte al administrador.");
        }

        if (verificacion == VerificacionClave.CorrectaRehashear)
        {
            usuario.ActualizarHash(hasher.Hashear(request.Clave));
        }

        usuario.RegistrarIngreso(ahora);
        await unitOfWork.SaveChangesAsync(ct);
        return Sesion(usuario);
    }

    public async Task<Result<SesionResponse>> ConfigurarAsync(ConfiguracionInicialRequest request, CancellationToken ct)
    {
        if (await configuracionValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        if (await repository.ExisteAlgunoAsync(ct))
        {
            return Error.Conflicto("Autenticacion.YaConfigurado", "El panel ya tiene usuarios. Inicie sesión.");
        }

        var usuario = Usuario.Crear(
            request.NombreUsuario, request.NombreCompleto, request.Correo, hasher.Hashear(request.Clave),
            esSuperAdministrador: true, permisos: []);
        usuario.RegistrarIngreso(reloj.GetUtcNow());

        repository.Agregar(usuario);
        await unitOfWork.SaveChangesAsync(ct);
        return Sesion(usuario);
    }

    public async Task<Result<SesionResponse>> CambiarMiClaveAsync(
        Guid usuarioId, CambiarMiClaveRequest request, CancellationToken ct)
    {
        if (await cambiarClaveValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var usuario = await repository.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null)
        {
            return Error.NoEncontrado("Usuario.NoEncontrado", "Su usuario ya no existe.");
        }

        if (hasher.Verificar(usuario.HashClave, request.ClaveActual) == VerificacionClave.Incorrecta)
        {
            return ClaveActualIncorrecta;
        }

        // Cambiar la clave cierra las demás sesiones; esta recibe un token nuevo.
        usuario.CambiarClave(hasher.Hashear(request.ClaveNueva));
        await unitOfWork.SaveChangesAsync(ct);
        return Sesion(usuario);
    }

    private SesionResponse Sesion(Usuario usuario)
    {
        var token = emisor.Emitir(usuario);
        return new SesionResponse(token.Token, token.Expira, PerfilResponse.Desde(usuario));
    }
}
