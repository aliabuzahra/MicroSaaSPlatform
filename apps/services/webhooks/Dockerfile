FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/webhooks/SaaS.Webhooks.Service.csproj", "apps/services/webhooks/"]

RUN dotnet restore "apps/services/webhooks/SaaS.Webhooks.Service.csproj"

COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/webhooks/", "apps/services/webhooks/"]

WORKDIR "/src/apps/services/webhooks"
RUN dotnet publish "SaaS.Webhooks.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Webhooks.Service.dll"]
