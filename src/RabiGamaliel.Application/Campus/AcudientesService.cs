using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

public interface IAcudientesService
{
    Task<Result<IReadOnlyList<VinculoResponse>>> VinculosAsync(Guid miembroId, CancellationToken ct);

    Task<Result<VinculoResponse>> VincularAsync(Guid miembroId, VincularRequest request, CancellationToken ct);

    Task<Result<Unit>> DesvincularAsync(Guid miembroId, Guid vinculoId, CancellationToken ct);

    Task<Result<ImportacionAcudientesResponse>> ImportarAsync(ImportarAcudientesRequest request, CancellationToken ct);

    Task<PerfilCampusResponse> PerfilAsync(MiembroCampus miembro, CancellationToken ct);

    Task<Result<CarnetVerificacionResponse>> VerificarCarnetAsync(string codigo, CancellationToken ct);
}

/// <summary>Acudientes y sus estudiantes; perfil del campus y verificación pública del carnet.</summary>
public sealed class AcudientesService(
    IMiembroCampusRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<ImportarAcudientesRequest> importarValidator) : IAcudientesService
{
    private static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Miembro.NoEncontrado", $"No existe un estudiante, docente o acudiente con id '{id}'.");

    public async Task<Result<IReadOnlyList<VinculoResponse>>> VinculosAsync(Guid miembroId, CancellationToken ct)
    {
        var miembro = await repository.ObtenerPorIdAsync(miembroId, ct);
        return miembro switch
        {
            null => NoEncontrado(miembroId),
            { Rol: RolCampus.Acudiente } => await repository.VinculosAsync(miembro.Id, null, ct),
            { Rol: RolCampus.Estudiante } => await repository.VinculosAsync(null, miembro.Id, ct),
            _ => Array.Empty<VinculoResponse>(),
        };
    }

    public async Task<Result<VinculoResponse>> VincularAsync(Guid miembroId, VincularRequest request, CancellationToken ct)
    {
        var miembro = await repository.ObtenerPorIdAsync(miembroId, ct);
        if (miembro is null)
        {
            return NoEncontrado(miembroId);
        }

        if (miembro.Rol == RolCampus.Docente)
        {
            return Error.Conflicto("Vinculo.RolNoValido", "Los vínculos son entre acudientes y estudiantes.");
        }

        var documento = MiembroCampus.NormalizarDocumento(request.Documento ?? string.Empty);
        var otro = await repository.ObtenerPorDocumentoAsync(documento, ct);
        var rolEsperado = miembro.Rol == RolCampus.Acudiente ? RolCampus.Estudiante : RolCampus.Acudiente;
        if (otro is null || otro.Rol != rolEsperado)
        {
            return Error.Campo(nameof(request.Documento), $"No hay un {(rolEsperado == RolCampus.Estudiante ? "estudiante" : "acudiente")} registrado con el documento {documento}.");
        }

        var (acudiente, estudiante) = miembro.Rol == RolCampus.Acudiente ? (miembro, otro) : (otro, miembro);
        var existentes = await repository.VinculosDeAcudienteAsync(acudiente.Id, ct);
        if (existentes.FirstOrDefault(v => v.EstudianteId == estudiante.Id) is { } existente)
        {
            existente.CambiarParentesco(request.Parentesco);
        }
        else
        {
            repository.AgregarVinculo(VinculoAcudiente.Crear(acudiente.Id, estudiante.Id, request.Parentesco));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await repository.VinculosAsync(acudiente.Id, estudiante.Id, ct)).Single();
    }

    public async Task<Result<Unit>> DesvincularAsync(Guid miembroId, Guid vinculoId, CancellationToken ct)
    {
        var vinculo = await repository.ObtenerVinculoAsync(vinculoId, ct);
        if (vinculo is null || (vinculo.AcudienteId != miembroId && vinculo.EstudianteId != miembroId))
        {
            return Error.NoEncontrado("Vinculo.NoEncontrado", "El vínculo no existe.");
        }

        repository.EliminarVinculo(vinculo);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<ImportacionAcudientesResponse>> ImportarAsync(ImportarAcudientesRequest request, CancellationToken ct)
    {
        if (await importarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var filas = request.Filas
            .Select((f, i) => (Fila: i + 1, Datos: f,
                Acudiente: MiembroCampus.NormalizarDocumento(f.DocumentoAcudiente ?? string.Empty),
                Estudiante: MiembroCampus.NormalizarDocumento(f.DocumentoEstudiante ?? string.Empty)))
            .ToList();

        var documentos = filas.SelectMany(f => new[] { f.Acudiente, f.Estudiante }).Where(d => d.Length > 0).Distinct().ToList();
        var personas = (await repository.ObtenerPorDocumentosAsync(documentos, ct)).ToDictionary(m => m.Documento);
        var errores = new List<ErrorImportacion>();
        int creados = 0, actualizados = 0, vinculos = 0;
        var vinculosPorAcudiente = new Dictionary<Guid, List<VinculoAcudiente>>();

        foreach (var (numero, datos, docAcudiente, docEstudiante) in filas)
        {
            if (docAcudiente.Length is < MiembroCampus.DocumentoMinLength or > MiembroCampus.DocumentoMaxLength)
            {
                errores.Add(new ErrorImportacion(numero, docAcudiente, "Documento del acudiente vacío o con un largo no válido."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(datos.NombreAcudiente) || datos.NombreAcudiente.Trim().Length > MiembroCampus.NombreMaxLength)
            {
                errores.Add(new ErrorImportacion(numero, docAcudiente, "Falta el nombre del acudiente o es demasiado largo."));
                continue;
            }

            if (!personas.TryGetValue(docEstudiante, out var estudiante) || estudiante.Rol != RolCampus.Estudiante)
            {
                errores.Add(new ErrorImportacion(numero, docAcudiente, $"No hay un estudiante con el documento {docEstudiante}. Importe primero a los estudiantes."));
                continue;
            }

            if (personas.TryGetValue(docAcudiente, out var acudiente))
            {
                if (acudiente.Rol != RolCampus.Acudiente)
                {
                    errores.Add(new ErrorImportacion(numero, docAcudiente, "Ese documento ya está registrado como estudiante o docente."));
                    continue;
                }

                acudiente.Actualizar(RolCampus.Acudiente, datos.NombreAcudiente, null, null, acudiente.Activo);
                acudiente.ActualizarContacto(datos.Correo ?? acudiente.Correo, datos.Telefono ?? acudiente.Telefono);
                actualizados++;
            }
            else
            {
                acudiente = MiembroCampus.Crear(RolCampus.Acudiente, docAcudiente, datos.NombreAcudiente, null, null);
                acudiente.ActualizarContacto(datos.Correo, datos.Telefono);
                repository.Agregar(acudiente);
                personas[docAcudiente] = acudiente;
                vinculosPorAcudiente[acudiente.Id] = [];
                creados++;
            }

            if (!vinculosPorAcudiente.TryGetValue(acudiente.Id, out var propios))
            {
                propios = await repository.VinculosDeAcudienteAsync(acudiente.Id, ct);
                vinculosPorAcudiente[acudiente.Id] = propios;
            }

            if (propios.FirstOrDefault(v => v.EstudianteId == estudiante.Id) is { } existente)
            {
                existente.CambiarParentesco(datos.Parentesco ?? existente.Parentesco);
            }
            else
            {
                var vinculo = VinculoAcudiente.Crear(acudiente.Id, estudiante.Id, datos.Parentesco);
                repository.AgregarVinculo(vinculo);
                propios.Add(vinculo);
                vinculos++;
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportacionAcudientesResponse(creados, actualizados, vinculos, errores);
    }

    public async Task<PerfilCampusResponse> PerfilAsync(MiembroCampus miembro, CancellationToken ct)
    {
        if (miembro.Rol != RolCampus.Acudiente)
        {
            return PerfilCampusResponse.Desde(miembro);
        }

        var vinculos = await repository.VinculosAsync(miembro.Id, null, ct);
        return PerfilCampusResponse.Desde(
            miembro,
            [.. vinculos.Select(v => new EstudianteVinculado(v.EstudianteId, v.EstudianteNombre, v.EstudianteDocumento, v.Grado, v.Grupo, v.Parentesco))]);
    }

    public async Task<Result<CarnetVerificacionResponse>> VerificarCarnetAsync(string codigo, CancellationToken ct)
    {
        var normalizado = new string([.. (codigo ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);
        var miembro = normalizado.Length == MiembroCampus.CodigoCarnetLength
            ? await repository.ObtenerPorCodigoCarnetAsync(normalizado, ct)
            : null;
        if (miembro is null || miembro.Rol == RolCampus.Acudiente)
        {
            return Error.NoEncontrado("Carnet.NoEncontrado", "No encontramos un carnet con ese código.");
        }

        var documento = miembro.Documento;
        var parcial = documento.Length <= 4 ? documento : new string('•', 4) + documento[^4..];
        return new CarnetVerificacionResponse(
            miembro.CodigoCarnet, miembro.NombreCompleto, miembro.Rol, miembro.Grado, miembro.Grupo, parcial, miembro.Activo,
            FechaColombia.Hoy(reloj).Year);
    }
}
