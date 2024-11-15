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
        .WithResourceMapping("./Import/import.json", "/opt/keycloak/data/import")
        //
        .WithResourceMapping("./Certs", "/opt/keycloak/certs")
        .WithCommand("--import-realm")
        .WithEnvironment("KC_HTTPS_CERTIFICATE_FILE", "/opt/keycloak/certs/certificate.pem")
        .WithEnvironment("KC_HTTPS_CERTIFICATE_KEY_FILE", "/opt/keycloak/certs/certificate.key")
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
    private readonly HttpClient _httpClient = apiFactory.CreateClient();
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
            { "username", "myuser" },
            { "password", "mypassword" }
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
    [Fact]
    public async Task AuthenticateEndpoint_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
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
            { "username", "myuser" },
            { "password", "badpassword" }
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
        result.IsSuccessStatusCode.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
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
    command:  start-dev --import-realm
    environment:
      KC_DB: postgres
      KC_DB_URL_HOST: postgres_keycloak
      KC_DB_URL_DATABASE: keycloak
      KC_DB_USERNAME: admin
      KC_DB_PASSWORD: passw0rd
      KC_BOOTSTRAP_ADMIN_USERNAME: admin
      KC_BOOTSTRAP_ADMIN_PASSWORD: admin
      KC_HTTPS_CERTIFICATE_FILE: /opt/keycloak/certs/certificate.pem
      KC_HTTPS_CERTIFICATE_KEY_FILE: /opt/keycloak/certs/certificate.key
    ports:
      - "8880:8080"
      - "8443:8443"
    depends_on:
      postgres_keycloak:
        condition: service_healthy
    volumes:
      - ./Certs:/opt/keycloak/certs
    networks:
      - keycloak_network
  
  postgres_keycloak:
    image: postgres:16.0
    container_name: postgres
    command: postgres -c 'max_connections=200'
    restart: always
    environment:
      POSTGRES_USER: "admin"
      POSTGRES_PASSWORD: "passw0rd"
      POSTGRES_DB: "keycloak"
    ports:
      - "5433:5432"
    volumes:
      - postgres-datas:/var/lib/postgresql/data
    healthcheck:
     test: "exit 0"
    networks:
      - keycloak_network

volumes:
  postgres-datas:
networks:
  keycloak_network:
    driver: bridge
```
Run the command to spin up the Keycloak container
```powershell
docker compose -f .\docker-compose.yml up -d
```
Open browser and open the ``https://localhost:8443``
You'll be redirected to the login page.

<img width="472" alt="image" src="https://github.com/user-attachments/assets/a551c8b9-33a5-4950-b253-eb54cca42d3e">

Login with username ``admin`` and password ``admin`` 
Create a new realm

<img width="232" alt="image" src="https://github.com/user-attachments/assets/14a1b04c-7dc7-4100-baaf-0b882a590f75">

For simplicity we'll name the realm ``myrealm``. Click **Create**.

<img width="838" alt="image" src="https://github.com/user-attachments/assets/eb73d009-c309-4a01-8d02-b266da7607b4">

Create a user

Initially, the realm has no users. Use these steps to create a user:

Verify that you are still in the myrealm realm, which is shown above the word Manage.

Click **Users** in the left-hand menu. Click **Create new user**.

Fill in the form with the following values:

Username: ``myuser``

Email: ``myuser@email.com``

First name: any first name

Last name: any last name

Click Create.

<img width="743" alt="image" src="https://github.com/user-attachments/assets/51f1ba84-6969-47ed-8594-7e3d9b603e15">

This user needs a password to log in. To set the initial password:

Click Credentials at the top of the page.

Fill in the Set password form with a ``mypassword`` password.

Toggle Temporary to Off so that the user does not need to update this password at the first login.

Click **Save**.

<img width="439" alt="image" src="https://github.com/user-attachments/assets/6cc98b78-a7a4-45f8-bb8d-b7814556ecd3">

Create Client.

Verify that you are still in the myrealm realm, which is shown above the word Manage.

Click **Clients**.

Click **Create client**

Fill in the form with the following values:

Client type: ``OpenID Connect``

Client ID: ``myclient``


<img width="870" alt="image" src="https://github.com/user-attachments/assets/35277a25-3553-4bc5-94c8-44634178c327">

Click **Next**.

Confirm that **Direct access grants** is enabled. For simplicity we'll create a public cllient.


<img width="751" alt="image" src="https://github.com/user-attachments/assets/8b9f7cc1-0f21-472f-8cb7-c644d65fbc65">

Click **Next**.

<img width="841" alt="image" src="https://github.com/user-attachments/assets/2b378983-70f1-48d5-830e-3a06c368a335">

Click **Save**.

By default the Client Audience is not mapped to the token. We have to create and map it.

Click on **Client Scope** on the left menu.

Click **Create client scope** tab button.

<img width="1087" alt="image" src="https://github.com/user-attachments/assets/3d21d3ac-f8e1-414d-a51e-dc52a798e8ba">

Fill in the form with the following values:

Name: ``audience``

Type: ``Default``

Toggle ``Display on consent`` screen to Off

<img width="832" alt="image" src="https://github.com/user-attachments/assets/5566a1b9-5d94-4a64-9c43-8dfab934fe46">

Click **Save**.

<img width="818" alt="image" src="https://github.com/user-attachments/assets/85eace07-003f-4531-bfee-893fe68aff57">

Click **Mapper** tab

Click **Configure new mapper** and select **Audience**

Fill in the form with the following values:

Name: any name

Included Client Audience: select ``myclient``

<img width="545" alt="image" src="https://github.com/user-attachments/assets/5e94605d-0a5b-44b5-94d6-3c5a33f225e1">

Click **Save**

Click **Clients** on nav menu, select ``myclient``.

Click **Add client scope** tab, select audience and click **Add** default.

<img width="421" alt="image" src="https://github.com/user-attachments/assets/87c6f48b-2205-4a88-8439-86bcd72fcbec">

### Export the realm configuration

In order to have this same configuration every time when the testcontainer is started, we will export this realm configuration to a import.josn file. The file will be imported by the test container during start-up.

Add a folder named **Import** to the test project.

<img width="203" alt="image" src="https://github.com/user-attachments/assets/1118d55c-261c-4eb5-aef7-c74f065f2c71">

Open a terminal and navigate to the folder.

Identify the keyclaok container ```docker ps```

Access the container ```docker exec -it (container id) /bin/bash```

Export the realm configuration

```powershell
cd /opt/keycloak/bin
./kc.sh export --file /tmp/(file name).json --realm (realm name)
```

<img width="784" alt="image" src="https://github.com/user-attachments/assets/65351476-f095-4c1a-88e2-a8c9fcd93067">


Copy the file from container to Import folder ```docker cp {container id):/tmp/{file name}.json ./{directory name}```


<img width="794" alt="image" src="https://github.com/user-attachments/assets/7acd9309-cbe5-40d5-a3ff-95aa51bf21e5">

### Testing

Run the tests. Both test shuold pass.

<img width="471" alt="image" src="https://github.com/user-attachments/assets/b96182b3-23c0-4d5b-806c-a71e8e003eec">























