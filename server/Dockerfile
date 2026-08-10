# Story 17.1 (T17.1.1). Build context is server/ (see docker-compose.yml at the repo root) so
# the multi-project reference chain (Api -> Application/Infrastructure -> Domain, one-way per
# ARCHITECTURE.md) resolves without reaching outside this folder.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, copying only the .csproj files each project needs — Docker's layer cache then
# skips the restore step entirely on a source-only change, which is the common case during
# iteration. Test projects are deliberately not copied into the image at all: dotnet restore on
# the Api project transitively restores Domain/Application/Infrastructure through their
# ProjectReferences, and this image never runs tests (CI already does that, see
# .github/workflows/ci.yml).
COPY Directory.Build.props .
COPY src/CrowdCoffee.Domain/CrowdCoffee.Domain.csproj src/CrowdCoffee.Domain/
COPY src/CrowdCoffee.Application/CrowdCoffee.Application.csproj src/CrowdCoffee.Application/
COPY src/CrowdCoffee.Infrastructure/CrowdCoffee.Infrastructure.csproj src/CrowdCoffee.Infrastructure/
COPY src/CrowdCoffee.Api/CrowdCoffee.Api.csproj src/CrowdCoffee.Api/
RUN dotnet restore src/CrowdCoffee.Api/CrowdCoffee.Api.csproj

COPY src/ src/
RUN dotnet publish src/CrowdCoffee.Api/CrowdCoffee.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Story 13.1's note in Program.cs: wwwroot/uploads/products must exist before
# WebApplication.CreateBuilder runs or IWebHostEnvironment.WebRootFileProvider binds to a null
# provider for the process's whole lifetime. Program.cs already creates the folder as its literal
# first line, so nothing extra is required here — this just documents why an empty wwwroot isn't
# a problem at container startup, unlike it would be if that guard were ever removed.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CrowdCoffee.Api.dll"]
