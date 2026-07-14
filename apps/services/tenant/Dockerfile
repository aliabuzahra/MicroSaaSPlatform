FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/tenant/SaaS.Tenant.Service.csproj", "apps/services/tenant/"]

RUN dotnet restore "apps/services/tenant/SaaS.Tenant.Service.csproj"

COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/tenant/", "apps/services/tenant/"]

WORKDIR "/src/apps/services/tenant"
RUN dotnet publish "SaaS.Tenant.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Tenant.Service.dll"]
