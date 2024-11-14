# Asp.Net Core and Keycloak testcontainer 
Testing a secure Asp.Net Core Api using Keycloak Testcontainer

## Keycloak container setup

Requirements: docker installed.

Pull the latest docker image
```powershell
docker pull keycloak/keycloak:26.0
```

## Solution and Projects setup
Create a new solution
```powershell
dotnet new sln -n KeycloakTestcontainer
```
Create and add a MinimalApi project to the solution
```powershell
dotnet new webapi -n KeycloakTestcontainer.Api
dotnet sln add ./KeycloakTestcontainer.Api
```
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
Add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11 to KeycloakTestcontainer.Test. It will spin up an in memory web api for testing.
```powershell
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11
```
Add IApiMarker interface to the root of KeycloakTestcontainer.Api project.

It will be used as entry point of the ```WebApplicationFactory<IApiMarker>```
 
<img width="250" alt="iapimarker" src="https://github.com/user-attachments/assets/f81e22f5-07af-42ff-a9a8-2ee04629f416">

Add ApiFactoryFixture class to KeycloakTestcontainer.Test project

<img width="250" alt="image" src="https://github.com/user-attachments/assets/106d1875-22bd-4b58-9495-0c5118b58ac0">



