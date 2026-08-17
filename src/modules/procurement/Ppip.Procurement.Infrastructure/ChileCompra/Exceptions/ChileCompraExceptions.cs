namespace Ppip.Procurement.Infrastructure.ChileCompra.Exceptions;

/// <summary>Base de todos los errores mapeados de la API Compra Ágil v2 (§7).</summary>
public abstract class ChileCompraException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>400 — parámetros de consulta inválidos.</summary>
public sealed class ChileCompraBadRequestException(string mensaje, string? detalle)
    : ChileCompraException(detalle is null ? mensaje : $"{mensaje} ({detalle})")
{
    public string? Detalle { get; } = detalle;
}

/// <summary>401 — falta el header ticket.</summary>
public sealed class ChileCompraUnauthorizedException(string mensaje) : ChileCompraException(mensaje);

/// <summary>403 — el ticket no existe, está inactivo o fue bloqueado.</summary>
public sealed class ChileCompraForbiddenException(string mensaje) : ChileCompraException(mensaje);

/// <summary>404 — no existe Compra Ágil con el código indicado (endpoint detalle).</summary>
public sealed class ChileCompraNotFoundException(string codigo)
    : ChileCompraException($"No existe Compra Ágil con código '{codigo}'.")
{
    public string Codigo { get; } = codigo;
}

/// <summary>429 — se agotó la cuota diaria del ticket (§4).</summary>
public sealed class ChileCompraRateLimitedException(string mensaje, TimeSpan? retryAfter)
    : ChileCompraException(mensaje)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>500/503 — error del lado del servidor de ChileCompra.</summary>
public sealed class ChileCompraServerException(int statusCode, string mensaje)
    : ChileCompraException($"Error {statusCode} de la API Compra Ágil: {mensaje}")
{
    public int StatusCode { get; } = statusCode;
}
