FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TaskLauncher.WebApp/Server/TaskLauncher.WebApp.Server.csproj", "TaskLauncher.WebApp/Server/"]
COPY ["TaskLauncher.WebApp/Client/TaskLauncher.WebApp.Client.csproj", "TaskLauncher.WebApp/Client/"]
COPY ["TaskLauncher.Common/TaskLauncher.Common.csproj", "TaskLauncher.Common/"]
COPY ["TaskLauncher.Api.Contracts/TaskLauncher.Api.Contracts.csproj", "TaskLauncher.Api.Contracts/"]
RUN dotnet restore "TaskLauncher.WebApp/Server/TaskLauncher.WebApp.Server.csproj"
COPY . .
WORKDIR "/src/TaskLauncher.WebApp/Server"
RUN dotnet build "TaskLauncher.WebApp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskLauncher.WebApp.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskLauncher.WebApp.Server.dll"]