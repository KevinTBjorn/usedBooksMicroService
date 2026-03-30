# ============================
# 1. Base runtime image
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080


# ============================
# 2. Build stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files for BOTH projects (OrderService + Domain)
COPY ./OrderService/OrderService.csproj ./OrderService/
COPY ./Domain/Domain.csproj ./Domain/

# Restore
RUN dotnet restore ./OrderService/OrderService.csproj

# Copy ALL source code (solution root)
COPY . .

# Build & publish
RUN dotnet publish ./OrderService/OrderService.csproj -c Release -o /app/publish


# ============================
# 3. Final runtime image
# ============================
FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OrderService.dll"]
