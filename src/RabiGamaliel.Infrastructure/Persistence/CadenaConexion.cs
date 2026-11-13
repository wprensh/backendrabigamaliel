using Npgsql;

namespace RabiGamaliel.Infrastructure.Persistence;

/// <summary>
/// Acepta la cadena de conexión en los dos formatos habituales de PostgreSQL:
/// <list type="bullet">
///   <item>Npgsql: <c>Host=...;Database=...;Username=...;Password=...</c></item>
///   <item>URL, como la que entregan Neon, Render o Supabase: <c>postgresql://usuario:clave@host/base?sslmode=require</c></item>
/// </list>
/// Así se puede pegar la cadena del proveedor tal cual en la variable de entorno.
/// </summary>
public static class CadenaConexion
{
    public static string Normalizar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        valor = valor.Trim();

        if (!valor.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !valor.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return valor;
        }

        var uri = new Uri(valor);
        var credenciales = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credenciales[0]),
            Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : null,
            // Los proveedores administrados exigen conexión cifrada.
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }
}
