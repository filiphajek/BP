FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TaskLauncher.Api/TaskLauncher.Api.csproj", "TaskLauncher.Api/"]
COPY ["TaskLauncher.Api.Contracts/TaskLauncher.Api.Contracts.csproj", "TaskLauncher.Api.Contracts/"]
COPY ["TaskLauncher.Common/TaskLauncher.Common.csproj", "TaskLauncher.Common/"]
COPY ["TaskLauncher.Api.DAL/TaskLauncher.Api.DAL.csproj", "TaskLauncher.Api.DAL/"]
RUN dotnet restore "TaskLauncher.Api/TaskLauncher.Api.csproj"
COPY . .
WORKDIR "/src/TaskLauncher.Api"
RUN dotnet build "TaskLauncher.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskLauncher.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskLauncher.Api.dll"]