# syntax=docker/dockerfile:1

# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Ficha Tecnica/Ficha Tecnica.csproj", "Ficha Tecnica/"]
RUN dotnet restore "Ficha Tecnica/Ficha Tecnica.csproj"

COPY . .
RUN dotnet publish "Ficha Tecnica/Ficha Tecnica.csproj" -c Release -o /app/publish

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libfontconfig1 \
        libfreetype6 \
        libharfbuzz0b \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "FichaTecnica.dll"]
