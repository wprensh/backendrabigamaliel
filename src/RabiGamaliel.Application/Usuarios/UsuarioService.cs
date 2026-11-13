using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Usuarios;

public interface IUsuarioService
{
    Task<PagedResult<UsuarioResponse>> ListarAsync(UsuarioQuery query, CancellationToken ct);

    Task<Result<UsuarioResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<UsuarioResponse>> CrearAsync(CrearUsuarioRequest request, CancellationToken ct);

    Task<Result<UsuarioResponse>> ActualizarAsync(Guid id, ActualizarUsuarioRequest request, Guid actorId, CancellationToken ct);

    Task<Result<Unit>> RestablecerClaveAsync(Guid id, RestablecerClaveRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, Guid actorId, CancellationToken ct);

    IReadOnlyList<ModuloPermisosResponse> CatalogoPermisos();
}

/// <summary>Administración de los usuarios del panel y de sus permisos por módulo.</summary>
public sealed class UsuarioService(
    IUsuarioRepository repository,
    IUnitOfWork unitOfWork,
    IHasherClaves hasher,
    TimeProvider reloj,
    IValidator<CrearUsuarioRequest> crearValidator,
    IValidator<ActualizarUsuarioRequest> actualizarValidator,
    IValidator<RestablecerClaveRequest> restablecerValidator) : IUsuarioService
{
    private static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Usuario.NoEncontrado", $"No existe un usuario con id '{id}'.");

    private static readonly Error UltimoSuperAdministrador = Error.Conflicto(
        "Usuario.UltimoSuperAdministrador",
        "Debe quedar al menos un superadministrador activo.");

    public async Task<PagedResult<UsuarioResponse>> ListarAsync(UsuarioQuery query, CancellationToken ct)
    {
        var normalizada = query with
        {
            Pagina = Math.Max(1, query.Pagina),
            TamanoPagina = Math.Clamp(query.TamanoPagina, 1, UsuarioQuery.TamanoPaginaMaximo),
        };

        var ahora = reloj.GetUtcNow();
        var pagina = await repository.ListarAsync(normalizada, ct);
        return pagina.Map(u => UsuarioResponse.Desde(u, ahora));
    }

    public async Task<Result<UsuarioResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var usuario = await repository.ObtenerPorIdAsync(id, ct);
        return usuario is null ? NoEncontrado(id) : UsuarioResponse.Desde(usuario, reloj.GetUtcNow());
    }

    public async Task<Result<UsuarioResponse>> CrearAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        if (await crearValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var nombreUsuario = Usuario.NormalizarNombreUsuario(request.NombreUsuario);
        if (await repository.ExisteNombreUsuarioAsync(nombreUsuario, ct))
        {
            return Error.Conflicto("Usuario.NombreEnUso", $"El nombre de usuario '{nombreUsuario}' ya está en uso.");
        }

        var usuario = Usuario.Crear(
            nombreUsuario, request.NombreCompleto, request.Correo, hasher.Hashear(request.Clave),
            request.EsSuperAdministrador, LeerPermisos(request.Permisos));

        repository.Agregar(usuario);
        await unitOfWork.SaveChangesAsync(ct);
        return UsuarioResponse.Desde(usuario, reloj.GetUtcNow());
    }

    public async Task<Result<UsuarioResponse>> ActualizarAsync(
        Guid id, ActualizarUsuarioRequest request, Guid actorId, CancellationToken ct)
    {
        if (await actualizarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var usuario = await repository.ObtenerPorIdAsync(id, ct);
        if (usuario is null)
        {
            return NoEncontrado(id);
        }

        if (id == actorId && !request.Activo)
        {
            return Error.Conflicto("Usuario.DesactivarseASiMismo", "No puede desactivar su propio usuario.");
        }

        var dejaDeSerSuperActivo = usuario.EsSuperAdministrador && usuario.Activo
                                   && (!request.EsSuperAdministrador || !request.Activo);
        if (dejaDeSerSuperActivo && await repository.ContarOtrosSuperAdministradoresActivosAsync(id, ct) == 0)
        {
            return UltimoSuperAdministrador;
        }

        usuario.ActualizarDatos(request.NombreCompleto, request.Correo);
        usuario.AsignarAcceso(request.EsSuperAdministrador, LeerPermisos(request.Permisos), request.Activo);
        await unitOfWork.SaveChangesAsync(ct);
        return UsuarioResponse.Desde(usuario, reloj.GetUtcNow());
    }

    public async Task<Result<Unit>> RestablecerClaveAsync(Guid id, RestablecerClaveRequest request, CancellationToken ct)
    {
        if (await restablecerValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var usuario = await repository.ObtenerPorIdAsync(id, ct);
        if (usuario is null)
        {
            return NoEncontrado(id);
        }

        // También desbloquea la cuenta y cierra las sesiones abiertas de ese usuario.
        usuario.CambiarClave(hasher.Hashear(request.ClaveNueva));
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        if (id == actorId)
        {
            return Error.Conflicto("Usuario.EliminarseASiMismo", "No puede eliminar su propio usuario.");
        }

        var usuario = await repository.ObtenerPorIdAsync(id, ct);
        if (usuario is null)
        {
            return NoEncontrado(id);
        }

        if (usuario.EsSuperAdministrador && usuario.Activo
            && await repository.ContarOtrosSuperAdministradoresActivosAsync(id, ct) == 0)
        {
            return UltimoSuperAdministrador;
        }

        repository.Eliminar(usuario);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public IReadOnlyList<ModuloPermisosResponse> CatalogoPermisos() =>
    [
        .. Enum.GetValues<ModuloSistema>()
            .Select(m => new ModuloPermisosResponse(m, Enum.GetValues<AccionPermiso>())),
    ];

    private static IEnumerable<Permiso> LeerPermisos(IReadOnlyList<string>? permisos) =>
        (permisos ?? []).Select(p => Permiso.TryParse(p, out var permiso) ? permiso : (Permiso?)null)
                        .Where(p => p is not null)
                        .Select(p => p!.Value);
}
