FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/apikeys/SaaS.ApiKeys.Service.csproj", "apps/services/apikeys/"]

RUN dotnet restore "apps/services/apikeys/SaaS.ApiKeys.Service.csproj"

COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/apikeys/", "apps/services/apikeys/"]

WORKDIR "/src/apps/services/apikeys"
RUN dotnet publish "SaaS.ApiKeys.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.ApiKeys.Service.dll"]
