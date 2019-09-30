FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["RetroGamingWebAPI/RetroGamingWebAPI.csproj", "RetroGamingWebAPI/"]
RUN dotnet restore "RetroGamingWebAPI/RetroGamingWebAPI.csproj"
COPY . .
WORKDIR "/src/RetroGamingWebAPI"
RUN dotnet build "RetroGamingWebAPI.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RetroGamingWebAPI.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RetroGamingWebAPI.dll"]
