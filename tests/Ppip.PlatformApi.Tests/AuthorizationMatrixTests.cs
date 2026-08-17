using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Ppip.PlatformApi.Tests;

/// <summary>
/// Prueba el RBAC de 5 roles (ADR-010 + Amendment) contra un Keycloak real
/// (Testcontainers, no un doble/mock) que importa el mismo
/// infrastructure/docker/config/keycloak/ppip-realm.json que usa el entorno
/// de desarrollo — ver docs/15-testing/01-test-strategy.md ("API": auth real
/// vía Keycloak testcontainer, RBAC 401/403).
/// </summary>
[Collection(PlatformApiAuthCollection.Name)]
public sealed class AuthorizationMatrixTests(PlatformApiAuthFixture fixture)
{
    private async Task<HttpResponseMessage> CallAsync(string path, string? username)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (username is not null)
        {
            var token = await fixture.GetAccessTokenAsync(username);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await fixture.ApiClient.SendAsync(request);
    }

    [Fact]
    public async Task NoToken_WhoAmI_Returns401()
    {
        var response = await CallAsync("/api/diagnostics/whoami", username: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoToken_TraceCheck_Returns401()
    {
        var response = await CallAsync("/api/diagnostics/trace-check", username: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("viewer.test")]
    [InlineData("analyst.test")]
    [InlineData("editor.test")]
    [InlineData("admin.test")]
    [InlineData("superadmin.test")]
    public async Task AnyRole_WhoAmI_Returns200(string username)
    {
        // "viewer" es el mínimo — todo rol de la jerarquía debe alcanzar.
        var response = await CallAsync("/api/diagnostics/whoami", username);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ViewerOnly_TraceCheck_Returns403()
    {
        // viewer no incluye "analyst" en la composición (es el rol más bajo).
        var response = await CallAsync("/api/diagnostics/trace-check", "viewer.test");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("analyst.test")]
    [InlineData("editor.test")]
    [InlineData("admin.test")]
    [InlineData("superadmin.test")]
    public async Task AnalystAndAbove_TraceCheck_Returns200(string username)
    {
        // Todo rol >= analyst debe alcanzar por composición de roles en
        // Keycloak (editor/admin/superadmin heredan "analyst" — sin lógica
        // de jerarquía en el código .NET, ver PpipRoles).
        var response = await CallAsync("/api/diagnostics/trace-check", username);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
