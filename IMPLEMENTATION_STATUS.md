# Boyka AI — Implementation Status

**Last updated:** 18 November 2026  
**Scope:** Current repository implementation, not future design intent.

## 1. Summary

Boyka AI currently contains a .NET 10 backend with SQL Server persistence,
backend-side trade/risk validation, mock execution, and a demo-only MT5 Expert
Advisor (EA) integration path. The EA polls the backend for queued commands,
submits them through an MT5 demo terminal, and reports results to the backend.

The mobile project exists as a default .NET MAUI shell only; it is not yet
connected to the backend. No AI strategy, live-money trading, or production
deployment has been implemented.

## 2. Solution structure

| Project / folder | Current responsibility |
| --- | --- |
| `backend/Boyka.Domain` | Domain entities and trading/MT5 status enums. |
| `backend/Boyka.Application` | DTOs, service interfaces, validation, order, risk, EA registration, and MT5 command services. |
| `backend/Boyka.Infrastructure` | EF Core SQL Server DbContext, migrations, and mock trade execution. |
| `backend/Boyka.API` | ASP.NET Core API controllers, middleware, dependency registration, Swagger in Development. |
| `backend/Boyka.Tests` | Unit tests for trade validation, mock execution, MT5 command lifecycle, and account-snapshot validation. |
| `ea/BoykaEA.mq5` | Demo-only MQL5 EA source. |
| `Boyka.Mobile` | MAUI starter application; currently the template counter screen. |

## 3. Backend foundation

### Domain and database

The backend models and persists:

- Trading accounts, including demo/live metadata and an `AutoTradingEnabled`
  field.
- Trade signals, orders, executions, and risk profiles.
- MT5 commands and their pending/delivered/acknowledged/expired lifecycle.
- EA registrations, API-key hashes, heartbeat time, and revocation state.
- Account-specific canonical-to-broker symbol mappings.

EF Core migrations create the schema and seed a demo account with ID:

```text
00000000-0000-0000-0000-000000000001
```

The default local configuration uses SQL Server LocalDB. The Phase 2 migration
adds the EA registrations, MT5 commands, symbol mappings, and order correlation
fields.

### Trading and risk behavior

- The standard trade path validates account activity, direction, positive
  volume/prices, directional stop-loss/take-profit rules, maximum lot size,
  and maximum open positions.
- `POST /api/trades/execute` uses the existing mock execution service. It does
  not contact MT5 or a broker.
- `POST /api/trades/queue-mt5` performs the same validation, creates a queued
  order, resolves an optional broker-symbol mapping, and creates an MT5 command
  for EA polling.
- The backend records broker results received from the EA as `TradeExecution`
  records and updates the linked trade order status.

### Available API routes

| Area | Implemented routes |
| --- | --- |
| Accounts | `GET/POST /api/accounts`, `GET /api/accounts/{id}` |
| Signals | `GET/POST /api/signals`, `GET /api/signals/{id}` |
| Trades | `GET /api/trades`, `GET /api/trades/{id}`, `GET /api/trades/{id}/execution`, `POST /api/trades/execute`, `POST /api/trades/queue-mt5` |
| Risk | `GET/PUT /api/risk/{accountId}` |
| MT5 | `POST /api/mt5/registrations`, `POST /api/mt5/heartbeat`, `POST /api/mt5/account-snapshots`, `GET /api/mt5/commands/next`, `POST /api/mt5/commands/{commandId}/acknowledge`, `POST /api/mt5/positions/snapshot` |

## 4. MT5 integration

### EA identity and authentication

1. An operator provisions an EA registration for a Boyka trading account.
2. The API generates a random API key, returns the plaintext value once, and
   stores only its SHA-256 hash.
3. The EA sends the key in `X-EA-Api-Key` for all MT5 integration routes.
4. Middleware resolves the active registration and makes its associated account
   available to MT5 endpoints.

Registration is intentionally unauthenticated for the current local demo
workflow. It must be protected by administrator authentication before a shared
or production deployment.

### Polling and command lifecycle

The EA timer performs these operations at its configured interval (five
seconds by default):

1. Sends a heartbeat.
2. Sends an account snapshot.
3. Polls for one pending command.
4. Validates and, only when explicitly enabled, submits a market order.
5. Acknowledges the actual broker result.
6. Sends a current-position snapshot.

The backend prevents cross-account acknowledgement, accepts one acknowledgement
per delivered command, and treats repeated acknowledgements as idempotent so a
lost HTTP response does not create a duplicate execution record.

Commands expire after 60 seconds by default. A `202 Accepted` response from
the queue endpoint means only that the work was queued; it is not evidence that
the broker filled the order.

### EA safety and broker compatibility

- The EA refuses to start on a non-demo account.
- `ExecutionEnabled` defaults to `false`; it must be deliberately set to
  `true` before any demo order can be submitted.
- The EA checks symbol availability, broker volume limits, expiry, and its
  execution switch before submitting an order.
- It uses MT5 account values for balance, equity, margin, free margin,
  leverage, login, broker company, server, and currency.
- Position snapshots use the compatible `PositionGetTicket(i)` approach.
- The EA is configured with an MT5 magic number and falls back from FOK to IOC
  filling when MT5 reports an invalid fill policy.

### Account snapshot contract

`POST /api/mt5/account-snapshots` currently receives:

```json
{
  "accountLogin": 346585694,
  "brokerCompany": "XM Global Limited",
  "balance": 0.0,
  "equity": 0.0,
  "margin": 0.0,
  "freeMargin": 0.0,
  "currency": "ZAR",
  "serverName": "XMGlobal-MT5 10",
  "leverage": 1000
}
```

The controller requires identity fields and verifies that `accountLogin`
matches the account bound to the authenticated EA registration. It logs the
reported identity and account values. Account and position snapshots are **not
persisted** yet by design.

### Local connectivity

For a remote MT5 terminal to access the local backend, the current documented
development setup uses ngrok forwarding to the backend HTTPS listener:

```text
public HTTPS ngrok URL -> https://localhost:7161
```

The EA's `BackendBaseUrl` and MT5's WebRequest allow-list must use the current
public ngrok base URL exactly. Free ngrok URLs can change after a tunnel restart.
See [EA_BACKEND_INTEGRATION.md](ea/EA_BACKEND_INTEGRATION.md) for setup and
verification steps.

## 5. Verification completed

| Check | Result |
| --- | --- |
| .NET solution build | Succeeds with 0 errors in an isolated output directory, so the running local API is not interrupted. |
| Backend unit tests | 20 passed, 0 failed. |
| EA compile | MetaEditor compile: 0 errors, 0 warnings. |

The solution currently emits dependency-vulnerability warnings for existing
`System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml`
packages. These warnings were not changed as part of the MT5 implementation.

## 6. Documentation available

- [README.md](README.md): backend setup and core API overview.
- [BoykaAI_Phase_2_1_MT5_Integration_Architecture.md](BoykaAI_Phase_2_1_MT5_Integration_Architecture.md): Phase 2 architecture and design decisions.
- [ea/README.md](ea/README.md): EA quick start.
- [ea/EA_BACKEND_INTEGRATION.md](ea/EA_BACKEND_INTEGRATION.md): EA/API provisioning, integration contract, testing, and troubleshooting.

## 7. Not implemented / deliberately deferred

- Live-money trading: the EA is demo-account-only.
- Mobile screens, backend connectivity, authentication, and trading controls.
- AI-generated signals, strategies, LangGraph, or Python integration.
- Persistent account snapshots and persistent MT5 position snapshots.
- Admin authentication/authorization for EA provisioning and the rest of the
  public API surface.
- API endpoints to revoke or administratively manage EA registrations, despite
  the underlying service supporting revocation.
- Enforcement of `AutoTradingEnabled` in the order queue path.
- Production hosting, rate limiting, monitoring dashboards, alerting, and
  operational reconciliation workflows.
- Real-broker execution acceptance testing. An actual MT5 demo-terminal test
  remains necessary to prove end-to-end broker order placement and reporting.

## 8. Current operational boundary

The implemented system is suitable for local development and controlled
demo-account integration testing. It is not ready for live trading or exposure
as a public production API.
