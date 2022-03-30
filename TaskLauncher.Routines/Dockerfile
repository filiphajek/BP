FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TaskLauncher.Routines/TaskLauncher.Routines.csproj", "TaskLauncher.Routines/"]
COPY ["TaskLauncher.App.DAL/TaskLauncher.App.DAL.csproj", "TaskLauncher.App.DAL/"]
COPY ["TaskLauncher.Common/TaskLauncher.Common.csproj", "TaskLauncher.Common/"]
COPY . .
WORKDIR "/src/TaskLauncher.Routines"
RUN dotnet build "TaskLauncher.Routines.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskLauncher.Routines.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskLauncher.Routines.dll"]