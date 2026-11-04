FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/eShop.Domain/eShop.Domain.csproj", "src/eShop.Domain/"]
COPY ["src/eShop.Application/eShop.Application.csproj", "src/eShop.Application/"]
COPY ["src/eShop.Web/eShop.Web.csproj", "src/eShop.Web/"]

RUN dotnet restore "src/eShop.Web/eShop.Web.csproj"

COPY src/ src/
WORKDIR "/src/src/eShop.Web"
RUN dotnet build "eShop.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "eShop.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "eShop.Web.dll"]

