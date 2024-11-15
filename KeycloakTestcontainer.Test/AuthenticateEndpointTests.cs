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
