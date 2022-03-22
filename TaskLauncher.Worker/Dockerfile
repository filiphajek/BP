FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TaskLauncher.ContainerLauncher/TaskLauncher.ContainerLauncher.csproj", "TaskLauncher.ContainerLauncher/"]
COPY ["TaskLauncher.Api.Contracts/TaskLauncher.Api.Contracts.csproj", "TaskLauncher.Api.Contracts/"]
COPY ["TaskLauncher.Common/TaskLauncher.Common.csproj", "TaskLauncher.Common/"]
RUN dotnet restore "TaskLauncher.ContainerLauncher/TaskLauncher.ContainerLauncher.csproj"
COPY . .
WORKDIR "/src/TaskLauncher.ContainerLauncher"
RUN dotnet build "TaskLauncher.ContainerLauncher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskLauncher.ContainerLauncher.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskLauncher.ContainerLauncher.dll"]