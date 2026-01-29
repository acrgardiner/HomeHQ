# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files for restore
COPY ["HomeHQ.Server/HomeHQ.Server.csproj", "HomeHQ.Server/"]

# If you have a solution file, copy it here as well:
# COPY ["HomeHQ.sln", "."]

# Restore dependencies
WORKDIR /src/HomeHQ.Server
RUN dotnet restore "HomeHQ.Server.csproj"

# Copy the rest of the source code
WORKDIR /src
COPY . .

# Build
WORKDIR /src/HomeHQ.Server
RUN dotnet build "HomeHQ.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/HomeHQ.Server
RUN dotnet publish "HomeHQ.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
ARG COMMIT_SHA
ENV COMMIT_SHA=$COMMIT_SHA

WORKDIR /app
COPY --from=publish /app/publish ./

ENTRYPOINT ["dotnet", "HomeHQ.Server.dll"]