# Boyka AI — Backend

Phase 1 backend for the Boyka AI trading assistant. Provides a REST API for managing trading accounts, signals, trade orders, executions, and risk profiles. All trade execution is simulated (no real broker or MT5 connectivity).

---

## Prerequisites

| Requirement | Version / Notes |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 |
| SQL Server Express or LocalDB | `(localdb)\mssqllocaldb` is used by default |
| IDE | Visual Studio 2022, VS Code, or JetBrains Rider |

---

## Local Configuration

The default connection string in `backend/Boyka.API/appsettings.json` targets LocalDB:

```json
"ConnectionStrings": {
  "BoykaDb": "Server=(localdb)\\mssqllocaldb;Database=BoykaAI;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

To override with SQL Server Express, use .NET user secrets (never commit credentials):

```bash
cd backend/Boyka.API
dotnet user-secrets set "ConnectionStrings:BoykaDb" "Server=.\\SQLEXPRESS;Database=BoykaAI;Trusted_Connection=True;MultipleActiveResultSets=true"
```

---

## Database Setup and Migrations

Apply the schema and seed data with:

```bash
cd backend
dotnet ef database update --project Boyka.Infrastructure --startup-project Boyka.API
```

This runs the `SeedDemoAccount` migration, which creates all tables and inserts one demo account:

- **Id:** `00000000-0000-0000-0000-000000000001`
- **AccountName:** Demo Account
- **BrokerName:** Mock Broker

---

## Run Commands

```bash
cd backend/Boyka.API
dotnet run
```

---

## Swagger UI

After starting the API, open the Swagger UI in your browser:

```
https://localhost:{port}/swagger
```

The port is printed in the console output when the application starts (e.g., `Now listening on: https://localhost:5001`). You can also check `backend/Boyka.API/Properties/launchSettings.json` for the configured ports.

---

## API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/accounts` | List all trading accounts |
| POST | `/api/accounts` | Create a new trading account |
| GET | `/api/accounts/{id}` | Get a specific trading account by ID |
| GET | `/api/signals` | List all trade signals |
| POST | `/api/signals` | Create a new trade signal |
| GET | `/api/trades` | List all trade orders |
| POST | `/api/trades/execute` | Execute a trade order (mock execution) |
| GET | `/api/trades/{id}` | Get a specific trade order by ID |
| GET | `/api/trades/{id}/execution` | Get the execution record(s) for a trade |
| GET | `/api/risk/{accountId}` | Get the risk profile for an account |
| PUT | `/api/risk/{accountId}` | Create or update the risk profile for an account |

---

## Sample Requests and Responses

### POST /api/accounts

**Request:**
```json
{
  "accountName": "My Live Account",
  "brokerName": "ACME Broker",
  "accountNumber": "MT5-98765",
  "serverName": "ACMEBroker-Live",
  "accountCurrency": "USD",
  "isDemo": false,
  "executionMode": "Live"
}
```

**Response (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accountName": "My Live Account",
  "brokerName": "ACME Broker",
  "accountNumber": "MT5-98765",
  "serverName": "ACMEBroker-Live",
  "accountCurrency": "USD",
  "isDemo": false,
  "executionMode": "Live",
  "isActive": true,
  "createdAt": "2026-01-15T10:00:00Z"
}
```

---

### POST /api/trades/execute

**Request (BUY EURUSD):**
```json
{
  "tradingAccountId": "00000000-0000-0000-0000-000000000001",
  "symbol": "EURUSD",
  "direction": 0,
  "volume": 0.1,
  "entryPrice": 1.1000,
  "stopLoss": 1.0900,
  "takeProfit": 1.1200
}
```

> `direction`: `0` = Buy, `1` = Sell

**Response (202 Accepted) — successful mock execution:**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "tradingAccountId": "00000000-0000-0000-0000-000000000001",
  "tradeSignalId": null,
  "symbol": "EURUSD",
  "direction": 0,
  "volume": 0.1,
  "entryPrice": 1.1000,
  "stopLoss": 1.0900,
  "takeProfit": 1.1200,
  "status": 4,
  "statusName": "Executed",
  "createdAt": "2026-01-15T10:05:00Z"
}
```

**Response (202 Accepted) — simulated rejection (~10% of calls):**
```json
{
  "status": 3,
  "statusName": "Rejected",
  ...
}
```

> `MT5TicketNumber` is always `null` in mock mode.

---

### PUT /api/risk/{accountId}

**Request:**
```json
{
  "riskPercentage": 2.0,
  "maximumLotSize": 1.0,
  "maximumDailyLoss": 500.0,
  "maximumOpenPositions": 5,
  "isEnabled": true
}
```

**Response (200 OK):**
```json
{
  "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "tradingAccountId": "00000000-0000-0000-0000-000000000001",
  "riskPercentage": 2.0,
  "maximumLotSize": 1.0,
  "maximumDailyLoss": 500.0,
  "maximumOpenPositions": 5,
  "isEnabled": true
}
```

---

## Known Limitations

- **Mock execution only:** no real MT5 instance or broker connectivity.
- **MT5TicketNumber is always null:** simulated executions never produce a real ticket.
- **~10% simulated failures:** `MockTradeExecutionService` randomly rejects roughly 1 in 10 orders.
- **No authentication or authorization:** all endpoints are open.
- **No real-time price feeds:** entry price is taken directly from the request.
- **Phase 1 only:** mobile app integration, real MT5 connectivity, and advanced analytics are planned for future phases.
