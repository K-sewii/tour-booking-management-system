# Running directly on Windows

Use this as an alternative to running API/Web in Docker. Do not start both on the same host ports.

From the repository root:

```powershell
dotnet restore .\DongNamTrip.sln
dotnet build .\DongNamTrip.sln --no-restore
```

Build the SPA while preserving the current directory:

```powershell
Push-Location .\src\Frontend\DongNamTrip.Spa
try {
    npm ci
    npm run build
}
finally {
    Pop-Location
}
```

The frontend output is written to:

`src/Frontend/DongNamTrip.Web/wwwroot/spa`

Configure the API connection string for your SQL instance. This example assumes a local `SQLEXPRESS` instance and an existing database:

```powershell
dotnet user-secrets set "ConnectionStrings:DongNamTrip" "Server=.\SQLEXPRESS;Database=DongNamTrip;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" --project .\src\Backend\DongNamTrip.Api
```

Start each application in a separate terminal from the repository root.

API:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\Backend\DongNamTrip.Api --launch-profile http
```

Web:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\Frontend\DongNamTrip.Web --launch-profile http
```

WinForms:

```powershell
dotnet run --project .\src\Frontend\DongNamTrip.Admin
```

For frontend hot reload, keep API and Web running, then use another terminal:

```powershell
Set-Location .\src\Frontend\DongNamTrip.Spa
npm run dev
```

Open `http://127.0.0.1:5173`.

Legacy MVC remains available through `?legacy=1` or `Spa__Enabled=false`.
