using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Ppip.BuildingBlocks.Security;

/// <summary>
/// Cablea autenticación JWT contra Keycloak (ADR-010): valida firma/issuer/
/// audience vía JWKS, y registra una policy de autorización por rol de
/// <see cref="PpipRoles"/>.
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Config esperada: <c>Ppip:Auth:Authority</c> (URL interna del realm,
    /// para descubrir el JWKS), <c>Ppip:Auth:Issuer</c> (el <c>iss</c> real
    /// del token — puede diferir de Authority si Keycloak resuelve su
    /// hostname externo distinto del DNS interno del contenedor) y
    /// <c>Ppip:Auth:Audience</c>.
    /// </summary>
    public static IHostApplicationBuilder AddPpipKeycloakAuth(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Se configura vía AddOptions<T>().Configure<IConfiguration>(...) en
        // vez de leer builder.Configuration directamente en este método: ese
        // delegate se resuelve de forma perezosa (con la IConfiguration ya
        // completamente mezclada, incluidas fuentes agregadas después de
        // este punto — p.ej. overrides de WebApplicationFactory en tests),
        // no en el momento en que Program.cs llama a este método.
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, config) =>
            {
                var authority = config["Ppip:Auth:Authority"] ?? string.Empty;
                var issuer = config["Ppip:Auth:Issuer"] ?? authority;
                var audience = config["Ppip:Auth:Audience"] ?? string.Empty;

                options.Authority = authority;
                options.MetadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
                options.RequireHttpsMetadata = false; // POC local http-only (ver docs/12-security/01)
                // Sin esto, .NET remapea claims estándar del JWT (p.ej. "sub")
                // a URIs XML legacy (ClaimTypes.NameIdentifier); se preservan
                // los nombres tal como los emite Keycloak.
                options.MapInboundClaims = false;

                // Keycloak (KC_HOSTNAME_STRICT=false) siempre embebe su
                // KC_HOSTNAME externo (p.ej. auth.ppip.localhost) en las URLs
                // propias del discovery document (issuer, jwks_uri...),
                // aunque se lo consulte por el DNS interno del contenedor.
                // curl y varios stacks HTTP resuelven "*.localhost" siempre a
                // loopback (RFC 6761), ignorando DNS — así que seguir
                // jwks_uri tal cual terminaría conectando a sí mismo, no a
                // Keycloak. Se fuerza la conexión física de todo el
                // backchannel de este handler (discovery + JWKS) al
                // host:puerto real de Authority, sin importar qué URL venga
                // en el documento de descubrimiento.
                var authorityUri = new Uri(authority);
                options.BackchannelHttpHandler = new SocketsHttpHandler
                {
                    ConnectCallback = async (_, cancellationToken) =>
                    {
                        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                        try
                        {
                            await socket.ConnectAsync(authorityUri.Host, authorityUri.Port, cancellationToken);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch
                        {
                            socket.Dispose();
                            throw;
                        }
                    },
                };
                // IssuerValidator explícito en vez de ValidIssuer(s): cuando
                // Authority/MetadataAddress están configurados, el pipeline
                // de JwtBearer reconstruye la validación de issuer a partir
                // del documento de descubrimiento OIDC, pisando
                // ValidIssuer/ValidIssuers salvo que se provea un
                // IssuerValidator — ese sí tiene prioridad absoluta.
                options.TokenValidationParameters.IssuerValidator = (issuerFromToken, _, _) =>
                    issuerFromToken == issuer || issuerFromToken == authority
                        ? issuerFromToken
                        : throw new SecurityTokenInvalidIssuerException(
                            $"Issuer '{issuerFromToken}' no coincide con Ppip:Auth:Issuer ('{issuer}') ni Ppip:Auth:Authority ('{authority}').")
                        { InvalidIssuer = issuerFromToken };
                options.TokenValidationParameters.ValidAudience = audience;
                options.Events = new JwtBearerEvents
                {
                    // Keycloak anida los roles del realm en un único claim
                    // "realm_access" (JSON: {"roles":[...]}), no como claims
                    // ClaimTypes.Role individuales. Sin este paso,
                    // RequireRole()/IsInRole() nunca encontrarían nada.
                    OnTokenValidated = context =>
                    {
                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        var realmAccessJson = context.Principal?.FindFirst("realm_access")?.Value;
                        if (identity is null || string.IsNullOrWhiteSpace(realmAccessJson))
                        {
                            return Task.CompletedTask;
                        }

                        using var doc = JsonDocument.Parse(realmAccessJson);
                        if (doc.RootElement.TryGetProperty("roles", out var roles))
                        {
                            foreach (var role in roles.EnumerateArray())
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                            }
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        var authorizationBuilder = builder.Services.AddAuthorizationBuilder();
        foreach (var role in PpipRoles.All)
        {
            authorizationBuilder.AddPolicy(role, policy => policy.RequireRole(role));
        }

        return builder;
    }
}
