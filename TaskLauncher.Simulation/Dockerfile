FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TaskLauncher.Simulation/TaskLauncher.Simulation.csproj", "TaskLauncher.Simulation/"]
COPY ["TaskLauncher.Api.Contracts/TaskLauncher.Api.Contracts.csproj", "TaskLauncher.Api.Contracts/"]
COPY ["TaskLauncher.Common/TaskLauncher.Common.csproj", "TaskLauncher.Common/"]
COPY ["TaskLauncher.Authorization/TaskLauncher.Authorization.csproj", "TaskLauncher.Authorization/"]
COPY . .
WORKDIR "/src/TaskLauncher.Simulation"
RUN dotnet build "TaskLauncher.Simulation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskLauncher.Simulation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskLauncher.Simulation.dll"]