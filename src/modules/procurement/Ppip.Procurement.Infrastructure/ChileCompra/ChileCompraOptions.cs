namespace Ppip.Procurement.Infrastructure.ChileCompra;

/// <summary>
/// Config esperada: <c>Ppip:ChileCompra:BaseUrl</c> (default
/// https://api2.mercadopublico.cl) y <c>Ppip:ChileCompra:Ticket</c> — el
/// ticket es personal y de uso limitado (ASM-01); NUNCA se versiona, viene
/// de <c>CHILECOMPRA_TICKET</c> en <c>.env</c> (gitignored).
/// </summary>
public sealed class ChileCompraOptions
{
    public const string SectionName = "Ppip:ChileCompra";

    public string BaseUrl { get; set; } = "https://api2.mercadopublico.cl";

    public string Ticket { get; set; } = string.Empty;
}
