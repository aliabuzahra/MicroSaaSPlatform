FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]

# Copy Billing Service
COPY ["apps/services/billing/SaaS.Billing.Service.csproj", "apps/services/billing/"]

# Restore dependencies
RUN dotnet restore "apps/services/billing/SaaS.Billing.Service.csproj"

# Copy full source
COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/billing/", "apps/services/billing/"]

# Build and Publish
WORKDIR "/src/apps/services/billing"
RUN dotnet publish "SaaS.Billing.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Billing.Service.dll"]
