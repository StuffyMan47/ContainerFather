FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
# Добавляем настройки прокси и таймаутов для NuGet
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTED SUPPORT=1
WORKDIR /src
COPY ["ContainerFather.API/ContainerFather.API.csproj", "ContainerFather.API/"]
COPY ["ContainerFather.Bot/ContainerFather.Bot.csproj", "ContainerFather.Bot/"]
COPY ["ContainerFather.Core/ContainerFather.Core.csproj", "ContainerFather.Core/"]
COPY ["ContainerFather.Infrastructure/ContainerFather.Infrastructure.csproj", "ContainerFather.Infrastructure/"]
# Добавляем опции для лучшей обработки ошибок при восстановлении пакетов
RUN dotnet restore "ContainerFather.API/ContainerFather.API.csproj" --disable-parallel --no-cache --ignore-failed-sources
COPY . .
WORKDIR "/src/ContainerFather.API"
RUN dotnet build "ContainerFather.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ContainerFather.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
# Определяем UID для пользователя приложения, чтобы избежать проблем с правами
ARG APP_UID=1000
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ContainerFather.API.dll"]