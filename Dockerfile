# Build stage: use .NET 8 SDK to restore and publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and restore
COPY . .
RUN dotnet restore
RUN dotnet publish YachtCRM.Web/YachtCRM.Web.csproj -c Release -o out

# Runtime stage: smaller ASP.NET 8 image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Render will inject PORT and we set ASPNETCORE_URLS via env var in Render.
# Just start the app:
ENTRYPOINT ["dotnet", "YachtCRM.Web.dll"]
