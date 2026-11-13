using FluentValidation;

namespace RabiGamaliel.Application.Common;

internal static class ValidationExtensions
{
    /// <summary>Valida la solicitud y devuelve un <see cref="Error"/> de validación o null si es válida.</summary>
    public static async Task<Error?> ValidarAsync<T>(this IValidator<T> validator, T instancia, CancellationToken ct)
    {
        var resultado = await validator.ValidateAsync(instancia, ct);
        if (resultado.IsValid)
        {
            return null;
        }

        return Error.Validacion(
            resultado.Errors
                .GroupBy(e => e.PropertyName)
                .Select(g => KeyValuePair.Create(g.Key, g.Select(e => e.ErrorMessage).Distinct().ToArray())));
    }
}
