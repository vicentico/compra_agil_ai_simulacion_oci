namespace Ppip.Knowledge.Domain;

/// <summary>
/// Clasificación de una afirmación IA (docs/03-domain/02: reusado por RAG,
/// FASE 9, y AIAnalysis, FASE 10). Una afirmación sin chunk de respaldo
/// nunca es FACT — ADR-008: "afirmaciones sin chunk de respaldo →
/// Información no encontrada en las fuentes analizadas".
/// </summary>
public enum AnswerType
{
    Fact,
    Inference,
    Recommendation,
    Unknown,
}
