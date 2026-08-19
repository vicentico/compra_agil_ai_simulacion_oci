namespace Ppip.DocumentIntelligence.Domain;

/// <summary>docs/09-document-intelligence/01: prioridad de cortes de chunking semántico.</summary>
public enum ChunkType
{
    Title,
    Paragraph,
    Table,
    Requirement,
    List,
    Annex,
}
