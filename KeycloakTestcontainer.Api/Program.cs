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

app.MapGet("api/authenticate", () =>
    Results.Ok($"{System.Net.HttpStatusCode.OK} authenticated"))
    .RequireAuthorization();

app.Run();