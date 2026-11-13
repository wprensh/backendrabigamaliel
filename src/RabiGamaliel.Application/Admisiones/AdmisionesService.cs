using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Certificaciones;

namespace RabiGamaliel.Application.Admisiones;

public sealed record RadicarAdmisionRequest(
    int AnioLectivo, int GradoSolicitado, TipoDocumento AspiranteTipoDocumento, string AspiranteDocumento, string AspiranteNombres,
    string AspiranteApellidos, DateOnly AspiranteFechaNacimiento, string? ColegioProcedencia, string? Direccion, string? Eps,
    string AcudienteNombre, string AcudienteDocumento, string? AcudienteParentesco, string AcudienteTelefono, string? AcudienteCorreo,
    string? Observaciones, bool AceptaTratamientoDatos);

public sealed record AdmisionRadicadaResponse(string Radicado, DateTimeOffset RadicadaEn);

public sealed record DocumentoAdmisionResponse(Guid Id, TipoDocumentoAdmision Tipo, string NombreArchivo, string TipoContenido, int Tamano, DateTimeOffset CreadoEn);

/// <summary>Lo que ve la familia al consultar el radicado.</summary>
public sealed record EstadoAdmisionResponse(
    string Radicado, string NombreAspirante, int GradoSolicitado, int AnioLectivo, EstadoAdmision Estado, string? MensajeFamilia,
    DateTimeOffset RadicadaEn, DateTimeOffset? EstadoCambiadoEn, bool AdmiteDocumentos, IReadOnlyList<DocumentoAdmisionResponse> Documentos);

public sealed record SolicitudAdmisionResponse(
    Guid Id, string Radicado, int AnioLectivo, int GradoSolicitado, TipoDocumento AspiranteTipoDocumento, string AspiranteDocumento,
    string AspiranteNombres, string AspiranteApellidos, DateOnly AspiranteFechaNacimiento, string? ColegioProcedencia, string? Direccion,
    string? Eps, string AcudienteNombre, string AcudienteDocumento, string? AcudienteParentesco, string AcudienteTelefono,
    string? AcudienteCorreo, string? Observaciones, EstadoAdmision Estado, string? MensajeFamilia, string? NotaInterna,
    DateTimeOffset CreadoEn, DateTimeOffset? EstadoCambiadoEn, Guid? EstudianteId, IReadOnlyList<DocumentoAdmisionResponse> Documentos)
{
    public static SolicitudAdmisionResponse Desde(SolicitudAdmision s, IReadOnlyList<DocumentoAdmisionResponse> documentos) => new(
        s.Id, s.Radicado, s.AnioLectivo, s.GradoSolicitado, s.AspiranteTipoDocumento, s.AspiranteDocumento, s.AspiranteNombres,
        s.AspiranteApellidos, s.AspiranteFechaNacimiento, s.ColegioProcedencia, s.Direccion, s.Eps, s.AcudienteNombre, s.AcudienteDocumento,
        s.AcudienteParentesco, s.AcudienteTelefono, s.AcudienteCorreo, s.Observaciones, s.Estado, s.MensajeFamilia, s.NotaInterna, s.CreadoEn,
        s.EstadoCambiadoEn, s.EstudianteId, documentos);
}

public sealed record AdmisionQuery(
    EstadoAdmision? Estado = null, int? AnioLectivo = null, int? Grado = null, string? Buscar = null, int Pagina = 1, int TamanoPagina = 20);

public sealed record CambiarEstadoAdmisionRequest(EstadoAdmision Estado, string? MensajeFamilia, string? NotaInterna);

public sealed record MatricularRequest(string? Grupo);

public sealed record MatriculaResponse(Guid EstudianteId, Guid AcudienteId, bool EstudianteNuevo, bool AcudienteNuevo);

public sealed record ArchivoAdmision(string NombreArchivo, string TipoContenido, byte[] Contenido);

public sealed record ConteoEstadoAdmision(EstadoAdmision Estado, int Total);

public interface IAdmisionRepository
{
    Task<SolicitudAdmision?> ObtenerAsync(Guid id, CancellationToken ct);

    Task<SolicitudAdmision?> PorRadicadoAsync(string radicado, CancellationToken ct);

    Task<bool> ExisteEnCursoAsync(string documentoAspirante, int anioLectivo, CancellationToken ct);

    Task<PagedResult<SolicitudAdmision>> ListarAsync(AdmisionQuery query, CancellationToken ct);

    Task<List<DocumentoAdmisionResponse>> DocumentosAsync(Guid solicitudId, CancellationToken ct);

    Task<DocumentoAdmision?> DocumentoAsync(Guid solicitudId, Guid documentoId, CancellationToken ct);

    Task<List<ConteoEstadoAdmision>> ConteoPorEstadoAsync(int? anioLectivo, CancellationToken ct);

    void Agregar(SolicitudAdmision solicitud);

    void Agregar(DocumentoAdmision documento);

    void Eliminar(SolicitudAdmision solicitud);
}

public sealed class RadicarAdmisionValidator : AbstractValidator<RadicarAdmisionRequest>
{
    public RadicarAdmisionValidator(TimeProvider reloj)
    {
        RuleFor(x => x.AceptaTratamientoDatos).Equal(true).WithMessage("Debe autorizar el tratamiento de datos personales (Ley 1581 de 2012).");
        RuleFor(x => x.AnioLectivo)
            .Must(a => a >= FechaColombia.AnioLectivo(reloj) && a <= FechaColombia.AnioLectivo(reloj) + 1)
            .WithMessage("Elija el año lectivo actual o el siguiente.");
        RuleFor(x => x.GradoSolicitado).InclusiveBetween(GradosEscolares.Transicion, GradosEscolares.Ultimo).WithMessage("Elija el grado.");
        RuleFor(x => x.AspiranteTipoDocumento).IsInEnum().WithMessage("Elija el tipo de documento.");
        RuleFor(x => x.AspiranteDocumento).DocumentoValido();
        RuleFor(x => x.AspiranteNombres).NotEmpty().WithMessage("Escriba los nombres del aspirante.").MaximumLength(SolicitudAdmision.NombreMaxLength);
        RuleFor(x => x.AspiranteApellidos).NotEmpty().WithMessage("Escriba los apellidos del aspirante.").MaximumLength(SolicitudAdmision.NombreMaxLength);
        RuleFor(x => x.AspiranteFechaNacimiento)
            .Must(f => f > FechaColombia.Hoy(reloj).AddYears(-25) && f < FechaColombia.Hoy(reloj).AddYears(-3))
            .WithMessage("Revise la fecha de nacimiento.");
        RuleFor(x => x.ColegioProcedencia).MaximumLength(SolicitudAdmision.TextoCortoMaxLength);
        RuleFor(x => x.Direccion).MaximumLength(SolicitudAdmision.TextoCortoMaxLength);
        RuleFor(x => x.Eps).MaximumLength(SolicitudAdmision.TextoCortoMaxLength);
        RuleFor(x => x.AcudienteNombre).NotEmpty().WithMessage("Escriba el nombre del acudiente.").MaximumLength(MiembroCampus.NombreMaxLength);
        RuleFor(x => x.AcudienteDocumento).DocumentoValido();
        RuleFor(x => x.AcudienteParentesco).MaximumLength(VinculoAcudiente.ParentescoMaxLength);
        RuleFor(x => x.AcudienteTelefono).NotEmpty().WithMessage("Escriba un teléfono de contacto.").TelefonoValido()
            .Must(t => t is not null && t.Count(char.IsDigit) >= 7).WithMessage("Escriba un teléfono de contacto válido.");
        RuleFor(x => x.AcudienteCorreo).CorreoValido();
        RuleFor(x => x.Observaciones).MaximumLength(SolicitudAdmision.ObservacionesMaxLength);
    }
}

public interface IAdmisionesService
{
    Task<Result<AdmisionRadicadaResponse>> RadicarAsync(RadicarAdmisionRequest request, CancellationToken ct);

    Task<Result<EstadoAdmisionResponse>> ConsultarAsync(string radicado, string documento, CancellationToken ct);

    Task<Result<DocumentoAdmisionResponse>> SubirDocumentoAsync(
        string radicado, string documento, TipoDocumentoAdmision tipo, ArchivoAdmision archivo, CancellationToken ct);

    Task<PagedResult<SolicitudAdmisionResponse>> ListarAsync(AdmisionQuery query, CancellationToken ct);

    Task<Result<SolicitudAdmisionResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<ArchivoAdmision>> DescargarAsync(Guid id, Guid documentoId, CancellationToken ct);

    Task<Result<SolicitudAdmisionResponse>> CambiarEstadoAsync(Guid id, CambiarEstadoAdmisionRequest request, CancellationToken ct);

    Task<Result<MatriculaResponse>> MatricularAsync(Guid id, MatricularRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct);
}

/// <summary>Pre-matrícula en línea: radicado público, carga de documentos, revisión en el panel y matrícula en el campus.</summary>
public sealed class AdmisionesService(
    IAdmisionRepository repository,
    IMiembroCampusRepository miembros,
    IColaCorreos correos,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<RadicarAdmisionRequest> validator) : IAdmisionesService
{
    private static readonly Error NoEncontradaPublica =
        Error.NoEncontrado("Admision.NoEncontrada", "No encontramos una solicitud con ese radicado y ese documento.");

    private static Error NoEncontrada(Guid id) => Error.NoEncontrado("Admision.NoEncontrada", $"No existe la solicitud '{id}'.");

    public async Task<Result<AdmisionRadicadaResponse>> RadicarAsync(RadicarAdmisionRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var documento = MiembroCampus.NormalizarDocumento(request.AspiranteDocumento);
        if (await repository.ExisteEnCursoAsync(documento, request.AnioLectivo, ct))
        {
            return Error.Conflicto(
                "Admision.EnCurso",
                "Ya hay una solicitud en curso para este aspirante. Consulte su estado con el radicado que recibió o comuníquese con la secretaría.");
        }

        var solicitud = SolicitudAdmision.Crear(
            request.AnioLectivo, request.GradoSolicitado, request.AspiranteTipoDocumento, documento, request.AspiranteNombres,
            request.AspiranteApellidos, request.AspiranteFechaNacimiento, request.ColegioProcedencia, request.Direccion, request.Eps,
            request.AcudienteNombre, request.AcudienteDocumento, request.AcudienteParentesco, request.AcudienteTelefono, request.AcudienteCorreo,
            request.Observaciones);
        repository.Agregar(solicitud);
        await unitOfWork.SaveChangesAsync(ct);

        if (solicitud.AcudienteCorreo is { } correo)
        {
            correos.Encolar(new CorreoSaliente(
                correo,
                $"Solicitud de cupo radicada: {solicitud.Radicado}",
                $"Hola, {solicitud.AcudienteNombre}:\n\nRecibimos la solicitud de cupo para {solicitud.NombreAspirante} en grado {GradosEscolares.Nombre(solicitud.GradoSolicitado)} " +
                $"({solicitud.AnioLectivo}).\n\nSu radicado es {solicitud.Radicado}. Con él y el documento del aspirante puede consultar el estado y cargar documentos en " +
                "la sección Admisiones del sitio web.\n\n— Secretaría · I.E. Rabí Gamaliel"));
        }

        return new AdmisionRadicadaResponse(solicitud.Radicado, solicitud.CreadoEn);
    }

    public async Task<Result<EstadoAdmisionResponse>> ConsultarAsync(string radicado, string documento, CancellationToken ct)
    {
        var solicitud = await BuscarPublicaAsync(radicado, documento, ct);
        if (solicitud is null)
        {
            return NoEncontradaPublica;
        }

        return new EstadoAdmisionResponse(
            solicitud.Radicado, solicitud.NombreAspirante, solicitud.GradoSolicitado, solicitud.AnioLectivo, solicitud.Estado, solicitud.MensajeFamilia,
            solicitud.CreadoEn, solicitud.EstadoCambiadoEn, solicitud.AdmiteDocumentos, await repository.DocumentosAsync(solicitud.Id, ct));
    }

    public async Task<Result<DocumentoAdmisionResponse>> SubirDocumentoAsync(
        string radicado, string documento, TipoDocumentoAdmision tipo, ArchivoAdmision archivo, CancellationToken ct)
    {
        var solicitud = await BuscarPublicaAsync(radicado, documento, ct);
        if (solicitud is null)
        {
            return NoEncontradaPublica;
        }

        if (!solicitud.AdmiteDocumentos)
        {
            return Error.Conflicto("Admision.Cerrada", "Esta solicitud ya fue resuelta y no admite más documentos.");
        }

        if (!Enum.IsDefined(tipo))
        {
            return Error.Campo("Tipo", "Elija qué documento está cargando.");
        }

        if (archivo.Contenido.Length is 0 or > DocumentoAdmision.TamanoMaximoBytes)
        {
            return Error.Campo("Archivo", "El archivo debe pesar como máximo 3 MB.");
        }

        if (!DocumentoAdmision.TiposPermitidos.Contains(archivo.TipoContenido) || !FirmaValida(archivo))
        {
            return Error.Campo("Archivo", "Solo se aceptan archivos PDF, JPG o PNG.");
        }

        var actuales = await repository.DocumentosAsync(solicitud.Id, ct);
        if (actuales.Count >= SolicitudAdmision.MaximoDocumentos)
        {
            return Error.Conflicto("Admision.MaximoDocumentos", $"Ya cargó {SolicitudAdmision.MaximoDocumentos} documentos, el máximo permitido.");
        }

        var nuevo = DocumentoAdmision.Crear(solicitud.Id, tipo, archivo.NombreArchivo, archivo.TipoContenido.ToLowerInvariant(), archivo.Contenido);
        repository.Agregar(nuevo);
        await unitOfWork.SaveChangesAsync(ct);
        return new DocumentoAdmisionResponse(nuevo.Id, nuevo.Tipo, nuevo.NombreArchivo, nuevo.TipoContenido, nuevo.Tamano, nuevo.CreadoEn);
    }

    public async Task<PagedResult<SolicitudAdmisionResponse>> ListarAsync(AdmisionQuery query, CancellationToken ct)
    {
        var pagina = await repository.ListarAsync(
            query with { Pagina = Math.Max(1, query.Pagina), TamanoPagina = Math.Clamp(query.TamanoPagina, 1, 100) }, ct);
        return pagina.Map(s => SolicitudAdmisionResponse.Desde(s, []));
    }

    public async Task<Result<SolicitudAdmisionResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var solicitud = await repository.ObtenerAsync(id, ct);
        return solicitud is null
            ? NoEncontrada(id)
            : SolicitudAdmisionResponse.Desde(solicitud, await repository.DocumentosAsync(id, ct));
    }

    public async Task<Result<ArchivoAdmision>> DescargarAsync(Guid id, Guid documentoId, CancellationToken ct)
    {
        var documento = await repository.DocumentoAsync(id, documentoId, ct);
        return documento is null
            ? Error.NoEncontrado("Admision.DocumentoNoEncontrado", "El documento no existe.")
            : new ArchivoAdmision(documento.NombreArchivo, documento.TipoContenido, documento.Contenido);
    }

    public async Task<Result<SolicitudAdmisionResponse>> CambiarEstadoAsync(Guid id, CambiarEstadoAdmisionRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Estado) || request.Estado == EstadoAdmision.Matriculada)
        {
            return Error.Campo("Estado", "Elija un estado. Para matricular use el botón Matricular.");
        }

        if ((request.MensajeFamilia?.Length ?? 0) > SolicitudAdmision.MensajeMaxLength || (request.NotaInterna?.Length ?? 0) > SolicitudAdmision.MensajeMaxLength)
        {
            return Error.Campo("MensajeFamilia", $"Máximo {SolicitudAdmision.MensajeMaxLength} caracteres.");
        }

        var solicitud = await repository.ObtenerAsync(id, ct);
        if (solicitud is null)
        {
            return NoEncontrada(id);
        }

        var cambio = solicitud.Estado != request.Estado;
        solicitud.CambiarEstado(request.Estado, request.MensajeFamilia, request.NotaInterna, reloj.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);

        if (cambio && solicitud.AcudienteCorreo is { } correo)
        {
            correos.Encolar(new CorreoSaliente(
                correo,
                $"Su solicitud {solicitud.Radicado}: {NombreEstado(solicitud.Estado)}",
                $"Hola, {solicitud.AcudienteNombre}:\n\nLa solicitud de cupo de {solicitud.NombreAspirante} cambió a: {NombreEstado(solicitud.Estado)}." +
                (solicitud.MensajeFamilia is { } m ? $"\n\nMensaje de la secretaría:\n{m}" : string.Empty) +
                $"\n\nConsulte el detalle con el radicado {solicitud.Radicado} en la sección Admisiones del sitio web.\n\n— Secretaría · I.E. Rabí Gamaliel"));
        }

        return SolicitudAdmisionResponse.Desde(solicitud, await repository.DocumentosAsync(id, ct));
    }

    public async Task<Result<MatriculaResponse>> MatricularAsync(Guid id, MatricularRequest request, CancellationToken ct)
    {
        var solicitud = await repository.ObtenerAsync(id, ct);
        if (solicitud is null)
        {
            return NoEncontrada(id);
        }

        if (solicitud.Estado is EstadoAdmision.Rechazada)
        {
            return Error.Conflicto("Admision.Rechazada", "La solicitud fue rechazada; cámbiela de estado antes de matricular.");
        }

        var grupo = string.IsNullOrWhiteSpace(request.Grupo) ? null : request.Grupo.Trim().ToUpperInvariant();
        if (grupo is { Length: > MiembroCampus.GrupoMaxLength } || (grupo is not null && !grupo.All(char.IsLetterOrDigit)))
        {
            return Error.Campo("Grupo", "El grupo solo admite letras y números (p. ej. A o 01).");
        }

        var personas = (await miembros.ObtenerPorDocumentosAsync([solicitud.AspiranteDocumento, solicitud.AcudienteDocumento], ct))
            .ToDictionary(m => m.Documento);

        var estudianteNuevo = !personas.TryGetValue(solicitud.AspiranteDocumento, out var estudiante);
        if (estudiante is not null && estudiante.Rol != RolCampus.Estudiante)
        {
            return Error.Conflicto("Admision.DocumentoEnUso", "El documento del aspirante ya está registrado como docente o acudiente.");
        }

        var acudienteNuevo = !personas.TryGetValue(solicitud.AcudienteDocumento, out var acudiente);
        if (acudiente is not null && acudiente.Rol != RolCampus.Acudiente)
        {
            return Error.Conflicto("Admision.DocumentoEnUso", "El documento del acudiente ya está registrado como estudiante o docente.");
        }

        if (estudiante is null)
        {
            estudiante = MiembroCampus.Crear(RolCampus.Estudiante, solicitud.AspiranteDocumento, solicitud.NombreAspirante, solicitud.GradoSolicitado, grupo);
            miembros.Agregar(estudiante);
        }
        else
        {
            estudiante.Actualizar(RolCampus.Estudiante, solicitud.NombreAspirante, solicitud.GradoSolicitado, grupo, activo: true);
        }

        if (acudiente is null)
        {
            acudiente = MiembroCampus.Crear(RolCampus.Acudiente, solicitud.AcudienteDocumento, solicitud.AcudienteNombre, null, null);
            miembros.Agregar(acudiente);
        }

        acudiente.ActualizarContacto(solicitud.AcudienteCorreo ?? acudiente.Correo, solicitud.AcudienteTelefono);

        var vinculos = acudienteNuevo ? [] : await miembros.VinculosDeAcudienteAsync(acudiente.Id, ct);
        if (vinculos.All(v => v.EstudianteId != estudiante.Id))
        {
            miembros.AgregarVinculo(VinculoAcudiente.Crear(acudiente.Id, estudiante.Id, solicitud.AcudienteParentesco));
        }

        solicitud.MarcarMatriculada(estudiante.Id, reloj.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
        return new MatriculaResponse(estudiante.Id, acudiente.Id, estudianteNuevo, acudienteNuevo);
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var solicitud = await repository.ObtenerAsync(id, ct);
        if (solicitud is null)
        {
            return NoEncontrada(id);
        }

        repository.Eliminar(solicitud);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public static string NombreEstado(EstadoAdmision estado) => estado switch
    {
        EstadoAdmision.Recibida => "Recibida",
        EstadoAdmision.EnRevision => "En revisión",
        EstadoAdmision.DocumentosPendientes => "Documentos pendientes",
        EstadoAdmision.Aprobada => "Aprobada",
        EstadoAdmision.Rechazada => "No aprobada",
        _ => "Matriculado",
    };

    private async Task<SolicitudAdmision?> BuscarPublicaAsync(string radicado, string documento, CancellationToken ct)
    {
        var codigo = (radicado ?? string.Empty).Trim().ToUpperInvariant();
        if (!codigo.StartsWith("ADM-", StringComparison.Ordinal))
        {
            codigo = "ADM-" + codigo;
        }

        var solicitud = codigo.Length == SolicitudAdmision.RadicadoLength ? await repository.PorRadicadoAsync(codigo, ct) : null;
        return solicitud is not null && solicitud.AspiranteDocumento == MiembroCampus.NormalizarDocumento(documento ?? string.Empty)
            ? solicitud
            : null;
    }

    /// <summary>Comprueba los primeros bytes: un ejecutable renombrado a .pdf no pasa.</summary>
    private static bool FirmaValida(ArchivoAdmision archivo)
    {
        var c = archivo.Contenido;
        return archivo.TipoContenido.ToLowerInvariant() switch
        {
            "application/pdf" => c.Length > 4 && c[0] == 0x25 && c[1] == 0x50 && c[2] == 0x44 && c[3] == 0x46,
            "image/png" => c.Length > 8 && c[0] == 0x89 && c[1] == 0x50 && c[2] == 0x4E && c[3] == 0x47,
            "image/jpeg" => c.Length > 3 && c[0] == 0xFF && c[1] == 0xD8 && c[2] == 0xFF,
            _ => false,
        };
    }
}
