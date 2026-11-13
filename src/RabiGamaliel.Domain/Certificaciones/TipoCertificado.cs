namespace RabiGamaliel.Domain.Certificaciones;

/// <summary>Clases de certificados y constancias que expide la institución.</summary>
public enum TipoCertificado
{
    Estudios = 1,
    Matricula = 2,
    Conducta = 3,
    PazYSalvo = 4,
    Asistencia = 5,
    Reconocimiento = 6,
    Laboral = 7,
    Personalizado = 8,
}

/// <summary>Documento de identidad del beneficiario.</summary>
public enum TipoDocumento
{
    RegistroCivil = 1,
    TarjetaIdentidad = 2,
    CedulaCiudadania = 3,
    CedulaExtranjeria = 4,
    PermisoProteccionTemporal = 5,
    Pasaporte = 6,
    Nuip = 7,
}
