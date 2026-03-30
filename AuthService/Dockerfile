# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopi�r csproj (og evt. flere hvis solution)
COPY AuthService.csproj ./

# Restore dependencies
RUN dotnet restore

# Kopi�r resten af projektet
COPY . .

# Publish til /app/publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AuthService.dll"]
