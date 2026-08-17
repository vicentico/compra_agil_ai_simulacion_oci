using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Keycloak;
using Xunit;

namespace Ppip.PlatformApi.Tests;

/// <summary>
/// Un solo Keycloak real (Testcontainers) + un solo WebApplicationFactory
/// compartidos por todos los tests de <see cref="AuthorizationMatrixTests"/>
/// — evita levantar 12 contenedores Keycloak (uno por [Fact]/[Theory] case).
/// </summary>
public sealed class PlatformApiAuthFixture : IAsyncLifetime
{
    public const string TestClientId = "ppip-test-client";
    public const string TestClientSecret = "ppip-test-client-secret-not-for-production";
    public const string TestUserPassword = "PpipTest123!";

    // Misma imagen que infrastructure/docker/docker-compose.yml (servicio keycloak).
    // WithRealm ya agrega "--import-realm" al comando ("start-dev" viene del
    // default del builder) — llamar WithCommand("start-dev", ...) aparte
    // duplica el token y rompe el parseo de argumentos de kc.sh.
    private readonly KeycloakContainer _keycloak = new KeycloakBuilder("quay.io/keycloak/keycloak:26.0")
        .WithRealm(Path.Combine(AppContext.BaseDirectory, "ppip-realm.json"))
        .Build();

    private WebApplicationFactory<Program> _factory = default!;
    private HttpClient _keycloakClient = default!;
    private string _authority = default!;

    public HttpClient ApiClient { get; private set; } = default!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await _keycloak.StartAsync();
        // GetBaseAddress() ya trae "/" final.
        _authority = $"{_keycloak.GetBaseAddress().TrimEnd('/')}/realms/ppip";
        _keycloakClient = new HttpClient();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ppip:Auth:Authority"] = _authority,
                    ["Ppip:Auth:Issuer"] = _authority,
                    ["Ppip:Auth:Audience"] = "ppip-platform-api",
                });
            });
        });
        ApiClient = _factory.CreateClient();
    }

    public async Task<string> GetAccessTokenAsync(string username)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_authority}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = TestClientId,
                ["client_secret"] = TestClientSecret,
                ["username"] = username,
                ["password"] = TestUserPassword,
                ["scope"] = "openid",
            }),
        };

        var response = await _keycloakClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Token request for '{username}' failed ({(int)response.StatusCode}): {body}");
        }

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public async Task DisposeAsync()
    {
        ApiClient.Dispose();
        _keycloakClient.Dispose();
        await _factory.DisposeAsync();
        await _keycloak.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PlatformApiAuthCollection : ICollectionFixture<PlatformApiAuthFixture>
{
    public const string Name = "PlatformApiAuth";
}
