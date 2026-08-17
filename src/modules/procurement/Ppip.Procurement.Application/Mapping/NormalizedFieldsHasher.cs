using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ppip.Procurement.Domain;

namespace Ppip.Procurement.Application.Mapping;

/// <summary>
/// Hash de los campos normalizados que <see cref="CompraAgil"/> realmente
/// versiona y puede reaplicar (título, monto, vigencia, estado —
/// exactamente lo que <c>ApplyUpdate</c>/<c>AlinearEstado</c> saben cambiar;
/// la institución se fija una sola vez en <c>Detect</c> y el agregado no
/// expone forma de reasignarla, así que incluirla aquí podría producir un
/// hash distinto sin ningún campo comparado que realmente cambie) —
/// deliberadamente NO es <c>RawCompraAgilPayload.ResponseHash</c> (hash del
/// JSON crudo completo).
///
/// Hallazgo al construir el orquestador (FASE 6): el JSON crudo de ChileCompra
/// trae campos que cambian en casi cada poll sin que nada relevante haya
/// cambiado (p.ej. <c>fecha_ultimo_cambio</c>, <c>total_ofertas_recibidas</c>).
/// Si <see cref="SyncPolicy"/> comparara el hash del crudo completo, casi todo
/// ciclo generaría una "actualización" espuria — y peor, con
/// <c>changedFields</c> vacío (viola el schema de <c>CompraAgilUpdated.v1</c>,
/// que exige al menos un campo). <c>RawCompraAgilPayload.ResponseHash</c> se
/// sigue calculando y persistiendo (auditoría del crudo, docs/08-data), pero
/// la decisión Create/Update/NoOp la gobierna este hash, no aquél.
/// </summary>
public static class NormalizedFieldsHasher
{
    public static string Compute(string titulo, Money montoEstimado, DateRange vigencia, EstadoCompra estado)
    {
        var canonical = string.Join(
            '|',
            titulo,
            montoEstimado.Amount.ToString(CultureInfo.InvariantCulture),
            montoEstimado.Currency,
            vigencia.Start.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            vigencia.End.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            estado.ToString());

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
