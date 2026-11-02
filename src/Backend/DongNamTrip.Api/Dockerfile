# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f src/Backend/DongNamTrip.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Shared/DongNamTrip.Contracts/*.csproj src/Shared/DongNamTrip.Contracts/
COPY src/Backend/DongNamTrip.Modules/*.csproj src/Backend/DongNamTrip.Modules/
COPY src/Backend/DongNamTrip.Api/*.csproj src/Backend/DongNamTrip.Api/
RUN dotnet restore src/Backend/DongNamTrip.Api/DongNamTrip.Api.csproj
COPY src/Shared/DongNamTrip.Contracts src/Shared/DongNamTrip.Contracts
COPY src/Backend/DongNamTrip.Modules src/Backend/DongNamTrip.Modules
COPY src/Backend/DongNamTrip.Api src/Backend/DongNamTrip.Api
RUN dotnet publish src/Backend/DongNamTrip.Api/DongNamTrip.Api.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DongNamTrip.Api.dll"]
