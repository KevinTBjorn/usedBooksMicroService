FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# set NuGet package folders for Linux build container
ENV NUGET_PACKAGES=/root/.nuget/packages
ENV NUGET_FALLBACK_PACKAGES=/root/.nuget/fallbackpackages

# create nuget folders
RUN mkdir -p $NUGET_PACKAGES \
    && mkdir -p $NUGET_FALLBACK_PACKAGES

# copy project files needed for restore
COPY WarehouseService/*.csproj ./WarehouseService/
COPY Domain/*.csproj ./Domain/

# restore
RUN dotnet restore WarehouseService/WarehouseService.csproj

# copy everything
COPY . .

# publish (do not use --no-restore so publish can use restored packages)
RUN dotnet publish WarehouseService/WarehouseService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "WarehouseService.dll"]
