# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f src/DongNamTrip.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/DongNamTrip.Contracts/*.csproj src/DongNamTrip.Contracts/
COPY src/DongNamTrip.Api/*.csproj src/DongNamTrip.Api/
RUN dotnet restore src/DongNamTrip.Api/DongNamTrip.Api.csproj
COPY src/DongNamTrip.Contracts src/DongNamTrip.Contracts
COPY src/DongNamTrip.Api src/DongNamTrip.Api
RUN dotnet publish src/DongNamTrip.Api/DongNamTrip.Api.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DongNamTrip.Api.dll"]
