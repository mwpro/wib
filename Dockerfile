# Stage 1: Build React SPA
FROM node:24-alpine AS frontend-build
WORKDIR /app/frontend
COPY wib-frontend/package.json wib-frontend/package-lock.json ./
RUN npm ci
COPY wib-frontend/ ./
RUN npm run build

# Stage 2: Build and Publish Backend API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/Wib.Api/Wib.Api.csproj backend/Wib.Api/
COPY backend/Wib.UnitTests/Wib.UnitTests.csproj backend/Wib.UnitTests/
RUN dotnet restore backend/Wib.Api/Wib.Api.csproj && \
    dotnet restore backend/Wib.UnitTests/Wib.UnitTests.csproj
COPY backend/Wib.Api/ backend/Wib.Api/
COPY backend/Wib.UnitTests/ backend/Wib.UnitTests/
RUN dotnet test backend/Wib.UnitTests/Wib.UnitTests.csproj -c Release --no-restore
# Copy frontend SPA assets into wwwroot
COPY --from=frontend-build /app/frontend/dist backend/Wib.Api/wwwroot/
RUN dotnet publish backend/Wib.Api/Wib.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime container
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=backend-build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Wib.Api.dll"]
