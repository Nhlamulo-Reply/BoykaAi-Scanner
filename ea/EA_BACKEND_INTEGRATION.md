# Boyka AI EA / Backend Integration Guide

This guide describes the demo-only connection between `BoykaEA.mq5` and
the Boyka ASP.NET Core backend. The EA never receives broker credentials
from the backend: it runs inside the MT5 terminal, polls the API for a
command, places a market order through MT5, and reports the broker result.

> This integration is for an MT5 **demo account only**. `BoykaEA` refuses
> to start on a live account.

## Architecture and trust boundary

```text
Boyka API + SQL Server  <- HTTPS / polling ->  BoykaEA in MT5  ->  Demo broker
       |                                      (X-EA-Api-Key)
       +-- creates and tracks command
```

- The API validates and queues a command for one Boyka trading account.
- A unique EA API key is associated with that account.
- The EA supplies the key in `X-EA-Api-Key` on every MT5 integration call.
- The backend derives the authorized account from the key; it does not trust
  an arbitrary account ID from the EA request.
- The API stores only a SHA-256 hash of the key. The plaintext key is shown
  once when the registration is provisioned.

## Prerequisites

1. .NET 10 SDK, SQL Server LocalDB (or the configured SQL Server), and MT5.
2. A broker **demo** account logged into MT5.
3. Database migrations applied:

   ```powershell
   cd C:\Users\OEM\Documents\BoykaAI\backend
   dotnet ef database update --project Boyka.Infrastructure --startup-project Boyka.API
   ```

4. The backend running with its HTTPS profile:

   ```powershell
   cd C:\Users\OEM\Documents\BoykaAI\backend
   dotnet run --project Boyka.API --launch-profile https
   ```

   The default HTTPS URL is `https://localhost:7161`. Confirm the listening
   URL printed by the application; do not assume a different port from an
   older guide.

5. In MT5: **Tools → Options → Expert Advisors** → enable **Allow WebRequest
   for listed URLs**, then add the exact API base URL, for example
   `https://localhost:7161`.

## Provision the EA identity

Use Swagger (`https://localhost:7161/swagger`) or send the following request.
The seeded demo account ID is shown below; replace it for any other account.

```http
POST /api/mt5/registrations
Content-Type: application/json

{
  "tradingAccountId": "00000000-0000-0000-0000-000000000001",
  "eaVersion": "1.0.0"
}
```

The successful response contains `rawApiKey`. Copy it immediately; it cannot
be retrieved from the backend later. Do not commit it, include it in logs, or
share it in screenshots.

Open `BoykaEA.mq5` in MetaEditor, compile it, attach it to a chart in the
demo terminal, and set these inputs:

| EA input | Value |
| --- | --- |
| `BackendBaseUrl` | The allowed API base URL, e.g. `https://localhost:7161` |
| `ApiKey` | The one-time `rawApiKey` value |
| `PollingIntervalSeconds` | `5` initially |
| `ExecutionEnabled` | `false` for connectivity testing; `true` only for a demo-order test |
| `EaVersion` | `1.0.0` |

The MT5 **Common** tab must permit algorithmic/live trading for the terminal
to submit a *demo* order. This setting does not override the EA's live-account
block.

## API contract used by the EA

All routes below are rooted at `/api/mt5`. Apart from registration, each
request needs the header `X-EA-Api-Key: <rawApiKey>`.

| Method and route | Called by | Purpose |
| --- | --- | --- |
| `POST /registrations` | Operator | Creates an EA registration and returns the key once. |
| `POST /heartbeat` | EA | Reports connectivity and EA version. |
| `POST /account-snapshots` | EA | Reports balance, equity, margin, currency, server, and leverage. |
| `GET /commands/next` | EA | Gets one queued command, or returns `204 No Content`. |
| `POST /commands/{commandId}/acknowledge` | EA | Reports the broker execution outcome. |
| `POST /positions/snapshot` | EA | Reports current MT5 positions. |

On every timer tick the EA sends a heartbeat and account snapshot, checks for
one command, and sends a position snapshot. A `200` command response includes
the command ID, account ID, broker symbol, direction, volume, entry reference,
stop loss, take profit, and expiration. The backend marks the command as
delivered before returning it.

The EA reports one of `FILLED`, `PARTIAL`, `REJECTED`, `FAILED`, or `UNKNOWN`
to the acknowledgement route. Repeating an acknowledgement is safe: the
backend treats it as idempotent and will not insert a second execution record.

## Queue a demo command

The operator queues work through the public trading route, not by calling the
EA polling route directly:

```http
POST /api/trades/queue-mt5
Content-Type: application/json

{
  "tradingAccountId": "00000000-0000-0000-0000-000000000001",
  "symbol": "XAUUSD",
  "direction": 1,
  "volume": 0.01,
  "entryPrice": 2500.00,
  "stopLoss": 2490.00,
  "takeProfit": 2520.00
}
```

`direction` uses the backend enum: `1` is Buy and `2` is Sell. Prices must be
positive and directionally valid: for Buy, stop loss is below the entry price
and take profit is above it; for Sell, the inverse applies. Use realistic
prices for the broker's current market and minimum stop-distance rules.

The response is `202 Accepted`. It means the command is queued; it does **not**
mean a broker order has executed. The command expires after 60 seconds by
default (`MT5:CommandExpirySeconds` in `appsettings.json`).

For a broker-specific symbol, add a `SymbolMapping` for the account so that a
canonical request symbol can be translated to the exact name exposed in MT5.

## Verify the demo flow

1. Confirm the EA's **Experts** log contains successful heartbeat calls.
2. Queue one small, valid command with `ExecutionEnabled=false` first. The EA
   should acknowledge it as rejected without submitting a broker order.
3. Set `ExecutionEnabled=true`, queue one small command, and wait one polling
   interval.
4. Confirm the Experts log shows the command ID, `OrderSend` result, and a
   successful acknowledgement response.
5. Query `GET /api/trades/{orderId}` and
   `GET /api/trades/{orderId}/execution`. A filled command should have an
   executed order status, a successful execution status, actual execution
   price/volume, and the MT5 ticket number.
6. Verify the matching order and position in the MT5 demo terminal.

## Failure behavior

| Symptom | Expected behavior / action |
| --- | --- |
| `HTTP 204` from command polling | Normal: no command is waiting. |
| HTTP `401` | The EA key is missing, invalid, or revoked; provision a new key and update EA inputs. |
| MT5 error `4060` | Add the exact API base URL to the MT5 WebRequest allow-list. |
| Command expires | The backend marks it expired; queue a new command rather than assuming it executed. |
| Broker rejects a command | The EA acknowledges the result and the API records a rejected execution. Inspect the broker retcode and symbol constraints. |
| Acknowledgement response is lost | Retrying the same acknowledgement is safe; it will not duplicate the execution record. |
| EA/API is offline | No broker order is created by the backend. Start/reconnect the EA, then queue a fresh command if the old one expired. |

## Operational safety

- Use HTTPS outside isolated local development; do not disable certificate
  verification in a deployed setup.
- Keep the API key only in MT5 EA inputs or another approved secret store.
- Do not expose SQL Server or the local API publicly for this demo setup.
- The current registration endpoint is intended for the local development
  workflow. Restrict provisioning behind administrator authentication before
  any shared or production deployment.
- Do not interpret a queued response as a filled trade. The broker result in
  the execution record is the source of truth.
