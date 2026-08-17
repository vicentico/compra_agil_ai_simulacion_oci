namespace Ppip.DocumentIntelligence.Domain.Exceptions;

/// <summary>
/// El destino de conexión real (tras resolver DNS) no pasó la revalidación
/// anti-SSRF/DNS-rebinding de <c>IAttachmentDownloader</c> — a diferencia de
/// un fallo de red, esto es una señal de política, no algo transitorio: el
/// caller debe rechazar el documento (<c>RejectByPolicy</c>), no reintentar.
/// </summary>
public sealed class AttachmentBlockedException(string message) : Exception(message);

/// <summary>El binario supera el tamaño máximo configurado — se corta el stream antes de terminar de bufferear (nunca se descarga completo un adjunto sobredimensionado).</summary>
public sealed class AttachmentTooLargeException(string message) : Exception(message);
