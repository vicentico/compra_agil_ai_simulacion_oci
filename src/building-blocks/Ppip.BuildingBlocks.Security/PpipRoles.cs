namespace Ppip.BuildingBlocks.Security;

/// <summary>
/// Jerarquía RBAC de ADR-010 (+ Amendment): viewer &lt; analyst &lt; editor &lt;
/// admin &lt; superadmin. La jerarquía se modela como roles compuestos en el
/// realm de Keycloak (infrastructure/docker/config/keycloak/ppip-realm.json)
/// — un token de "admin" ya trae "editor"/"analyst"/"viewer" en
/// realm_access.roles, así que `RequireRole(Analyst)` alcanza para admin sin
/// lógica de jerarquía en este código.
/// </summary>
public static class PpipRoles
{
    public const string Viewer = "viewer";
    public const string Analyst = "analyst";
    public const string Editor = "editor";
    public const string Admin = "admin";
    public const string SuperAdmin = "superadmin";

    public static readonly IReadOnlyList<string> All = [Viewer, Analyst, Editor, Admin, SuperAdmin];
}
