FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute AS base
RUN apt-get update \
	&& apt-get install -y --no-install-recommends fonts-liberation \
	&& rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PropertyAdmin.Api/PropertyAdmin.Api.csproj", "PropertyAdmin.Api/"]
COPY ["PropertyAdmin.Contracts/PropertyAdmin.Contracts.csproj", "PropertyAdmin.Contracts/"]
RUN dotnet restore "./PropertyAdmin.Api/PropertyAdmin.Api.csproj"
COPY . .
WORKDIR "/src/PropertyAdmin.Api"
RUN dotnet publish "./PropertyAdmin.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PropertyAdmin.Api.dll"]
