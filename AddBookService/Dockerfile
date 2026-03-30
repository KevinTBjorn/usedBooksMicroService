# BUILD STAGE
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# set NuGet package folders for Linux build container
ENV NUGET_PACKAGES=/root/.nuget/packages
ENV NUGET_FALLBACK_PACKAGES=/root/.nuget/fallbackpackages

# create nuget folders
RUN mkdir -p $NUGET_PACKAGES \
    && mkdir -p $NUGET_FALLBACK_PACKAGES

# Copy csproj files
COPY AddBookService/AddBookService.csproj AddBookService/
COPY Domain/Domain.csproj Domain/

# Clean any obj folders that might have been copied
RUN find /src -type d -name obj -exec rm -rf {} + || true
RUN find /src -type d -name bin -exec rm -rf {} + || true

# Restore with correct NuGet config
RUN dotnet restore AddBookService/AddBookService.csproj

# Copy full source
COPY AddBookService/ AddBookService/
COPY Domain/ Domain/

WORKDIR /src/AddBookService
RUN dotnet publish -c Release -o /app --no-self-contained


# RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AddBookService.dll"]
