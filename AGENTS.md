# AGENTS.md

## Project Mission
**TraefikForwardAuth** is a lightweight authentication service designed to provide **Single Sign-On (SSO)** and forward authentication for applications served behind a **Traefik reverse proxy**. It enables centralized session management and secure access control by intercepting and validating requests through Traefik's `forward-auth` middleware.

## Tech Stack & Core Dependencies
- **Languages:** C# (.NET 8.0)
- **Web Framework:** ASP.NET Core MVC (Razor Views)
- **Database:** **MongoDB** (via `MongoDB.EntityFrameworkCore`)
- **Caching:** **Redis** (`StackExchange.Redis`) for distributed session storage.
- **Observability:** **OpenTelemetry** (Traces/Metrics), **Serilog** (Structured Logging).
- **Security:** ASP.NET Core Identity-like custom implementation with **Dynamic Cookie Authentication**.

## Architecture & Design Patterns
- **Multi-Domain Dynamic Auth:** Supports multiple auth domains by dynamically selecting the cookie scheme based on the request host.
- **Service Abstraction:** Business logic is decoupled from controllers via `IAuthService` and `IHostedApplicationService`.
- **Session Externalization:** Auth tickets are stored in a distributed cache (Redis) via a custom `ITicketStore` (`AppTicketStore`).
- **Observability-First:** Extensive instrumentation using OpenTelemetry OTLP exporters and custom `ActivitySources`.
- **Middleware Integration:** Heavily utilizes `X-Forwarded-*` headers to resolve request context from reverse proxies.

## Directory Mental Model
- **`src/Abstractions/`**: Core interfaces (`IAuthService`, etc.) defining the domain boundaries.
- **`src/Auth/`**: Custom authentication handlers, policy schemes, and cookie event logic.
- **`src/Configuration/`**: Strongly-typed `AppOptions` and extension methods for domain/scheme mapping.
- **`src/Controllers/`**: MVC controllers handling Login, Home, and Hosted Application management.
- **`src/Database/`**: EF Core context (`AppDbContext`) and MongoDB models.
- **`src/Helpers/`**: Global exception handling, custom claim types, and Activity/Telemetry utilities.
- **`src/Models/`**: UI-focused ViewModels and API BindingModels.
- **`src/Services/`**: Concrete implementations of domain logic and database interactions.
- **`src/Views/`**: Server-side rendered Razor views.
- **`tests/`**: Integration and unit tests (XUnit).

## Development Standards
- **Naming Conventions:** Standard **PascalCase** for C# classes/methods; **camelCase** for local variables and private fields.
- **Configuration:** Use `AppOptions` via the Options pattern. Environment variables must be prefixed with **`APP_`** (e.g., `APP_AuthCookieDomain`).
- **Error Handling:** Centralized through `GlobalExceptionHandler` (implementing `IExceptionHandler`). Do not use try-catch for flow control in controllers.
- **Logging:** Use `ILogger<T>` and prefer **structured logging** templates (e.g., `LogInformation("User {User} logged in", user)`).

## Hard Constraints & Anti-Patterns
- **No Direct DB in Controllers:** Always go through a Service or the defined Abstractions.
- **No Static Domain Strings:** Always use `AppOptions.GetOrderedAuthDomains()` and related extensions to handle domain logic.
- **Avoid standard `AddCookie`:** Use the `AddDynamicCookieAuth` extension to ensure multi-domain support is preserved.
- **CSRF Protection:** Always use `[ValidateAntiForgeryToken]` on POST actions in `LoginController`.

## Operational Commands
- **Build:** `dotnet build`
- **Run Locally:** `dotnet run --project src/TraefikForwardAuth.csproj`
- **Run Tests:** `dotnet test`
- **Docker Build:** `docker build -t traefik-forward-auth -f build/Dockerfile .`
- **Clean:** `dotnet clean`
