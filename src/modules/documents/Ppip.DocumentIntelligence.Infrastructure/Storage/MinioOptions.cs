namespace Ppip.DocumentIntelligence.Infrastructure.Storage;

/// <summary>
/// Config esperada: <c>Ppip:MinIo:Endpoint</c> (ya usado por el health check
/// HTTP desde FASE 1, formato URL completa p.ej. <c>http://minio:9000</c> —
/// se reutiliza tal cual, no se introduce un formato nuevo) +
/// <c>Ppip:MinIo:AccessKey</c>/<c>SecretKey</c> (nuevos en FASE 7, mismas
/// credenciales que <c>MINIO_ROOT_USER</c>/<c>MINIO_ROOT_PASSWORD</c>).
/// </summary>
public sealed class MinioOptions
{
    public const string SectionName = "Ppip:MinIo";

    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}
