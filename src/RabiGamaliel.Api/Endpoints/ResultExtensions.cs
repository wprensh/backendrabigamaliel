using RabiGamaliel.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Traduce los <see cref="Error"/> de la capa de aplicación a respuestas HTTP ProblemDetails.</summary>
internal static class ResultExtensions
{
    public static ProblemHttpResult ToProblem(this Error error) => error.Tipo switch
    {
        ErrorType.Validacion => TypedResults.Problem(new HttpValidationProblemDetails(
            error.ErroresValidacion.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Title = error.Mensaje,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["code"] = error.Codigo },
        }),
        ErrorType.NoEncontrado => Problem(StatusCodes.Status404NotFound, error),
        ErrorType.Conflicto => Problem(StatusCodes.Status409Conflict, error),
        ErrorType.NoAutorizado => Problem(StatusCodes.Status401Unauthorized, error),
        _ => Problem(StatusCodes.Status500InternalServerError, error),
    };

    private static ProblemHttpResult Problem(int status, Error error) =>
        TypedResults.Problem(
            title: error.Mensaje,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Codigo });
}
