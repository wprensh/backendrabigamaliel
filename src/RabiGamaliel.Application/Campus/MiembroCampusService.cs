using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

public interface IMiembroCampusService
{
    Task<PagedResult<MiembroCampusResponse>> ListarAsync(MiembroQuery query, CancellationToken ct);

    Task<Result<MiembroCampusResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<MiembroCampusResponse>> CrearAsync(CrearMiembroRequest request, CancellationToken ct);

    Task<Result<MiembroCampusResponse>> ActualizarAsync(Guid id, ActualizarMiembroRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct);

    Task<Result<ImportacionResponse>> ImportarAsync(ImportarMiembrosRequest request, CancellationToken ct);

    Task<Result<IReadOnlyList<ClaveGenerada>>> GenerarClavesAsync(GenerarClavesRequest request, CancellationToken ct);
}

/// <summary>Estudiantes y docentes del campus: registro, importación desde una lista y claves de acceso.</summary>
public sealed class MiembroCampusService(
    IMiembroCampusRepository repository,
    IUnitOfWork unitOfWork,
    IHasherCodigosAcceso hasher,
    TimeProvider reloj,
    IValidator<CrearMiembroRequest> crearValidator,
    IValidator<ActualizarMiembroRequest> actualizarValidator,
    IValidator<ImportarMiembrosRequest> importarValidator) : IMiembroCampusService
{
    private static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Miembro.NoEncontrado", $"No existe un estudiante o docente con id '{id}'.");

    public async Task<PagedResult<MiembroCampusResponse>> ListarAsync(MiembroQuery query, CancellationToken ct)
    {
        var normalizada = query with
        {
            Grupo = string.IsNullOrWhiteSpace(query.Grupo) ? null : query.Grupo.Trim().ToUpperInvariant(),
            Pagina = Math.Max(1, query.Pagina),
            TamanoPagina = Math.Clamp(query.TamanoPagina, 1, MiembroQuery.TamanoPaginaMaximo),
        };

        var ahora = reloj.GetUtcNow();
        var pagina = await repository.ListarAsync(normalizada, ct);
        return pagina.Map(m => MiembroCampusResponse.Desde(m, ahora));
    }

    public async Task<Result<MiembroCampusResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var miembro = await repository.ObtenerPorIdAsync(id, ct);
        return miembro is null ? NoEncontrado(id) : MiembroCampusResponse.Desde(miembro, reloj.GetUtcNow());
    }

    public async Task<Result<MiembroCampusResponse>> CrearAsync(CrearMiembroRequest request, CancellationToken ct)
    {
        if (await crearValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var documento = MiembroCampus.NormalizarDocumento(request.Documento);
        if (await repository.ObtenerPorDocumentoAsync(documento, ct) is not null)
        {
            return Error.Conflicto("Miembro.DocumentoEnUso", $"Ya existe una persona con el documento {documento}.");
        }

        var miembro = MiembroCampus.Crear(request.Rol, documento, request.NombreCompleto, request.Grado, request.Grupo);
        miembro.ActualizarContacto(request.Correo, request.Telefono);
        repository.Agregar(miembro);
        await unitOfWork.SaveChangesAsync(ct);
        return MiembroCampusResponse.Desde(miembro, reloj.GetUtcNow());
    }

    public async Task<Result<MiembroCampusResponse>> ActualizarAsync(Guid id, ActualizarMiembroRequest request, CancellationToken ct)
    {
        if (await actualizarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var miembro = await repository.ObtenerPorIdAsync(id, ct);
        if (miembro is null)
        {
            return NoEncontrado(id);
        }

        miembro.Actualizar(request.Rol, request.NombreCompleto, request.Grado, request.Grupo, request.Activo);
        miembro.ActualizarContacto(request.Correo, request.Telefono);
        await unitOfWork.SaveChangesAsync(ct);
        return MiembroCampusResponse.Desde(miembro, reloj.GetUtcNow());
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var miembro = await repository.ObtenerPorIdAsync(id, ct);
        if (miembro is null)
        {
            return NoEncontrado(id);
        }

        if (await repository.TieneHistorialAsync(id, ct))
        {
            return Error.Conflicto(
                "Miembro.ConHistorial",
                "Esta persona ya tiene historial (votos, notas, asistencia, observador o simulacros). Desactívela en lugar de eliminarla.");
        }

        repository.Eliminar(miembro);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<ImportacionResponse>> ImportarAsync(ImportarMiembrosRequest request, CancellationToken ct)
    {
        if (await importarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var errores = new List<ErrorImportacion>();
        var validas = new Dictionary<string, FilaImportacion>();
        for (var i = 0; i < request.Filas.Count; i++)
        {
            var fila = request.Filas[i];
            var documento = MiembroCampus.NormalizarDocumento(fila.Documento ?? string.Empty);
            if (ReglasMiembro.ErrorDeFila(request.Rol, fila) is { } mensaje)
            {
                errores.Add(new ErrorImportacion(i + 1, documento, mensaje));
            }
            else if (!validas.TryAdd(documento, fila))
            {
                errores.Add(new ErrorImportacion(i + 1, documento, "El documento está repetido en la lista."));
            }
        }

        var existentes = (await repository.ObtenerPorDocumentosAsync(validas.Keys, ct)).ToDictionary(m => m.Documento);
        int creados = 0, actualizados = 0;
        foreach (var (documento, fila) in validas)
        {
            if (existentes.TryGetValue(documento, out var miembro))
            {
                // Al importar no se reactiva a nadie: un retiro se respeta hasta que se cambie a mano.
                miembro.Actualizar(request.Rol, fila.NombreCompleto, fila.Grado, fila.Grupo, miembro.Activo);
                actualizados++;
            }
            else
            {
                repository.Agregar(MiembroCampus.Crear(request.Rol, documento, fila.NombreCompleto, fila.Grado, fila.Grupo));
                creados++;
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportacionResponse(creados, actualizados, errores);
    }

    public async Task<Result<IReadOnlyList<ClaveGenerada>>> GenerarClavesAsync(GenerarClavesRequest request, CancellationToken ct)
    {
        var filtro = request with { Grupo = string.IsNullOrWhiteSpace(request.Grupo) ? null : request.Grupo.Trim().ToUpperInvariant() };
        var miembros = await repository.ParaGenerarClavesAsync(filtro, GenerarClavesRequest.MaximoPorSolicitud, ct);
        if (miembros.Count == 0)
        {
            return Error.NoEncontrado("Miembro.SinCoincidencias", "No hay personas activas que coincidan con el filtro.");
        }

        var claves = new List<ClaveGenerada>(miembros.Count);
        foreach (var miembro in miembros)
        {
            var clave = CodigosAcceso.Generar();
            miembro.AsignarClave(hasher.Hashear(CodigosAcceso.Normalizar(clave)));
            claves.Add(new ClaveGenerada(
                miembro.Id, miembro.Documento, miembro.NombreCompleto, miembro.Rol, miembro.Grado, miembro.Grupo, clave));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return claves;
    }
}
