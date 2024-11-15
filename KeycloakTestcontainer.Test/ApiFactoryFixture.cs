using DotNet.Testcontainers.Builders;
using KeycloakTestcontainer.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Keycloak;

namespace KeycloakTestcontainer.Test;

public class ApiFactoryFixture : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public string? BaseAddress { get; private set; } = "https://localhost:8443";


    private readonly KeycloakContainer _container = new KeycloakBuilder()
        .WithImage("keycloak/keycloak:26.0")
        .WithPortBinding(8443, 8443)
        .WithResourceMapping("./Import/import.json", "/opt/keycloak/data/import")
        .WithResourceMapping("./Certs", "/opt/keycloak/certs")
        .WithCommand("--import-realm")
        .WithEnvironment("KC_HTTPS_CERTIFICATE_FILE", "/opt/keycloak/certs/certificate.pem")
        .WithEnvironment("KC_HTTPS_CERTIFICATE_KEY_FILE", "/opt/keycloak/certs/certificate.key")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8443))
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.StopAsync();
    }

}
