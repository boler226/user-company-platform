FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore UserService.API/UserService.API.csproj
RUN dotnet restore CompanyService.API/CompanyService.API.csproj
RUN dotnet restore NotificationService.Worker/NotificationService.Worker.csproj
RUN dotnet restore DatabaseSetup/DatabaseSetup.csproj
RUN dotnet restore NotificationService.API/NotificationService.API.csproj
RUN dotnet publish UserService.API/UserService.API.csproj -c Release --no-restore -o /out/users
RUN dotnet publish CompanyService.API/CompanyService.API.csproj -c Release --no-restore -o /out/companies
RUN dotnet publish NotificationService.Worker/NotificationService.Worker.csproj -c Release --no-restore -o /out/notifications
RUN dotnet publish DatabaseSetup/DatabaseSetup.csproj -c Release --no-restore -o /out/setup
RUN dotnet publish NotificationService.API/NotificationService.API.csproj -c Release --no-restore -o /out/notifications-api

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS users-service
WORKDIR /app
COPY --from=build /out/users .
ENTRYPOINT ["dotnet", "UserService.API.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS companies-service
WORKDIR /app
COPY --from=build /out/companies .
ENTRYPOINT ["dotnet", "CompanyService.API.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS notifications-worker
WORKDIR /app
COPY --from=build /out/notifications .
ENTRYPOINT ["dotnet", "NotificationService.Worker.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS notifications-api
WORKDIR /app
COPY --from=build /out/notifications-api .
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS database-setup
WORKDIR /app
COPY --from=build /out/setup .
ENTRYPOINT ["dotnet", "DatabaseSetup.dll"]
