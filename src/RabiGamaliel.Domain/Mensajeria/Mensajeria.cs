using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Mensajeria;

/// <summary>
/// Conversación privada entre un docente y la familia (acudiente o estudiante) sobre un estudiante. Reemplaza los
/// grupos de WhatsApp: queda registro en el colegio y nadie expone su número personal.
/// </summary>
public sealed class Conversacion : Entity
{
    public const int AsuntoMaxLength = 150;

    private Conversacion()
    {
    }

    public Guid DocenteId { get; private set; }

    /// <summary>Acudiente o estudiante que conversa con el docente.</summary>
    public Guid FamiliaId { get; private set; }

    public Guid EstudianteId { get; private set; }

    public string Asunto { get; private set; } = string.Empty;

    public DateTimeOffset UltimoMensajeEn { get; private set; }

    public DateTimeOffset? LeidaDocenteEn { get; private set; }

    public DateTimeOffset? LeidaFamiliaEn { get; private set; }

    /// <summary>Un docente puede cerrar la conversación; la familia ya no responde ahí.</summary>
    public bool Cerrada { get; private set; }

    public static Conversacion Crear(Guid docenteId, Guid familiaId, Guid estudianteId, string asunto) => new()
    {
        DocenteId = docenteId,
        FamiliaId = familiaId,
        EstudianteId = estudianteId,
        Asunto = asunto.Trim(),
    };

    public bool Participa(Guid miembroId) => miembroId == DocenteId || miembroId == FamiliaId;

    public Guid Otro(Guid miembroId) => miembroId == DocenteId ? FamiliaId : DocenteId;

    public void RegistrarMensaje(Guid autorId, DateTimeOffset ahora)
    {
        UltimoMensajeEn = ahora;
        MarcarLeida(autorId, ahora);
        Cerrada = false;
    }

    public void MarcarLeida(Guid miembroId, DateTimeOffset ahora)
    {
        if (miembroId == DocenteId)
        {
            LeidaDocenteEn = ahora;
        }
        else if (miembroId == FamiliaId)
        {
            LeidaFamiliaEn = ahora;
        }
    }

    public bool NoLeida(Guid miembroId)
    {
        var leida = miembroId == DocenteId ? LeidaDocenteEn : LeidaFamiliaEn;
        return leida is null || leida < UltimoMensajeEn;
    }

    public void Cerrar() => Cerrada = true;
}

public sealed class Mensaje : Entity
{
    public const int TextoMaxLength = 2000;

    private Mensaje()
    {
    }

    public Guid ConversacionId { get; private set; }

    public Guid AutorId { get; private set; }

    public string Texto { get; private set; } = string.Empty;

    public static Mensaje Crear(Guid conversacionId, Guid autorId, string texto) => new()
    {
        ConversacionId = conversacionId,
        AutorId = autorId,
        Texto = texto.Trim(),
    };
}
