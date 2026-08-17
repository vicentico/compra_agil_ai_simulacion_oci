namespace Ppip.Procurement.Domain.Ports;

/// <summary>Puerto del agregado <see cref="Institution"/> (NFR-013). Adaptador Mongo real en FASE 6.</summary>
public interface IInstitutionRepository
{
    Task<Institution?> FindAsync(string codigoOficial, CancellationToken cancellationToken = default);

    Task SaveAsync(Institution institution, CancellationToken cancellationToken = default);
}
