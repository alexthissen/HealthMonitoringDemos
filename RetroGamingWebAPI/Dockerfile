FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk AS build
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