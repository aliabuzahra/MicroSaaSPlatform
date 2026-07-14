FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/features/SaaS.Features.Service.csproj", "apps/services/features/"]

RUN dotnet restore "apps/services/features/SaaS.Features.Service.csproj"

COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/features/", "apps/services/features/"]

WORKDIR "/src/apps/services/features"
RUN dotnet publish "SaaS.Features.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Features.Service.dll"]
