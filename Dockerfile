# ==========================================
# STAGE 1: Build the React Frontend
# ==========================================
# Uses a lightweight Node.js image to install dependencies and bundle the React SPA.
FROM node:22-alpine AS frontend-build
WORKDIR /app/web

# Copy package manifests first to leverage Docker's layer caching for faster builds
COPY web/package*.json ./
RUN npm install

# Copy the rest of the React source code and compile the production bundle
COPY web/ ./
RUN npm run build


# ==========================================
# STAGE 2: Build the .NET Monolith Backend
# ==========================================
# Uses the full .NET Software Development Kit (SDK) to compile C# code projects.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# Copy solution and project files for dependency restoration across the monorepo
COPY SupplySmart.slnx ./
COPY SupplySmart.Host/SupplySmart.Host.csproj SupplySmart.Host/
COPY SupplySmart.Domain/SupplySmart.Domain.csproj SupplySmart.Domain/
COPY SupplySmart.Integrations/SupplySmart.Integrations.csproj SupplySmart.Integrations/
COPY SupplySmart.Endpoints/SupplySmart.Endpoints.csproj SupplySmart.Endpoints/

# Restore NuGet dependencies for the host project graph
RUN dotnet restore SupplySmart.Host/SupplySmart.Host.csproj

# Copy all remaining backend source code across the monorepo projects
COPY . .

# Publish the compiled backend application binaries into an output folder
WORKDIR /src/SupplySmart.Host
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false


# ==========================================
# STAGE 3: Final Production Runtime Image
# ==========================================
# Uses a lightweight ASP.NET Core runtime image (no heavy SDK tools) for production execution.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

# Copy compiled backend binaries from Stage 2
COPY --from=backend-build /app/publish .

# Copy built React frontend static assets from Stage 1 directly into the backend's wwwroot folder
# This creates a unified monolith container where C# serves both the API and the React SPA.
COPY --from=frontend-build /app/SupplySmart.Host/wwwroot ./wwwroot

# Set the container's entry point executable to run the C# Web Host
ENTRYPOINT ["dotnet", "SupplySmart.Host.dll"]