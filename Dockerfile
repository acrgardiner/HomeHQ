# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

# Install ICU and timezone data for globalization
RUN apk add --no-cache icu-libs tzdata

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HomeHQ.csproj", "."]
RUN dotnet restore "./HomeHQ.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./HomeHQ.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HomeHQ.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
ARG COMMIT_SHA
ENV COMMIT_SHA=$COMMIT_SHA

WORKDIR /app
COPY --from=publish /app/publish ./

# If using non-root user
# USER app

ENTRYPOINT ["dotnet", "HomeHQ.dll"]