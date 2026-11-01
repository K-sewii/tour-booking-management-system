# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f src/DongNamTrip.Web/Dockerfile .
FROM node:22-alpine AS spa
WORKDIR /src/DongNamTrip.Spa
COPY src/DongNamTrip.Spa/package.json src/DongNamTrip.Spa/package-lock.json ./
RUN npm ci
COPY src/DongNamTrip.Spa ./
# vite.config.ts writes to ../DongNamTrip.Web/wwwroot/spa
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/DongNamTrip.Contracts/*.csproj src/DongNamTrip.Contracts/
COPY src/DongNamTrip.Client/*.csproj src/DongNamTrip.Client/
COPY src/DongNamTrip.Web/*.csproj src/DongNamTrip.Web/
RUN dotnet restore src/DongNamTrip.Web/DongNamTrip.Web.csproj
COPY src/DongNamTrip.Contracts src/DongNamTrip.Contracts
COPY src/DongNamTrip.Client src/DongNamTrip.Client
COPY src/DongNamTrip.Web src/DongNamTrip.Web
COPY --from=spa /src/DongNamTrip.Web/wwwroot/spa src/DongNamTrip.Web/wwwroot/spa
RUN dotnet publish src/DongNamTrip.Web/DongNamTrip.Web.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DongNamTrip.Web.dll"]
