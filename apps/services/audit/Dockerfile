FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]

# Copy Audit Service
COPY ["apps/services/audit/SaaS.Audit.Service.csproj", "apps/services/audit/"]

# Restore dependencies
RUN dotnet restore "apps/services/audit/SaaS.Audit.Service.csproj"

# Copy full source
COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/audit/", "apps/services/audit/"]

# Build and Publish
WORKDIR "/src/apps/services/audit"
RUN dotnet publish "SaaS.Audit.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Audit.Service.dll"]
