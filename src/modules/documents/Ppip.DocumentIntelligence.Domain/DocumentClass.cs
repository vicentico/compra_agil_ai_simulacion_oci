namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Clasificación de un PDF (FR-012, docs/09-document-intelligence/01-document-pipeline.md §Clasificación).</summary>
public enum DocumentClass
{
    Textual,
    Scanned,
    Mixed,
    Tables,
    Images,
    Complex,
}
