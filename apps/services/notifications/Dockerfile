FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["libs/SaaS.Shared.Kernel/SaaS.Shared.Kernel.csproj", "libs/SaaS.Shared.Kernel/"]

# Copy Notifications Service
COPY ["apps/services/notifications/SaaS.Notifications.Service.csproj", "apps/services/notifications/"]

# Restore dependencies
RUN dotnet restore "apps/services/notifications/SaaS.Notifications.Service.csproj"

# Copy full source
COPY ["libs/SaaS.Shared.Kernel/", "libs/SaaS.Shared.Kernel/"]
COPY ["apps/services/notifications/", "apps/services/notifications/"]

# Build and Publish
WORKDIR "/src/apps/services/notifications"
RUN dotnet publish "SaaS.Notifications.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SaaS.Notifications.Service.dll"]
