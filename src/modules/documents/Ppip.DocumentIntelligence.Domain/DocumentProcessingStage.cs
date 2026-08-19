namespace Ppip.DocumentIntelligence.Domain;

/// <summary>
/// Etapa del pipeline de inteligencia documental (UC-003 pasos 4-9, FASE 8)
/// sobre una <see cref="DocumentVersion"/> ya descargada. Separada de
/// <see cref="DocumentStage"/> (que cubre solo descarga/storage, FASE 7) a
/// propósito: son máquinas de estado independientes sobre objetos distintos.
/// </summary>
public enum DocumentProcessingStage
{
    Pending,
    Extracted,
    Chunked,
    ProcessingFailed,
}
