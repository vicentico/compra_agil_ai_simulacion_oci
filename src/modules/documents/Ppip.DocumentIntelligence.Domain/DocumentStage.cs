namespace Ppip.DocumentIntelligence.Domain;

/// <summary>
/// Etapa del documento dentro de UC-003 pasos 1-3 (descarga/storage — FASE 7;
/// clasificación/extracción/OCR llegan en FASE 8, con sus propios estados).
/// </summary>
public enum DocumentStage
{
    Detected,
    Downloaded,
    DownloadFailed,
    RejectedByPolicy,
}
