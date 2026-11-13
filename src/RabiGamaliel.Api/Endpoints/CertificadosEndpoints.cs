using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Certificaciones;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Certificaciones;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Certificados con el membrete institucional y su verificación pública por código.</summary>
internal static class CertificadosEndpoints
{
    public const string RateLimitPolicy = "verificacion-certificados";

    public static IEndpointRouteBuilder MapCertificadosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/certificados").WithTags("Certificados");

        // Público: cualquiera que reciba un certificado puede comprobar que es auténtico.
        // Limitado por IP para que no se puedan recorrer códigos al azar.
        group.MapGet("/verificar/{codigo}", VerificarAsync)
            .WithName("VerificarCertificado")
            .WithSummary("Verifica la autenticidad de un certificado por su código.")
            .RequireRateLimiting(RateLimitPolicy);

        // Contienen datos personales de estudiantes (Ley 1581): según los permisos del módulo Certificados.
        // Un certificado no se borra: "eliminar" equivale a anularlo.
        group.MapGet("/", ListarAsync)
            .WithName("ListarCertificados")
            .WithSummary("Lista los certificados expedidos.")
            .RequierePermiso(ModuloSistema.Certificados, AccionPermiso.Ver);

        group.MapGet("/{id:guid}", ObtenerAsync)
            .WithName("ObtenerCertificado")
            .WithSummary("Obtiene un certificado por su id.")
            .RequierePermiso(ModuloSistema.Certificados, AccionPermiso.Ver);

        group.MapPost("/", ExpedirAsync)
            .WithName("ExpedirCertificado")
            .WithSummary("Expide un certificado y le asigna su código de verificación.")
            .RequierePermiso(ModuloSistema.Certificados, AccionPermiso.Crear);

        group.MapPut("/{id:guid}/anulacion", AnularAsync)
            .WithName("AnularCertificado")
            .WithSummary("Anula un certificado expedido.")
            .RequierePermiso(ModuloSistema.Certificados, AccionPermiso.Eliminar);

        return app;
    }

    private static async Task<Ok<PagedResult<CertificadoResponse>>> ListarAsync(
        ICertificadoService service,
        CancellationToken ct,
        string? buscar = null,
        TipoCertificado? tipo = null,
        int pagina = 1,
        int tamanoPagina = 10) =>
        TypedResults.Ok(await service.ListarAsync(new CertificadoQuery(buscar, tipo, pagina, tamanoPagina), ct));

    private static async Task<Results<Ok<CertificadoResponse>, ProblemHttpResult>> ObtenerAsync(
        Guid id, ICertificadoService service, CancellationToken ct)
    {
        var resultado = await service.ObtenerAsync(id, ct);
        return resultado.Match<Results<Ok<CertificadoResponse>, ProblemHttpResult>>(
            certificado => TypedResults.Ok(certificado),
            error => error.ToProblem());
    }

    private static async Task<Results<CreatedAtRoute<CertificadoResponse>, ProblemHttpResult>> ExpedirAsync(
        ExpedirCertificadoRequest request, ICertificadoService service, CancellationToken ct)
    {
        var resultado = await service.ExpedirAsync(request, ct);
        return resultado.Match<Results<CreatedAtRoute<CertificadoResponse>, ProblemHttpResult>>(
            certificado => TypedResults.CreatedAtRoute(certificado, "ObtenerCertificado", new { id = certificado.Id }),
            error => error.ToProblem());
    }

    private static async Task<Results<Ok<CertificadoResponse>, ProblemHttpResult>> AnularAsync(
        Guid id, AnularCertificadoRequest request, ICertificadoService service, CancellationToken ct)
    {
        var resultado = await service.AnularAsync(id, request, ct);
        return resultado.Match<Results<Ok<CertificadoResponse>, ProblemHttpResult>>(
            certificado => TypedResults.Ok(certificado),
            error => error.ToProblem());
    }

    private static async Task<Results<Ok<VerificacionCertificadoResponse>, ProblemHttpResult>> VerificarAsync(
        string codigo, ICertificadoService service, CancellationToken ct)
    {
        var resultado = await service.VerificarAsync(codigo, ct);
        return resultado.Match<Results<Ok<VerificacionCertificadoResponse>, ProblemHttpResult>>(
            verificacion => TypedResults.Ok(verificacion),
            error => error.ToProblem());
    }
}
