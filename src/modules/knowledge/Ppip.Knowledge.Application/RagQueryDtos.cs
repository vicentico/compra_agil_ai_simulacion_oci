using Ppip.Knowledge.Domain;

namespace Ppip.Knowledge.Application;

/// <summary>Request de UC-005 (docs/06-api/01-example-rag-query.md). El <c>compraAgilId</c> lo inyecta el servidor desde la ruta — nunca lo decide el cliente (ADR-008).</summary>
public sealed record RagQueryRequest(string CompraAgilId, string Question, int TopK);

public sealed record EvidenceItem(
    string DocumentId,
    int DocumentVersion,
    string? DocumentName,
    int Page,
    string ChunkId,
    string SourceText,
    double Score,
    double Confidence);

public sealed record ExecutionInfo(string Model, string PromptVersion, int TokensIn, int TokensOut, long LatencyMs);

/// <summary>Resultado de UC-005: ninguna mutación de dominio, solo lectura + auditoría (AIExecution).</summary>
public sealed record RagAnswer(
    string Answer,
    AnswerType AnswerType,
    IReadOnlyList<EvidenceItem> Evidence,
    bool Unanswered,
    ExecutionInfo? Execution,
    string CorrelationId);
