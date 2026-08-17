using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>
/// Agregado raíz de Procurement (docs/03-domain/02-domain-model.md, UC-001).
/// Toda versión normalizada deriva de un <see cref="RawCompraAgilPayload"/>
/// identificado por <see cref="RawPayloadHash"/> — la decisión de
/// crear/actualizar/no-op la toma <see cref="SyncPolicy"/> comparando ese hash.
/// </summary>
public sealed class CompraAgil : AggregateRoot<CompraAgilId>
{
    public InstitutionRef Institution { get; private set; }
    public string Titulo { get; private set; }
    public Money MontoEstimado { get; private set; }
    public DateRange Vigencia { get; private set; }
    public EstadoCompra Estado { get; private set; }
    public int Version { get; private set; }
    public string RawPayloadHash { get; private set; }
    public DateTimeOffset UltimaActualizacion { get; private set; }

    private readonly List<ProductRequirement> _requirements;
    public IReadOnlyList<ProductRequirement> Requirements => _requirements;

    private CompraAgil(
        CompraAgilId id,
        InstitutionRef institution,
        string titulo,
        Money montoEstimado,
        DateRange vigencia,
        string rawPayloadHash,
        IEnumerable<ProductRequirement> requirements)
        : base(id)
    {
        Institution = institution;
        Titulo = titulo;
        MontoEstimado = montoEstimado;
        Vigencia = vigencia;
        Estado = EstadoCompra.Publicada;
        Version = 1;
        RawPayloadHash = rawPayloadHash;
        UltimaActualizacion = DateTimeOffset.UtcNow;
        _requirements = [.. requirements];
    }

    /// <summary>Reconstruye el agregado tal como quedó persistido — no levanta eventos (no es un hecho de negocio, es una lectura).</summary>
    private CompraAgil(
        CompraAgilId id,
        InstitutionRef institution,
        string titulo,
        Money montoEstimado,
        DateRange vigencia,
        EstadoCompra estado,
        int version,
        string rawPayloadHash,
        DateTimeOffset ultimaActualizacion,
        IEnumerable<ProductRequirement> requirements)
        : base(id)
    {
        Institution = institution;
        Titulo = titulo;
        MontoEstimado = montoEstimado;
        Vigencia = vigencia;
        Estado = estado;
        Version = version;
        RawPayloadHash = rawPayloadHash;
        UltimaActualizacion = ultimaActualizacion;
        _requirements = [.. requirements];
    }

    /// <summary>Usado por los repositorios (FASE 6) para reconstruir el agregado desde almacenamiento.</summary>
    public static CompraAgil Rehydrate(
        CompraAgilId id,
        InstitutionRef institution,
        string titulo,
        Money montoEstimado,
        DateRange vigencia,
        EstadoCompra estado,
        int version,
        string rawPayloadHash,
        DateTimeOffset ultimaActualizacion,
        IEnumerable<ProductRequirement> requirements) =>
        new(id, institution, titulo, montoEstimado, vigencia, estado, version, rawPayloadHash, ultimaActualizacion, requirements);

    /// <summary>Primera vez que se ve esta Compra Ágil (UC-001 paso 6) — levanta <see cref="CompraAgilDetected"/>.</summary>
    public static CompraAgil Detect(
        CompraAgilId id,
        InstitutionRef institution,
        string titulo,
        Money montoEstimado,
        DateRange vigencia,
        string rawPayloadHash,
        IEnumerable<ProductRequirement> requirements,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        }

        if (string.IsNullOrWhiteSpace(rawPayloadHash))
        {
            throw new ArgumentException("El hash del payload crudo es obligatorio.", nameof(rawPayloadHash));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio.", nameof(correlationId));
        }

        var compra = new CompraAgil(id, institution, titulo.Trim(), montoEstimado, vigencia, rawPayloadHash, requirements);
        compra.Raise(new CompraAgilDetected(Guid.CreateVersion7(), DateTimeOffset.UtcNow, id.Value, rawPayloadHash, correlationId));
        return compra;
    }

    /// <summary>
    /// Reaplica los datos normalizados de un payload más reciente (UC-001
    /// pasos 7-8). Si el hash no cambió, es un no-op explícito: sin escritura
    /// ni evento (regla "sin cambio no genera escritura").
    /// </summary>
    public void ApplyUpdate(
        string titulo,
        Money montoEstimado,
        DateRange vigencia,
        string newRawPayloadHash,
        IEnumerable<ProductRequirement> requirements,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(newRawPayloadHash))
        {
            throw new ArgumentException("El hash del payload crudo es obligatorio.", nameof(newRawPayloadHash));
        }

        if (newRawPayloadHash == RawPayloadHash)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio.", nameof(correlationId));
        }

        var changedFields = new List<string>();
        if (Titulo != titulo)
        {
            changedFields.Add(nameof(Titulo));
        }

        if (MontoEstimado != montoEstimado)
        {
            changedFields.Add(nameof(MontoEstimado));
        }

        if (Vigencia != vigencia)
        {
            changedFields.Add(nameof(Vigencia));
        }

        Titulo = titulo.Trim();
        MontoEstimado = montoEstimado;
        Vigencia = vigencia;
        RawPayloadHash = newRawPayloadHash;
        Version++;
        UltimaActualizacion = DateTimeOffset.UtcNow;

        _requirements.Clear();
        _requirements.AddRange(requirements);

        Raise(new CompraAgilUpdated(Guid.CreateVersion7(), DateTimeOffset.UtcNow, Id.Value, Version, changedFields, newRawPayloadHash, correlationId));
    }

    public void Cerrar() => TransitionTo(EstadoCompra.Cerrada);

    public void Adjudicar() => TransitionTo(EstadoCompra.Adjudicada);

    public void DeclararDesierta() => TransitionTo(EstadoCompra.Desierta);

    /// <summary>
    /// Alinea el estado local con el que reporta ChileCompra (UC-001 paso 7),
    /// atravesando estados intermedios si hace falta: la API no siempre
    /// reporta el estado intermedio explícitamente (p.ej. una compra puede
    /// aparecer "desierta" en el primer sync que la ve tras estar cerrada
    /// varios días). No-op si ya coincide; lanza si el destino implica
    /// retroceder (una compra cerrada nunca vuelve a publicada).
    /// </summary>
    public void AlinearEstado(EstadoCompra objetivo)
    {
        if (Estado == objetivo)
        {
            return;
        }

        if (objetivo == EstadoCompra.Cerrada)
        {
            Cerrar();
            return;
        }

        if (objetivo is EstadoCompra.Adjudicada or EstadoCompra.Desierta)
        {
            if (Estado == EstadoCompra.Publicada)
            {
                Cerrar();
            }

            TransitionTo(objetivo);
            return;
        }

        throw new InvalidOperationException($"Transición de estado inválida: {Estado} → {objetivo}.");
    }

    private void TransitionTo(EstadoCompra nuevo)
    {
        if (!EstadoCompraTransitions.IsValid(Estado, nuevo))
        {
            throw new InvalidOperationException($"Transición de estado inválida: {Estado} → {nuevo}.");
        }

        Estado = nuevo;
        UltimaActualizacion = DateTimeOffset.UtcNow;
    }
}
