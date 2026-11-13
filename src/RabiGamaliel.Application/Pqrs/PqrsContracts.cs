using RabiGamaliel.Domain.Pqrs;

namespace RabiGamaliel.Application.Pqrs;

/// <summary>Formulario público "Envíanos un mensaje (PQRS)".</summary>
public sealed record RadicarPqrsRequest(
    string NombreCompleto,
    string Correo,
    string Mensaje,
    TipoPqrs Tipo = TipoPqrs.Peticion);

/// <summary>Confirmación que recibe el ciudadano tras radicar su solicitud.</summary>
public sealed record PqrsRadicadaResponse(Guid Id, string Radicado, DateTimeOffset RadicadaEn);

public sealed record ResponderPqrsRequest(string Respuesta);

/// <summary>Detalle de una solicitud para el panel administrativo.</summary>
public sealed record PqrsResponse(
    Guid Id,
    string Radicado,
    TipoPqrs Tipo,
    string NombreCompleto,
    string Correo,
    string Mensaje,
    EstadoPqrs Estado,
    string? Respuesta,
    DateTimeOffset CreadoEn)
{
    public static PqrsResponse Desde(SolicitudPqrs s) => new(
        s.Id, s.Radicado, s.Tipo, s.NombreCompleto, s.Correo, s.Mensaje, s.Estado, s.Respuesta, s.CreadoEn);
}

public sealed record PqrsQuery(EstadoPqrs? Estado = null, int Pagina = 1, int TamanoPagina = 20);
