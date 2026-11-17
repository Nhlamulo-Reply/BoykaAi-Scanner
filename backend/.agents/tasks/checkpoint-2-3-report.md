# Checkpoint 2.3 — MQL5 Expert Advisor
> Generated after implementation

---

## Checkpoint completed: 2.3 — MQL5 Expert Advisor

---

## Files created

| File | Purpose |
|---|---|
| `c:\Users\OEM\Documents\BoykaAI\ea\BoykaEA.mq5` | Complete MQL5 Expert Advisor source |
| `c:\Users\OEM\Documents\BoykaAI\ea\README.md` | Setup, usage, and troubleshooting guide |

---

## EA architecture summary

### Input parameters
Seven configurable inputs exposed in the MT5 EA properties dialog:  
`BackendBaseUrl`, `ApiKey`, `PollingIntervalSeconds`, `ExecutionEnabled`,  
`EaVersion`, `MagicNumber`, `SlippagePoints`.

### Lifecycle callbacks

| Callback | Responsibility |
|---|---|
| `OnInit` | Validates demo-account guard and non-empty `ApiKey`; starts the timer; sends an initial heartbeat. Returns `INIT_FAILED` on violations. |
| `OnDeinit` | Kills the polling timer. |
| `OnTimer` | Fires every `PollingIntervalSeconds`. Calls the four main functions in order: heartbeat → account snapshot → poll/execute → position snapshot. |

### Application functions

| Function | Responsibility |
|---|---|
| `SendHeartbeat()` | POSTs `eaVersion`, `terminalConnected`, `accountBalance`, `accountEquity`, `accountCurrency` to `POST /api/mt5/heartbeat`. |
| `SendAccountSnapshot()` | POSTs balance, equity, margin, free margin, currency, server name, and leverage to `POST /api/mt5/account-snapshots`. |
| `PollAndExecuteCommand()` | GETs `/api/mt5/commands/next`. On 204 returns silently. On 200 parses JSON, checks expiry, checks `ExecutionEnabled`, validates symbol, adjusts volume to broker constraints, calls `OrderSend`, maps retcode to status, calls `AcknowledgeCommand`. |
| `AcknowledgeCommand(...)` | POSTs execution result JSON to `POST /api/mt5/commands/{commandId}/acknowledge`. |
| `SendPositionSnapshot()` | Iterates `PositionsTotal()`, serialises each position, POSTs array wrapper to `POST /api/mt5/positions/snapshot`. |

### HTTP helpers

| Function | Responsibility |
|---|---|
| `HttpPost(url, json, &body)` | Encodes JSON to UTF-8 `char[]`, sets `Content-Type` and `X-EA-Api-Key` headers, calls `WebRequest("POST",...)`. Returns HTTP status code or -1. |
| `HttpGet(url, &body)` | Sets `X-EA-Api-Key` header, calls `WebRequest("GET",...)`. Returns HTTP status code or -1. |

### Utility functions

| Function | Responsibility |
|---|---|
| `RetcodeToStatus(retcode)` | Maps MT5 retcodes to `"FILLED"`, `"PARTIAL"`, `"REJECTED"`, `"FAILED"`, `"UNKNOWN"`. |
| `ToIso8601Utc()` | Returns current GMT time as `"YYYY-MM-DDTHH:MM:SSZ"`. |
| `StructToIso8601(dt)` | Formats any `datetime` value as ISO 8601 UTC string. |
| `ParseIso8601(s)` | Parses `"YYYY-MM-DDTHH:MM:SSZ"` into a `datetime` for expiry checking. |
| `EscapeJson(s)` | Minimal JSON escaping: escapes `\` then `"`. |
| `JsonExtractString(json, key)` | Extracts a string field value from a flat JSON object by string search. |
| `JsonExtractDouble(json, key)` | Extracts a numeric field value from a flat JSON object by string search. |

---

## Endpoint URLs in BoykaEA.mq5

All seven backend endpoints are present:

| # | Method | URL |
|---|---|---|
| 1 | POST | `{BackendBaseUrl}/api/mt5/heartbeat` |
| 2 | POST | `{BackendBaseUrl}/api/mt5/account-snapshots` |
| 3 | GET  | `{BackendBaseUrl}/api/mt5/commands/next` |
| 4 | POST | `{BackendBaseUrl}/api/mt5/commands/{commandId}/acknowledge` |
| 5 | POST | `{BackendBaseUrl}/api/mt5/positions/snapshot` |

> Note: `POST /api/mt5/registrations` is a one-time provisioning step  
> performed manually via Swagger, not from within the EA.  
> There are therefore five URL call-sites in the EA source.

---

## Demo-account guard

```mql5
if(AccountInfoInteger(ACCOUNT_TRADE_MODE) != ACCOUNT_TRADE_MODE_DEMO)
{
    Print("[BoykaEA] ERROR: This EA may only run on a DEMO account. Aborting.");
    return INIT_FAILED;
}
```

Located in `OnInit()`, before any timer or HTTP call. The EA will not  
initialise on a live or contest account.

---

## MT5 terminal configuration steps

1. Open MetaTrader 5.
2. Go to **Tools → Options → Expert Advisors**.
3. Check **"Allow WebRequest for listed URLs"**.
4. Add `https://localhost:5001` (or your actual backend URL) to the list.
5. Click **OK** and restart the terminal if prompted.

---

## Known MQL5 limitations

| Limitation | Impact |
|---|---|
| No async/await | All HTTP calls block the timer thread for up to `HTTP_TIMEOUT_MS` (5 s). During a slow request the chart will not update. |
| No JSON parser in standard library | JSON is built by `StringFormat` and parsed by string search. Only flat objects are supported. Nested or escaped string values may not parse correctly. |
| Single-threaded execution | `OnTimer` and `OnTick` share the same thread. If `OnTimer` is slow, `OnTick` is delayed. |
| `WebRequest` URL allow-list | Every backend URL must be manually added in MT5 options. CI/CD cannot do this automatically. |
| 31-character order comment limit | The `commandId` GUID is truncated to fit: `"BoykaAI:" + commandId` → first 31 chars only. |
| No retry on network errors | A failed `WebRequest` is logged and skipped. The next timer tick will retry naturally. |
| `ORDER_FILLING_FOK` not universal | Some brokers do not support FOK. The EA retries with IOC on retcode 10030, but may still fail if the broker supports only `SYMBOL_FILLING_RETURN`. |
| `TimeGMT()` vs broker time | `TimeGMT()` is used for expiry comparison. If broker server time drifts from GMT, commands may be wrongly flagged as expired. |

---

## Integration test checklist (manual)

Perform these steps after the backend is running and the EA is attached to a XAUUSD chart.

- [ ] **Heartbeat round-trip**: Wait one polling interval. Check MT5 Experts tab for  
  `SendHeartbeat: HTTP 200`. Check the backend logs for a heartbeat record.

- [ ] **Account snapshot**: Confirm `SendAccountSnapshot: HTTP 200` appears in Experts tab.

- [ ] **No-command poll**: Confirm `PollAndExecuteCommand: HTTP 204` appears (empty queue is normal).

- [ ] **Command end-to-end (buy)**:
  1. Use Swagger to call `POST /api/trades` or the queue endpoint to create a Buy XAUUSD command.
  2. Wait one polling interval.
  3. Confirm `Command received:` appears in Experts tab.
  4. Confirm `OrderSend result: retcode=10009 status=FILLED` (demo account).
  5. Confirm `AcknowledgeCommand: HTTP 200`.
  6. Confirm the order appears in MT5 Trade tab.
  7. Confirm the backend command row transitions to `Acknowledged`.

- [ ] **Command end-to-end (sell)**: Repeat the above with a Sell command.

- [ ] **Expired command**: Set a very short expiry (e.g. 2 seconds) on a command.  
  Wait until it expires, then wait one polling interval.  
  Confirm `Acknowledging as UNKNOWN` and `errorMessage="Command expired..."`.

- [ ] **ExecutionEnabled=false**: Set the input to false and re-attach.  
  Create a command. Confirm `Acknowledging command as REJECTED` with  
  `"Execution disabled on this EA installation."`.

- [ ] **Position snapshot**: After placing a trade, confirm `SendPositionSnapshot: HTTP 200`  
  and that the backend logs the correct position count.

- [ ] **API key validation**: Enter a wrong API key. Confirm all requests return HTTP 401  
  and that no orders are placed.

- [ ] **URL not in allow-list**: Remove the backend URL from MT5 options.  
  Confirm `GetLastError=4060` appears in Experts tab and no orders are placed.

---

## Next checkpoint: 2.4 — Integration testing and end-to-end validation

STOPPED — waiting for your approval before starting Checkpoint 2.4.
