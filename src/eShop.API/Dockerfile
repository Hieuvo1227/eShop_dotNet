FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/eShop.Domain/eShop.Domain.csproj", "src/eShop.Domain/"]
COPY ["src/eShop.Application/eShop.Application.csproj", "src/eShop.Application/"]
COPY ["src/eShop.Infrastructure/eShop.Infrastructure.csproj", "src/eShop.Infrastructure/"]
COPY ["src/eShop.API/eShop.API.csproj", "src/eShop.API/"]

RUN dotnet restore "src/eShop.API/eShop.API.csproj"

COPY src/ src/
WORKDIR "/src/src/eShop.API"
RUN dotnet build "eShop.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "eShop.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "eShop.API.dll"]

