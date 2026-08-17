namespace Ppip.Procurement.Domain;

public enum SyncDecision
{
    Create,
    Update,
    NoOp,
}

/// <summary>
/// Decide creación/actualización/no-op comparando el hash del payload
/// entrante contra la copia local (UC-001 pasos 6-8). Puro: sin acceso a
/// infraestructura — la resolución de "existing" (buscar por id) es
/// responsabilidad de la capa de aplicación.
/// </summary>
public static class SyncPolicy
{
    public static SyncDecision Decide(CompraAgil? existing, string incomingPayloadHash)
    {
        if (string.IsNullOrWhiteSpace(incomingPayloadHash))
        {
            throw new ArgumentException("El hash del payload entrante es obligatorio.", nameof(incomingPayloadHash));
        }

        if (existing is null)
        {
            return SyncDecision.Create;
        }

        return existing.RawPayloadHash == incomingPayloadHash ? SyncDecision.NoOp : SyncDecision.Update;
    }
}
