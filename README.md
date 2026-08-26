# IceCream (ASP.NET Core)

A small ASP.NET Core web application that exposes REST endpoints, serves static client pages, and uses SignalR for real-time notifications. The project includes a background worker that consumes an internal log queue and appends messages to a file to keep request threads responsive.

## Features
- REST API controllers for ice-cream and user management
- SignalR hub for real-time notifications
- Background log worker that writes queued messages to `logs/requests.log`
- Static client app served from `wwwroot`

## Requirements
- .NET 9 SDK (or matching runtime installed)
- Windows, Linux, or macOS (paths and examples use Windows PowerShell where noted)

## Quick start (development)
1. Restore packages and build:
   - PowerShell: `dotnet restore; dotnet build`
2. Run the application:
   - PowerShell: `dotnet run`
3. Open the browser to `http://localhost:5000` (or the URL printed in the console).

## Configuration
- `appsettings.json` and `appsettings.Development.json` contain configuration options (Kestrel, logging, connection strings if added).
- The worker creates the `logs` directory on startup and writes to `logs/requests.log`.

## Project structure (important files)
- `Program.cs` — application bootstrap and service registration
- `Controllers/` — API controllers (e.g., `IceCreamController.cs`, `UsersController.cs`)
- `Hubs/NotificationHub.cs` — SignalR hub for notifications
- `Services/` — background worker and service implementations (`LogWorker.cs`, `LogQueue.cs`, etc.)
- `wwwroot/` — static client files (HTML, CSS, JS)

## Logging
This project uses an in-memory queue (`LogQueue`) to offload synchronous request logging, and a hosted service (`LogWorker`) to persist logs to disk. This design reduces latency for HTTP request handlers and centralizes file I/O.

## Development notes
- The background worker honors the provided cancellation token and will stop gracefully during application shutdown.
- Constructor dependency checks should throw early for missing required services (ArgumentNullException). Consider adding health checks for external dependencies if needed.

## Contributing
Contributions are welcome. Please follow the existing code style and include concise XML documentation for public types and methods.
.
