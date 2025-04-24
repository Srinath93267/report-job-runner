# Base image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file
COPY ["ReportJobRunner.csproj", "./"]
RUN dotnet restore "./ReportJobRunner.csproj"

# Copy all source files
COPY . .

# Build the app
RUN dotnet build "ReportJobRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ReportJobRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReportJobRunner.dll"]
