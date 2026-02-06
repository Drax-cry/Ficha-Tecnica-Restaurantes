# syntax=docker/dockerfile:1

# Stage 1 – build the ASP.NET Core application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["Ficha Tecnica/Ficha Tecnica.csproj", "Ficha Tecnica/"]
RUN dotnet restore "Ficha Tecnica/Ficha Tecnica.csproj"

# Copy the rest of the source and publish the app
COPY . .
RUN dotnet publish "Ficha Tecnica/Ficha Tecnica.csproj" -c Release -o /app/publish

# Stage 2 – create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install native dependencies required by QuestPDF/SkiaSharp
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libfontconfig1 \
        libfreetype6 \
        libharfbuzz0b \
    && rm -rf /var/lib/apt/lists/*

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Expose the HTTP port used by Cloud Run and start the app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "Ficha Tecnica.dll"]
