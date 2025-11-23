FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src
COPY ["ContainerFather/ContainerFather.API/ContainerFather.API.csproj", "ContainerFather.API/"]
COPY ["ContainerFather/ContainerFather.Bot/ContainerFather.Bot.csproj", "ContainerFather.Bot/"]
COPY ["ContainerFather/ContainerFather.Core/ContainerFather.Core.csproj", "ContainerFather.Core/"]
COPY ["ContainerFather/ContainerFather.Infrastructure/ContainerFather.Infrastructure.csproj", "ContainerFather.Infrastructure/"]
RUN dotnet restore "ContainerFather.API/ContainerFather.API.csproj"
COPY . .
WORKDIR "/src/ContainerFather.API"
RUN dotnet build "ContainerFather.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ContainerFather.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update
    
ENTRYPOINT ["dotnet", "ContainerFather.API.dll"]
