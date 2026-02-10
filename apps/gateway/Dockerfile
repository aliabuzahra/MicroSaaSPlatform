FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]

# Copy Gateway
COPY ["apps/gateway/SaaS.Gateway.csproj", "apps/gateway/"]

# Restore dependencies
RUN dotnet restore "apps/gateway/SaaS.Gateway.csproj"

# Copy full source
COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/gateway/", "apps/gateway/"]

# Build and Publish
WORKDIR "/src/apps/gateway"
RUN dotnet publish "SaaS.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Gateway.dll"]
