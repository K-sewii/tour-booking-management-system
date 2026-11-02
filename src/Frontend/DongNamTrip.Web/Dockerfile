# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f src/Frontend/DongNamTrip.Web/Dockerfile .
FROM node:26-alpine AS spa
WORKDIR /src/Frontend/DongNamTrip.Spa
COPY src/Frontend/DongNamTrip.Spa/package.json src/Frontend/DongNamTrip.Spa/package-lock.json ./
RUN npm ci
COPY src/Frontend/DongNamTrip.Spa ./
# main.tsx imports the shared CSS from the Web project
COPY src/Frontend/DongNamTrip.Web/wwwroot/css /src/Frontend/DongNamTrip.Web/wwwroot/css
# vite.config.ts writes to ../DongNamTrip.Web/wwwroot/spa
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Shared/DongNamTrip.Contracts/*.csproj src/Shared/DongNamTrip.Contracts/
COPY src/Frontend/DongNamTrip.Client/*.csproj src/Frontend/DongNamTrip.Client/
COPY src/Frontend/DongNamTrip.Web/*.csproj src/Frontend/DongNamTrip.Web/
RUN dotnet restore src/Frontend/DongNamTrip.Web/DongNamTrip.Web.csproj
COPY src/Shared/DongNamTrip.Contracts src/Shared/DongNamTrip.Contracts
COPY src/Frontend/DongNamTrip.Client src/Frontend/DongNamTrip.Client
COPY src/Frontend/DongNamTrip.Web src/Frontend/DongNamTrip.Web
COPY --from=spa /src/Frontend/DongNamTrip.Web/wwwroot/spa src/Frontend/DongNamTrip.Web/wwwroot/spa
RUN dotnet publish src/Frontend/DongNamTrip.Web/DongNamTrip.Web.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DongNamTrip.Web.dll"]
