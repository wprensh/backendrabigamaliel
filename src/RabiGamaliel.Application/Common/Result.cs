using System.Diagnostics.CodeAnalysis;

namespace RabiGamaliel.Application.Common;

public enum ErrorType
{
    Validacion,
    NoEncontrado,
    Conflicto,
    NoAutorizado,
}

/// <summary>Error de negocio tipado. La capa API lo traduce a ProblemDetails (RFC 9457).</summary>
public sealed record Error(string Codigo, string Mensaje, ErrorType Tipo)
{
    public IReadOnlyDictionary<string, string[]> ErroresValidacion { get; init; } =
        new Dictionary<string, string[]>();

    public static Error NoEncontrado(string codigo, string mensaje) => new(codigo, mensaje, ErrorType.NoEncontrado);

    public static Error Conflicto(string codigo, string mensaje) => new(codigo, mensaje, ErrorType.Conflicto);

    public static Error NoAutorizado(string codigo, string mensaje) => new(codigo, mensaje, ErrorType.NoAutorizado);

    /// <summary>Error de validación de un solo campo.</summary>
    public static Error Campo(string campo, string mensaje) => Validacion(KeyValuePair.Create(campo, new[] { mensaje }));

    /// <summary>
    /// Crea un error de validación. Usa una colección <c>params</c> de C# 13, que acepta
    /// cualquier <see cref="IEnumerable{T}"/> (listas, arrays, expresiones de colección) sin copias extra.
    /// </summary>
    public static Error Validacion(params IEnumerable<KeyValuePair<string, string[]>> errores) =>
        new("Validacion", "Uno o más campos no son válidos.", ErrorType.Validacion)
        {
            ErroresValidacion = errores.ToDictionary(),
        };
}

/// <summary>
/// Resultado de una operación de la capa de aplicación (patrón Result).
/// Evita usar excepciones para el control de flujo de errores esperados.
/// </summary>
public sealed class Result<T>
{
    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error? Error { get; }

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}

/// <summary>Atajos para crear resultados.</summary>
public static class Resultado
{
    /// <summary>Éxito explícito: la conversión implícita no aplica cuando el valor es una interfaz (p. ej. IReadOnlyList).</summary>
    public static Result<T> Exito<T>(T value) => value;
}

/// <summary>Valor vacío para operaciones que no devuelven datos (p. ej. eliminar).</summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
