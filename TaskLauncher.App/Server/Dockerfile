FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TaskLauncher.App/Server/TaskLauncher.App.Server.csproj", "TaskLauncher.App/Server/"]
COPY ["TaskLauncher.App.DAL/TaskLauncher.App.DAL.csproj", "TaskLauncher.App.DAL/"]
COPY ["TaskLauncher.Common/TaskLauncher.Common.csproj", "TaskLauncher.Common/"]
COPY ["TaskLauncher.App/Client/TaskLauncher.App.Client.csproj", "TaskLauncher.App/Client/"]
COPY ["TaskLauncher.Api.Contracts/TaskLauncher.Api.Contracts.csproj", "TaskLauncher.Api.Contracts/"]
COPY ["TaskLauncher.Authorization/TaskLauncher.Authorization.csproj", "TaskLauncher.Authorization/"]
COPY . .
WORKDIR "/src/TaskLauncher.App/Server"
RUN dotnet build "TaskLauncher.App.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskLauncher.App.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskLauncher.App.Server.dll"]