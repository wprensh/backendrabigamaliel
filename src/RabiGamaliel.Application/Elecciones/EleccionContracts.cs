using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Elecciones;

namespace RabiGamaliel.Application.Elecciones;

public sealed record EleccionResponse(
    Guid Id,
    string Nombre,
    string? Descripcion,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    EstadoEleccion Estado,
    IReadOnlyList<CargoEleccion> Cargos,
    int TotalCandidatos,
    int TotalVotantes)
{
    public static EleccionResponse Desde(Eleccion e, DateTimeOffset ahora, int candidatos, int votantes) =>
        new(e.Id, e.Nombre, e.Descripcion, e.Inicio, e.Fin, e.Estado(ahora), e.Cargos, candidatos, votantes);
}

/// <summary>Cuerpo de POST y PUT /api/elecciones.</summary>
public sealed record GuardarEleccionRequest(
    string Nombre, string? Descripcion, DateTimeOffset Inicio, DateTimeOffset Fin, IReadOnlyList<CargoEleccion> Cargos);

public sealed record CandidatoResponse(
    Guid Id,
    Guid EleccionId,
    CargoEleccion Cargo,
    Guid EstudianteId,
    string NombreCompleto,
    int Grado,
    string? Grupo,
    string Ambito,
    string NombreAmbito,
    int Numero,
    string? Lema,
    string? Propuestas,
    string? FotoUrl)
{
    public static CandidatoResponse Desde(Candidato c) => new(
        c.Id, c.EleccionId, c.Cargo, c.EstudianteId, c.NombreCompleto, c.Grado, c.Grupo, c.Ambito,
        ReglasCargo.NombreAmbito(c.Ambito), c.Numero, c.Lema, c.Propuestas, c.FotoUrl);
}

/// <summary>Cuerpo de POST /api/elecciones/{id}/candidatos. Sin número, toma el siguiente libre.</summary>
public sealed record CrearCandidatoRequest(
    CargoEleccion Cargo, Guid EstudianteId, int? Numero, string? Lema, string? Propuestas, string? FotoUrl);

/// <summary>Cuerpo de PUT /api/elecciones/{id}/candidatos/{candidatoId}: el perfil de campaña.</summary>
public sealed record ActualizarCandidatoRequest(int Numero, string? Lema, string? Propuestas, string? FotoUrl);

/// <summary>Estudiante que puede inscribirse a un cargo.</summary>
public sealed record AspiranteResponse(Guid Id, string Documento, string NombreCompleto, int Grado, string? Grupo);

// ---------- Resultados ----------

public sealed record ResultadoCandidato(Guid CandidatoId, string NombreCompleto, int Numero, string? FotoUrl, int Votos);

/// <summary>Una contienda: un cargo en un ámbito (p. ej. vocero de 7°).</summary>
public sealed record ContiendaResultado(
    CargoEleccion Cargo,
    string NombreCargo,
    string Ambito,
    string NombreAmbito,
    IReadOnlyList<ResultadoCandidato> Candidatos,
    int VotosEnBlanco,
    int TotalVotos,
    Guid? GanadorId,
    bool Empate,
    bool GanaVotoEnBlanco);

public sealed record ParticipacionGrado(int Grado, string NombreGrado, int Habilitados, int Votantes);

public sealed record ParticipacionResponse(int Habilitados, int Votantes, IReadOnlyList<ParticipacionGrado> PorGrado);

/// <summary>Resultados de una elección. <see cref="Contiendas"/> va vacío si todavía no se pueden mostrar.</summary>
public sealed record ResultadosResponse(
    Guid EleccionId,
    string Nombre,
    EstadoEleccion Estado,
    bool ConteoDisponible,
    IReadOnlyList<ContiendaResultado> Contiendas,
    ParticipacionResponse Participacion);

// ---------- Campus: tarjetón y voto ----------

/// <summary>Elección tal como la ve un estudiante o docente en el campus.</summary>
public sealed record EleccionCampusResponse(
    Guid Id,
    string Nombre,
    string? Descripcion,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    EstadoEleccion Estado,
    IReadOnlyList<CargoEleccion> Cargos,
    bool? YaVoto);

public sealed record ContiendaTarjeton(
    CargoEleccion Cargo, string NombreCargo, string Ambito, string NombreAmbito, IReadOnlyList<CandidatoResponse> Candidatos);

/// <summary>
/// Tarjetón de la elección. Para un estudiante: solo las contiendas en las que vota.
/// Para un docente: todas, para conocer a los candidatos.
/// </summary>
public sealed record TarjetonResponse(
    Guid EleccionId,
    string Nombre,
    EstadoEleccion Estado,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    bool PuedeVotar,
    bool? YaVoto,
    IReadOnlyList<ContiendaTarjeton> Contiendas);

/// <summary>Un voto por cargo. <see cref="CandidatoId"/> null es voto en blanco.</summary>
public sealed record VotoRequest(CargoEleccion Cargo, Guid? CandidatoId);

public sealed record VotarRequest(IReadOnlyList<VotoRequest> Votos);

/// <summary>Quién hace la petición en el campus.</summary>
public sealed record VotanteActual(Guid Id, RolCampus Rol, int? Grado, string? Grupo);
