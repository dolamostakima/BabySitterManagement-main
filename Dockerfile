# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and how Visual Studio uses this Dockerfile to build your images.

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ENV DOTNET_ENABLE_AOT=0
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["SmartBabySitter.csproj", "."]
RUN dotnet restore "./SmartBabySitter.csproj"

COPY . .

RUN dotnet build "./SmartBabySitter.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./SmartBabySitter.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SmartBabySitter.dll"]