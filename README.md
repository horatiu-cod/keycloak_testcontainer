# Asp.Net Core and Keycloak testcontainer 
Testing a secure Asp.Net Core Api using Keycloak Testcontainer

Create a new solution
```powershell
dotnet new sln -n KeycloakTestcontainer
```
Create and add a MinimalApi project to the solution
```powershell
dotnet new webapi -n KeycloakTestcontainer.Api
```
```powershell
dotnet sln add ./KeycloakTestcontainer.Api
```
Create and add a xUnit test project to the solution
```powershell
dotnet new xunit -n KeycloakTestcontainer.Test
```
```powershell
dotnet sln add ./KeycloakTestcontainer.Test
```
Add reference to KeycloakTestcontainer.Api project
```powershell
cd KeycloakTestcontainer.Test
```
```powershell
dotnet add reference ../KeycloakTestcontainer.Api
```
