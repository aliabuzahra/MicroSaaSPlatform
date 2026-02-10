FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]

# Copy Identity Service
COPY ["apps/services/identity/SaaS.Identity.Service.csproj", "apps/services/identity/"]

# Restore dependencies
RUN dotnet restore "apps/services/identity/SaaS.Identity.Service.csproj"

# Copy full source
COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/identity/", "apps/services/identity/"]

# Build and Publish
WORKDIR "/src/apps/services/identity"
RUN dotnet publish "SaaS.Identity.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Identity.Service.dll"]
