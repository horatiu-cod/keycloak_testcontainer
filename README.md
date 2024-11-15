# Asp.Net Core and Keycloak testcontainer 
Testing a secure Asp.Net Core Api using Keycloak Testcontainer

## Solution and Projects setup
Create a new solution
```powershell
dotnet new sln -n KeycloakTestcontainer
```
### API project setup
Create and add a MinimalApi project to the solution
```powershell
dotnet new webapi -n KeycloakTestcontainer.Api
dotnet sln add ./KeycloakTestcontainer.Api
```
Add package Microsoft.AspNetCore.Authentication.JwtBearer for token validation. Change the version as required.
```powershell
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version x.x.x
```
Add Authentication and Authorization to program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);
👇
// the realm and the client configured in the Keycloak server
var realm = "myrealm";
var client = "myclient";

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://localhost:8443/realms/{realm}";
        options.Audience = $"{client}";
    });
builder.Services.AddAuthorization();
👆
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();
👇
app.UseAuthentication();
app.UseAuthorization();
👆
app.Run();
```
Add the secure endpoint.
```csharp
var builder = WebApplication.CreateBuilder(args);

// the realm and the client configured in the Keycloak server
var realm = "myrealm";
var client = "myclient";

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://localhost:8443/realms/{realm}";
        options.Audience = $"{client}";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
👇
app.MapGet("api/authenticate", () =>
    Results.Ok($"{System.Net.HttpStatusCode.OK} authenticated"))
    .RequireAuthorization();
👆
app.Run();
```
Add ``IApiMarker.cs`` interface to the root of KeycloakTestcontainer.Api project.

It will be used as entry point of the ```WebApplicationFactory<IApiMarker>```
 
<img width="250" alt="iapimarker" src="https://github.com/user-attachments/assets/f81e22f5-07af-42ff-a9a8-2ee04629f416">

### Test project setup
Create and add a xUnit test project to the solution
```powershell
dotnet new xunit -n KeycloakTestcontainer.Test
dotnet sln add ./KeycloakTestcontainer.Test
```
Add reference to KeycloakTestcontainer.Api project
```powershell
cd KeycloakTestcontainer.Test
dotnet add reference ../KeycloakTestcontainer.Api
```
Add package Testcontainers.Keycloak KeycloakTestcontainer.Test.  Change the version as required.
```powershell
dotnet add package Testcontainers.Keycloak --version x.x.x
```
Add package Microsoft.AspNetCore.Mvc.Testing. It will spin up an in memory web api for testing. Change the version as required.
```powershell
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version x.x.x
```
Add package dotnet add package FluentAssertions. Change the version as required.
```powershell
dotnet add package FluentAssertions --version x.x.x
```
Add ``ApiFactoryFixture.cs`` class to the KeycloakTestcontainer.Test project.

<img width="250" alt="image" src="https://github.com/user-attachments/assets/106d1875-22bd-4b58-9495-0c5118b58ac0">

Add the following code to ApiFactoryFixture 
```csharp
using DotNet.Testcontainers.Builders;
using KeycloakTestcontainer.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Keycloak;

namespace KeycloakTestcontainer.Test;

public class ApiFactoryFixture : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public string? BaseAddress { get; set; } = "https://localhost:8443";

    private readonly KeycloakContainer _container = new KeycloakBuilder()
        .WithImage("keycloak/keycloak:26.0")
        .WithPortBinding(8443, 8443)
        //map the realm configuration file import.json.
        .WithResourceMapping("./Integration/import/import.json", "/opt/keycloak/data/import")
        //
        .WithResourceMapping("./Integration/Certs", "/opt/keycloak/certs")
        .WithCommand("--import-realm")
        .WithEnvironment("KC_HTTPS_CERTIFICATE_FILE", "/opt/keycloak/certs/cert.pem")
        .WithEnvironment("KC_HTTPS_CERTIFICATE_KEY_FILE", "/opt/keycloak/certs/key.key")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8443))
        .WithClean(true)
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
```
Add ``ApiFactoryFixtureCollection.cs`` class. Using xUnit fixture collection, only a single Keycloak container will be created for all the tests.

<img width="208" alt="image" src="https://github.com/user-attachments/assets/5d5f0c02-c565-48d1-9719-b5da54cdf73c">

Add the following code to it.
```csharp
namespace KeycloakTestcontainer.Test;

[CollectionDefinition(nameof(ApiFactoryFixtureCollection))]
public class ApiFactoryFixtureCollection : ICollectionFixture<ApiFactoryFixture>
{
}
```
Now let's create the ``AuthenticateEndpointTests.cs`` test class.

<img width="209" alt="image" src="https://github.com/user-attachments/assets/bdd272fa-edf2-4d95-98b9-a31a265db983">

Add the following code to it.
```csharp
using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace KeycloakTestcontainer.Test;

[Collection(nameof(ApiFactoryFixtureCollection))]
public class AuthenticateEndpointTests(ApiFactoryFixture apiFactory)
{
    //create api http client
    private readonly HttpClient _httpClient = apiFactory.CreateClient();
    //http client to call Keycloak server
    private readonly HttpClient _client = new();
    private readonly string _baseAddress = apiFactory.BaseAddress ?? string.Empty;

    [Fact]
    public async Task AuthenticateEndpoint_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        //Arrange
        //The realm and the client configured in the Keycloak server
        var realm = "myrealm";
        var client = "myclient";

        //Keycloak server token endpoint
        var url = $"{_baseAddress}/realms/{realm}/protocol/openid-connect/token";
        //Api secure endpoint 
        var apiUrl = "api/authenticate";

        //Create the url encoded body
        var data = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", $"{client}" },
            { "username", "h@g.com" },
            { "password", "s3cr3t" }
        };

        //Get the access token from the Keycloak server
        var response = await _client.PostAsync(url, new FormUrlEncodedContent(data));
        var content = await response.Content.ReadFromJsonAsync<JsonObject>();
        var token = content?["access_token"]?.ToString();

        //Act
        //Add the access token to request header
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //Call the Api secure endpoint
        var result = await _httpClient.GetAsync(apiUrl);

        //Assert
        result.IsSuccessStatusCode.Should().BeTrue();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
```

## Keycloak container setup

Requirements: docker installed.

Pull the docker image
```powershell
docker pull keycloak/keycloak:26.0
```
To avoid the ```ERR_SSL_PROTOCOL_ERROR``` in the browser , will use the developer certificates for https connection.

Create a Crets folder in KeycloakTestcontainer.Test. Will store the certificates here.

<img width="206" alt="image" src="https://github.com/user-attachments/assets/9e8be60d-6a91-4ea6-b8d1-5694c4f16d23">

Open an terminal and navigate to the folder.
Create a certificate, trust it, and export it to a PEM file including the private key:
```powershell
dotnet dev-certs https -ep ./certificate.crt -p $YOUR_PASSWORD$ --trust --format PEM
```
Command will generate two files, certificate.pem and certificate.key. Do not forget to add .pem and .key extensions to .gitignore. 

<img width="205" alt="image" src="https://github.com/user-attachments/assets/e758c63f-060d-4d38-9d8f-22f097da55c8">

Let's create a docker compose file for the initial setup of the Keycloak realm, client and users.
Add the ``docker-compose.yml`` file to KeycloakTestcontainer.Test project.

<img width="201" alt="image" src="https://github.com/user-attachments/assets/cd6a0a8f-e05b-4e84-af8d-2940e276a5cd">

```yaml
services:
  keycloak_server:
    image:  keycloak/keycloak:26.0
    container_name: keycloak
    command:  start-dev
    environment:
      KC_BOOTSTRAP_ADMIN_USERNAME: admin
      KC_BOOTSTRAP_ADMIN_PASSWORD: admin
      KC_HTTPS_CERTIFICATE_FILE: /opt/keycloak/certs/certificate.pem
      KC_HTTPS_CERTIFICATE_KEY_FILE: /opt/keycloak/certs/certificate.key
    ports:
      - "8080:8080"
      - "8443:8443"
    volumes:
      - ./Certs:/opt/keycloak/certs
    networks:
      - keycloak_network

networks:
  keycloak_network:
    driver: bridge
```


