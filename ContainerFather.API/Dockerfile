FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src
COPY ["ContainerFather.API/ContainerFather.API.csproj", "ContainerFather.API/"]
COPY ["ContainerFather.Bot/ContainerFather.Bot.csproj", "ContainerFather.Bot/"]
COPY ["ContainerFather.Core/ContainerFather.Core.csproj", "ContainerFather.Core/"]
COPY ["ContainerFather.Infrastructure/ContainerFather.Infrastructure.csproj", "ContainerFather.Infrastructure/"]
RUN dotnet restore "ContainerFather.API/ContainerFather.API.csproj"
COPY . .
WORKDIR "/src/ContainerFather.API"
RUN dotnet build --no-restore "ContainerFather.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish --no-restore "ContainerFather.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# ==========================================
# НАЧАЛО: Установка доверенных сертификатов
# ==========================================
USER root

# Копируем все 5 сертификатов в системную папку доверенных сертификатов
COPY ContainerFather.API/Certs/russian_trusted_root_ca_gost_2025_pem.crt /usr/local/share/ca-certificates/russian_trusted_root_ca_gost_2025_pem.crt
COPY ContainerFather.API/Certs/russian_trusted_root_ca_pem.crt /usr/local/share/ca-certificates/russian_trusted_root_ca_pem.crt
COPY ContainerFather.API/Certs/russian_trusted_sub_ca_2024_pem.crt /usr/local/share/ca-certificates/russian_trusted_sub_ca_2024_pem.crt
COPY ContainerFather.API/Certs/russian_trusted_sub_ca_gost_2025_pem.crt /usr/local/share/ca-certificates/russian_trusted_sub_ca_gost_2025_pem.crt
COPY ContainerFather.API/Certs/russian_trusted_sub_ca_pem.crt /usr/local/share/ca-certificates/russian_trusted_sub_ca_pem.crt

# Обновляем хранилище сертификатов (создаст хеши в /etc/ssl/certs/)
RUN update-ca-certificates

# Возвращаем безопасного пользователя для запуска приложения
USER $APP_UID
# ==========================================
# КОНЕЦ: Установка доверенных сертификатов
# ==========================================

COPY --from=publish /app/publish .
COPY ContainerFather.API/Files ./Files/

ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1

# RUN apt-get update && apt-get install -y \
#     package1 \
#     package2 \
#     && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ContainerFather.API.dll"]