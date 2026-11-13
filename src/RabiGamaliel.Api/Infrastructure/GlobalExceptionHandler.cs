using Microsoft.AspNetCore.Diagnostics;

namespace RabiGamaliel.Api.Infrastructure;

/// <summary>
/// Manejo centralizado de excepciones no controladas (<see cref="IExceptionHandler"/>).
/// Registra el error y responde ProblemDetails sin exponer detalles internos al cliente.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Una solicitud mal formada (cuerpo o formulario que no se puede leer) es error del cliente, no del servidor.
        if (exception is BadHttpRequestException solicitudInvalida)
        {
            httpContext.Response.StatusCode = solicitudInvalida.StatusCode;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Title = "La solicitud no tiene el formato esperado.",
                    Status = solicitudInvalida.StatusCode,
                },
            });
        }

        LogExcepcion(logger, httpContext.Request.Method, httpContext.Request.Path, exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "Ocurrió un error inesperado.",
                Status = StatusCodes.Status500InternalServerError,
            },
        });
    }

    // Logging de alto rendimiento generado en compilación (LoggerMessage source generator).
    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en {Metodo} {Ruta}")]
    private static partial void LogExcepcion(ILogger logger, string metodo, string ruta, Exception exception);
}
