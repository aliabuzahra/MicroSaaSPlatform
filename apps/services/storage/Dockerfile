FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/storage/SaaS.Storage.Service.csproj", "apps/services/storage/"]

RUN dotnet restore "apps/services/storage/SaaS.Storage.Service.csproj"

COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/storage/", "apps/services/storage/"]

WORKDIR "/src/apps/services/storage"
RUN dotnet publish "SaaS.Storage.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Storage.Service.dll"]
