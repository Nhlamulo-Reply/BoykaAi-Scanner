# Checkpoint 2.2 — MT5 Backend Integration Layer
> Generated after implementation

---

## Checkpoint completed: 2.2 — MT5 backend integration layer

---

## Files created or modified

### Domain (Boyka.Domain)
| Action | File |
|---|---|
| Modified | `Boyka.Domain/Enums/TradeOrderStatus.cs` — added `Queued = 6`, `SentToMT5 = 7` |
| Modified | `Boyka.Domain/Enums/TradeExecutionStatus.cs` — added `PartiallyFilled = 5`, `Unknown = 6` |
| Created  | `Boyka.Domain/Enums/MT5CommandStatus.cs` |
| Created  | `Boyka.Domain/Entities/MT5Command.cs` |
| Created  | `Boyka.Domain/Entities/EaRegistration.cs` |
| Created  | `Boyka.Domain/Entities/SymbolMapping.cs` |
| Modified | `Boyka.Domain/Entities/TradingAccount.cs` — added `AutoTradingEnabled`, `EaRegistrations`, `SymbolMappings`, `MT5Commands` |
| Modified | `Boyka.Domain/Entities/TradeOrder.cs` — added `MT5CommandId` |

### Infrastructure (Boyka.Infrastructure)
| Action | File |
|---|---|
| Modified | `Boyka.Infrastructure/Data/BoykaDbContext.cs` — added 3 DbSets + OnModelCreating config for new entities |
| Created  | `Boyka.Infrastructure/Data/BoykaDbContextFactory.cs` — IDesignTimeDbContextFactory for EF tooling |
| Created  | `Boyka.Infrastructure/Migrations/20261117195652_Phase2_MT5Integration.cs` |
| Created  | `Boyka.Infrastructure/Migrations/20261117195652_Phase2_MT5Integration.Designer.cs` |
| Modified | `Boyka.Infrastructure/Boyka.Infrastructure.csproj` — added `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Configuration.Json` |

### Application (Boyka.Application)
| Action | File |
|---|---|
| Modified | `Boyka.Application/Interfaces/IBoykaDbContext.cs` — added `MT5Commands`, `EaRegistrations`, `SymbolMappings` DbSets |
| Modified | `Boyka.Application/Interfaces/ITradeOrderService.cs` — added `QueueForMT5Async` |
| Created  | `Boyka.Application/Interfaces/IEaRegistrationService.cs` |
| Created  | `Boyka.Application/Interfaces/IMT5CommandService.cs` |
| Created  | `Boyka.Application/DTOs/MT5/ProvisionEaRequest.cs` |
| Created  | `Boyka.Application/DTOs/MT5/ProvisionEaResponse.cs` |
| Created  | `Boyka.Application/DTOs/MT5/MT5HeartbeatRequest.cs` |
| Created  | `Boyka.Application/DTOs/MT5/MT5CommandResponse.cs` |
| Created  | `Boyka.Application/DTOs/MT5/MT5AcknowledgeRequest.cs` |
| Created  | `Boyka.Application/DTOs/MT5/MT5AccountSnapshotRequest.cs` |
| Created  | `Boyka.Application/DTOs/MT5/MT5PositionSnapshotRequest.cs` (includes `MT5PositionItem`) |
| Created  | `Boyka.Application/Services/EaRegistrationService.cs` |
| Created  | `Boyka.Application/Services/MT5CommandService.cs` |
| Modified | `Boyka.Application/Services/TradeOrderService.cs` — added `IMT5CommandService` injection + `QueueForMT5Async` |
| Modified | `Boyka.Application/Boyka.Application.csproj` — added `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Configuration.Binder` |

### API (Boyka.API)
| Action | File |
|---|---|
| Created  | `Boyka.API/Controllers/MT5Controller.cs` |
| Created  | `Boyka.API/Middleware/EaApiKeyMiddleware.cs` |
| Modified | `Boyka.API/Extensions/ServiceCollectionExtensions.cs` — registered `IEaRegistrationService`, `IMT5CommandService`, `EaApiKeyMiddleware` |
| Modified | `Boyka.API/Program.cs` — added `app.UseMiddleware<EaApiKeyMiddleware>()` after ExceptionHandlingMiddleware |
| Modified | `Boyka.API/appsettings.json` — added `MT5` section |
| Modified | `Boyka.API/Middleware/ExceptionHandlingMiddleware.cs` — added `UnauthorizedAccessException` → 403 handler |

### Tests
| Action | File |
|---|---|
| Modified | `Boyka.Tests/TradeOrderServiceTests.cs` — added `FakeMT5CommandService` stub; updated `BuildService` to pass three-arg constructor |

---

## Implementation summary

### Domain
- `TradeOrderStatus` gains `Queued` (order placed on MT5 outbound queue) and `SentToMT5` (reserved for future EA delivery confirmation).
- `TradeExecutionStatus` gains `PartiallyFilled` and `Unknown` to match MT5 `TRADE_RETCODE_DONE_PARTIAL` and unclassified retcodes.
- `MT5CommandStatus` enum (`Pending → Delivered → Acknowledged / Expired / Cancelled`) models the round-trip command lifecycle.
- Three new entities: `MT5Command` (outbound command queue), `EaRegistration` (per-account EA identity), `SymbolMapping` (canonical-to-broker symbol translation, e.g. "XAUUSD" → broker-specific name).
- `TradingAccount` grows `AutoTradingEnabled` and three navigation collections.
- `TradeOrder` grows `MT5CommandId` (nullable correlation field, no FK constraint).

### Infrastructure
- `BoykaDbContext` registers the three new DbSets and configures decimal precision, string lengths, FK behaviors, and the unique index on `SymbolMappings(TradingAccountId, CanonicalSymbol)`.
- `BoykaDbContextFactory` enables `dotnet ef` tooling without a running host.

### Application
- `EaRegistrationService`: generates a 32-byte random hex raw key, hashes it with SHA-256, stores only the hash. The raw key is returned once in `ProvisionEaResponse` and never logged or persisted in plaintext. Revoke sets `IsActive = false` and `RevokedAt`. Validate hashes the incoming key and matches against active registrations.
- `MT5CommandService`: queues commands with configurable expiry (`MT5:CommandExpirySeconds`, default 60 s). `GetNextPendingCommandAsync` returns the oldest non-expired pending command and marks it `Delivered`. `AcknowledgeCommandAsync` maps EA status strings to `TradeExecutionStatus` / `TradeOrderStatus`, persists a `TradeExecution`, and marks the command `Acknowledged`. `ExpireStaleCommandsAsync` sweeps `Pending`/`Delivered` commands past their `ExpiresAt` and sets the linked order to `Rejected`.
- `TradeOrderService.QueueForMT5Async`: runs the same validation logic as `ExecuteTradeAsync`, creates the order with `Status = Queued`, resolves the broker symbol via `SymbolMappings` (falls back to canonical if no mapping exists), calls `MT5CommandService.QueueCommandAsync`, then stores the correlation `MT5CommandId` on the order. `ExecuteTradeAsync` is completely unchanged.

### API
- `EaApiKeyMiddleware` implements `IMiddleware` (injected as a scoped service). It applies only to paths starting with `/api/mt5/` and skips `POST /api/mt5/registrations`. Valid registrations are stored in `HttpContext.Items["EaRegistration"]`.
- `MT5Controller` exposes six endpoints. All EA-authenticated endpoints read `TradingAccountId` exclusively from the validated `EaRegistration` in `HttpContext.Items`, never from the request body.
- `ExceptionHandlingMiddleware` now catches `UnauthorizedAccessException` → HTTP 403.

---

## Migration name generated

`20261117195652_Phase2_MT5Integration`

Tables added: `MT5Commands`, `EaRegistrations`, `SymbolMappings`  
Columns added: `TradingAccounts.AutoTradingEnabled` (bit, default false), `TradeOrders.MT5CommandId` (uniqueidentifier, nullable)

---

## Security note: raw API key handling

| Location | Raw key present? |
|---|---|
| `ProvisionEaResponse.RawApiKey` returned to caller | **Yes — once only** |
| `EaRegistration.ApiKeyHash` stored in DB | No — SHA-256 hex hash only |
| `EaRegistrationService` logs | No |
| `X-EA-Api-Key` header validated in middleware | Hashed immediately; raw value not stored |
| Any other response or log | No |

The plaintext key is generated in memory, returned in the provisioning response, and discarded. All subsequent operations compare against the stored hash.

---

## Build result

```
Build succeeded.
0 Error(s)
12 Warning(s) [all NU190x package vulnerability advisories — pre-existing, not introduced by this checkpoint]
```

---

## Test result

```
Test run for Boyka.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 1 s
```

All 13 existing tests pass.

---

## Outstanding issues

- `BoykaDbContextFactory` uses `Directory.GetCurrentDirectory()` to find the API project's `appsettings.json`. This assumes `dotnet ef` is run from the `backend/` directory (as specified). If run from elsewhere, the path will be wrong.
- The `SentToMT5 = 7` enum value is reserved in `TradeOrderStatus` but no service sets it yet. It is available for a future background job or EA delivery confirmation step.
- `MT5Controller.AccountSnapshot` and `MT5Controller.PositionSnapshot` log but do not persist. Persistence can be added in a later checkpoint.
- Vulnerability warnings for `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` are pre-existing transitive dependencies, not introduced by this checkpoint.

---

## Next checkpoint: 2.3 — MQL5 Expert Advisor

STOPPED — waiting for your approval before starting Checkpoint 2.3.
