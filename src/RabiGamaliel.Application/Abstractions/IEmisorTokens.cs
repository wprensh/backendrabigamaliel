using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Abstractions;

public sealed record TokenEmitido(string Token, DateTimeOffset Expira);

/// <summary>Emite los tokens de sesión del panel administrativo y del campus virtual (lo implementa la capa API).</summary>
public interface IEmisorTokens
{
    TokenEmitido Emitir(Usuario usuario);

    /// <summary>Token del campus: no sirve para el panel administrativo, ni al revés.</summary>
    TokenEmitido EmitirCampus(MiembroCampus miembro);
}
