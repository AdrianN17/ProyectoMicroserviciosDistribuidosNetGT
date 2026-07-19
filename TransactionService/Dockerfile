# Etapa 1: Construcción (build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar la solución y proyectos para restaurar
COPY ["*.sln", "./"]
COPY ["TransactionService.Api/TransactionService.Api.csproj", "TransactionService.Api/"]
COPY ["TransactionService.Application/TransactionService.Application.csproj", "TransactionService.Application/"]
COPY ["TransactionService.Domain/TransactionService.Domain.csproj", "TransactionService.Domain/"]
COPY ["TransactionService.Infrastructure/TransactionService.Infrastructure.csproj", "TransactionService.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "TransactionService.Api/TransactionService.Api.csproj"

# Copiar el resto del código
COPY . .

# Compilar
WORKDIR "/src/TransactionService.Api"
RUN dotnet build "TransactionService.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

# Publicar la aplicación
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TransactionService.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false


# Etapa 2: final - runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
# Puerto expuesto (ajústalo si usas otro)
EXPOSE 8080

ENTRYPOINT ["dotnet", "TransactionService.Api.dll"]
