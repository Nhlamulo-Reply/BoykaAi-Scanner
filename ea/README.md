# Boyka AI — MQL5 Expert Advisor

**BoykaEA.mq5** connects a MetaTrader 5 demo account to the Boyka AI backend.  
It polls the backend for pending trade commands, executes them as market orders,  
and reports results back through the REST API.

> **Demo accounts only.** The EA refuses to run on live accounts.

For the full API contract, provisioning flow, command lifecycle, and demo
verification procedure, see [EA_BACKEND_INTEGRATION.md](EA_BACKEND_INTEGRATION.md).

---

## Prerequisites

| Requirement | Notes |
|---|---|
| MetaTrader 5 | Any broker demo account |
| Boyka AI backend | Running locally or on a reachable host |
| SQL Server Express | Backend database must be migrated |
| .NET 10 SDK | Required to run the backend |
| EA API key | Provisioned via `POST /api/mt5/registrations` (see below) |

---

## 1 — Configure MT5 to allow WebRequests

The EA uses MT5's built-in `WebRequest` function to talk to the backend.  
MT5 blocks all outbound HTTP by default.

1. Open MetaTrader 5.
2. Go to **Tools → Options → Expert Advisors**.
3. Check **"Allow WebRequest for listed URLs"**.
4. Click **Add** and enter your backend base URL, for example:
   ```
   https://localhost:7161
   ```
5. Click **OK**.

Without this step every HTTP call will fail silently with error `4060`  
(`ERR_WEBREQUEST_INVALID_ADDRESS`) and the EA will log  
`"URL not in allowed list"`.

---

## 2 — Start the backend

```powershell
cd c:\Users\OEM\Documents\BoykaAI\backend
dotnet run --project Boyka.API
```

The API will be available at `https://localhost:7161`.  
Swagger UI: `https://localhost:7161/swagger`

---

## 3 — Provision an EA API key

The EA needs a unique key so the backend can identify which trading account  
it belongs to.

1. Open Swagger UI at `https://localhost:7161/swagger`.
2. Find **POST /api/mt5/registrations**.
3. Send a request body like:

   ```json
   {
     "tradingAccountId": "00000000-0000-0000-0000-000000000001",
     "eaVersion": "1.0.0",
     "terminalId": "my-terminal-1"
   }
   ```

4. The response contains a `rawApiKey` field.  
   **Copy it immediately — it is shown only once.**

5. Store the key in a safe place. You will paste it into the EA's  
   `ApiKey` input parameter.

---

## 4 — Compile the EA in MetaEditor

1. Open **MetaEditor** (press **F4** in MT5, or open from the taskbar).
2. Go to **File → Open** and open `BoykaEA.mq5`.
3. Press **F7** (or **Compile** button) to compile.
4. Confirm the Output window shows `0 errors`.
5. The compiled `.ex5` file is placed automatically in the MT5  
   `MQL5\Experts` folder.

---

## 5 — Attach the EA to a chart

1. In MT5, open a chart for **XAUUSD** (or any symbol on a demo account).
   The recommended timeframe is **M1** or **M5** so the timer fires regularly.
2. In the Navigator panel, expand **Expert Advisors** and double-click  
   **BoykaEA** (or drag it onto the chart).
3. In the EA properties dialog:
   - **Inputs tab** — fill in the parameters (see table below).
   - **Common tab** — ensure **"Allow live trading"** is checked.
4. Click **OK**.

A smiley face icon in the chart's top-right corner confirms the EA is running.

---

## 6 — Input parameters reference

| Parameter | Default | Description |
|---|---|---|
| `BackendBaseUrl` | `https://localhost:7161` | Base URL of the Boyka AI backend. Must match the URL you added to the MT5 WebRequest allow-list. |
| `ApiKey` | _(empty)_ | The raw API key returned by `POST /api/mt5/registrations`. **Required.** The EA will not start without it. |
| `PollingIntervalSeconds` | `5` | How often (in seconds) the EA polls for new commands, sends heartbeats, and updates snapshots. |
| `ExecutionEnabled` | `true` | Safety switch. Set to `false` to have the EA receive and log commands without placing any broker orders. Useful for testing connectivity. |
| `EaVersion` | `1.0.0` | Version string reported to the backend in every heartbeat. |
| `MagicNumber` | `20261117` | Integer stamped on every order placed by this EA. Lets you distinguish EA orders from manual orders in the history. |
| `SlippagePoints` | `10` | Maximum allowed slippage in points. Orders outside this range will be rejected by the broker. |

---

## 7 — How the polling loop works

On every timer tick (`PollingIntervalSeconds`) the EA performs these steps in order:

1. **Heartbeat** — POST account balance and equity to `/api/mt5/heartbeat`.
2. **Account snapshot** — POST detailed margin/leverage data to `/api/mt5/account-snapshots`.
3. **Poll for command** — GET `/api/mt5/commands/next`.
   - `204 No Content` → no pending command; loop continues.
   - `200 OK` → command received; EA validates, executes, and acknowledges it.
4. **Position snapshot** — POST all open positions to `/api/mt5/positions/snapshot`.

Command execution follows this sequence:

```
GET /api/mt5/commands/next
        ↓ 200 OK
  Parse command JSON
        ↓
  Check expiry (expiresAtUtc)
        ↓ not expired
  Check ExecutionEnabled flag
        ↓ enabled
  Validate symbol in Market Watch
        ↓ valid
  Adjust volume to broker step/min/max
        ↓
  OrderSend (market order, FOK filling)
        ↓ if FOK rejected → retry with IOC
        ↓
  POST /api/mt5/commands/{id}/acknowledge
```

---

## 8 — Reading EA logs

All EA messages appear in the **Experts** tab of the MT5 terminal  
(bottom panel → **Experts**).

Useful messages to look for:

| Message | Meaning |
|---|---|
| `[BoykaEA] Initialized. Polling every N second(s).` | EA started successfully. |
| `SendHeartbeat: HTTP 200` | Backend received the heartbeat. |
| `PollAndExecuteCommand: HTTP 204` | No command queued — normal. |
| `Command received: <id>` | A command was dequeued. |
| `OrderSend result: retcode=10009 status=FILLED` | Order executed successfully. |
| `AcknowledgeCommand: HTTP 200` | Backend accepted the result. |

---

## 9 — Troubleshooting

### "URL not in allowed list" / `GetLastError=4060`

You have not added the backend URL to MT5's WebRequest allow-list.  
Follow Section 1 of this guide.  
The URL must match exactly (scheme + host + port).

### EA stops immediately with "ApiKey input is empty"

The `ApiKey` input is blank. Provision a key (Section 3) and re-attach the EA.

### EA stops with "DEMO account only"

You attached the EA to a live account. Use a demo account.

### "No commands appearing"

- Confirm the backend is running and reachable from your machine.
- Use Swagger to call `POST /api/trades/queue-mt5` to create a queued command,  
  then wait for the next polling tick.
- Check that the `TradingAccountId` in your EA registration matches  
  the account that has commands queued.

### HTTP 401 on all requests

The API key does not match any active registration.  
Provision a new key and update the EA input.

### `OrderSend` fails with retcode 10030 (invalid fill)

The broker does not support FOK filling. The EA automatically retries  
with IOC. If that also fails, reduce `SlippagePoints` or check the  
broker's minimum volume requirements.

---

## 10 — Security notes

- Never share your `ApiKey`. It grants backend access to your account data.
- The backend stores only a SHA-256 hash of the key — the plaintext  
  is returned once at provisioning and never logged.
- The EA logs do not print the API key.
- Do not use this EA on a live account.
