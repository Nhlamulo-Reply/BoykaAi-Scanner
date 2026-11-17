# Boyka AI --- Phase 2.1: MT5 Integration Architecture

**Project:** Boyka AI\
**Phase:** 2 --- MT5 Connectivity and Trade Execution\
**Checkpoint:** 2.1 --- Integration Architecture\
**Status:** Proposed --- awaiting user approval\
**Target framework:** .NET 10\
**Primary backend:** ASP.NET Core Web API\
**Trading platform:** MetaTrader 5 (MT5)\
**Initial environment:** Local Windows development machine and MT5 demo
account

------------------------------------------------------------------------

## 1. Purpose

This checkpoint defines the architecture and contracts for connecting
the existing Boyka AI backend to MetaTrader 5.

The purpose is to agree on the communication model, responsibilities,
data flow, security boundaries, and failure handling **before
implementing any MQL5 code or changing the existing Phase 1 backend**.

Phase 2.1 is design-only. Do not implement the Expert Advisor, change
database entities, add packages, create migrations, or enable real
trading during this checkpoint.

## 2. Phase 1 baseline

Phase 1 is reported complete, with these deliverables:

-   Domain entities and enums.
-   EF Core persistence and initial migration.
-   DTOs, service interfaces, and service implementations.
-   API controllers, middleware, dependency-injection configuration, and
    Swagger.
-   Mock trade execution and validation.
-   Seed data, reviewer fixes, 13 unit tests, and README.

The Phase 1 mock execution path must remain available until the MT5
integration has been tested and explicitly accepted. Do not remove or
silently replace it.

Before implementation in a later checkpoint, inspect the actual
repository and confirm the names and behavior of the existing entities,
DTOs, interfaces, endpoints, and execution services. This document does
not assume that unverified class or property names exist.

## 3. Scope

### Included in Phase 2

1.  Define a secure backend-to-MT5 communication design.
2.  Define an MQL5 Expert Advisor (EA) that runs inside an MT5 terminal.
3.  Register and identify individual trading accounts and EA
    installations.
4.  Exchange account snapshots, trading commands, and execution reports.
5.  Validate and execute approved commands on an MT5 demo account.
6.  Handle retries, duplicate delivery, terminal disconnection, broker
    rejection, and execution reporting.
7.  Test account isolation and order lifecycle behavior.

### Not included in Phase 2.1

-   Writing or compiling MQL5 code.
-   Implementing real MT5 connectivity.
-   Connecting a live trading account.
-   Enabling automatic live trading.
-   Building the mobile application.
-   Adding AI, Python, LangGraph, or AI-generated trade decisions.
-   Replacing the Phase 1 mock execution implementation.
-   Building a multi-service or microservices architecture.

## 4. Proposed architecture

The mobile application must not connect directly to MT5 and must not
hold broker credentials. The backend is the central authority for users,
registered accounts, risk rules, order approval, and command tracking.

``` text
Boyka MAUI Mobile App
        |
        | HTTPS (future mobile integration)
        v
Boyka ASP.NET Core API
        |
        +---- Application services and risk validation
        |
        +---- SQL Server / EF Core
        |
        | HTTPS polling and reporting
        v
MQL5 Expert Advisor (inside MT5 terminal)
        |
        | MT5 trading functions (CTrade)
        v
MT5 Terminal
        |
        v
Broker Demo Account
```

  -----------------------------------------------------------------------
  Component                           Responsibility
  ----------------------------------- -----------------------------------
  Boyka.API                           Authenticated HTTP endpoints,
                                      request validation, and API
                                      responses.

  Boyka.Application                   Account registration, command
                                      lifecycle, risk checks, and
                                      execution orchestration.

  Boyka.Domain                        Trading rules and domain state.

  Boyka.Infrastructure                Database persistence and external
                                      integration implementation.

  MQL5 EA                             Polls for commands, validates
                                      command identity, submits orders
                                      through MT5, and reports broker
                                      results.

  MT5 terminal                        Maintains the broker connection and
                                      performs trading operations through
                                      the terminal.

  Broker demo account                 Accepts or rejects demo orders and
                                      provides actual execution
                                      information.
  -----------------------------------------------------------------------

The EA should communicate outward to the backend using HTTPS requests
initiated by the terminal. This avoids requiring the backend to open an
inbound connection to a user's MT5 terminal.

## 5. Communication model

### 5.1 EA polling

The proposed initial model is **EA-initiated HTTPS polling**:

1.  The EA starts in an MT5 terminal.
2.  The EA identifies its registered trading account and installation.
3.  At a configured interval, it requests the next pending command for
    that account.
4.  The backend returns either no work or one command.
5.  The EA validates the command's required fields and account identity.
6.  The EA submits the order to MT5 only if the command is valid and
    execution is enabled.
7.  The EA reports the result to the backend.
8.  The backend records the execution report and updates the order
    lifecycle.

Polling interval must be configurable. The implementation checkpoint
must select a sensible initial interval and document it.

### 5.2 Why polling first

Polling is suitable for the initial local/demo implementation because:

-   The EA can initiate outbound HTTPS requests.
-   No public inbound port to the user's PC is required.
-   The backend can keep pending commands in the database.
-   A temporarily disconnected EA can reconnect and request work later.
-   The same contract can support a future hosted backend.

Polling is not instantaneous. The UI must not imply real-time execution
when the EA only checks periodically.

### 5.3 MT5 WebRequest configuration

MT5 requires the backend base URL to be added to the terminal's allowed
WebRequest URL list.

Document this terminal configuration in the implementation README. Use
HTTPS outside isolated local development. Do not disable TLS certificate
validation in production.

## 6. Account identity and isolation

Each registered trading account must have a stable internal Boyka
account ID. The EA must identify itself using a provisioned integration
identity associated with that account.

Do not rely only on a broker account number supplied in an HTTP request
as proof of identity.

Minimum isolation rules:

-   Every command is associated with one internal trading account.
-   Every EA integration identity is associated with one authorized
    account.
-   The backend derives the authorized account from the authenticated
    integration identity.
-   The backend does not trust an arbitrary account ID in the request
    body.
-   Commands and execution reports are checked against the same account
    association.
-   Credentials or tokens are not returned by account-list or
    account-detail APIs.
-   Revoking an integration identity prevents further command polling
    and reporting.

For the first implementation, use one MT5 demo account and one EA
installation. Design the contract so additional accounts can be
registered later.

## 7. Proposed API contract

These are **proposed Phase 2 integration endpoints**, not claims that
they already exist. Final route names and DTO shapes must be reconciled
with the actual Phase 1 code before implementation.

  ---------------------------------------------------------------------------------------------
  Method                  Proposed route                                Purpose
  ----------------------- --------------------------------------------- -----------------------
  POST                    `/api/mt5/registrations`                      Register/provision an
                                                                        EA installation for an
                                                                        existing Boyka trading
                                                                        account.

  POST                    `/api/mt5/heartbeat`                          Report EA connectivity
                                                                        and terminal status.

  POST                    `/api/mt5/account-snapshots`                  Submit account balance,
                                                                        equity, margin, free
                                                                        margin, currency, and
                                                                        server details.

  GET                     `/api/mt5/commands/next`                      Retrieve the next
                                                                        pending command for the
                                                                        authenticated EA
                                                                        account.

  POST                    `/api/mt5/commands/{commandId}/acknowledge`   Acknowledge receipt of
                                                                        a command.

  POST                    `/api/mt5/executions`                         Submit the result of an
                                                                        attempted execution.

  POST                    `/api/mt5/positions/snapshot`                 Submit the current
                                                                        open-position snapshot.
  ---------------------------------------------------------------------------------------------

The implementation must not blindly create all these routes. First
inspect Phase 1 and reuse existing services and models where
appropriate.

### 7.1 Command response --- illustrative

``` json
{
  "commandId": "unique-command-id",
  "accountId": "registered-boyka-account-id",
  "symbol": "XAUUSD",
  "direction": "BUY",
  "volume": 0.01,
  "stopLoss": 0.0,
  "takeProfit": 0.0,
  "createdAtUtc": "2026-11-17T18:00:00Z",
  "expiresAtUtc": "2026-11-17T18:01:00Z"
}
```

This is illustrative only. The implementation must agree whether entry
price is a requested price, a market-order reference, or omitted for
market execution. Zero SL/TP must not be interpreted as permission to
ignore a risk policy. The backend must validate required protective
settings according to approved trading rules.

### 7.2 Execution report --- illustrative

``` json
{
  "commandId": "unique-command-id",
  "accountId": "registered-boyka-account-id",
  "status": "FILLED",
  "brokerTicket": "123456789",
  "symbol": "XAUUSD",
  "direction": "BUY",
  "requestedVolume": 0.01,
  "executedVolume": 0.01,
  "executionPrice": 2650.25,
  "brokerRetcode": 10009,
  "errorMessage": null,
  "executedAtUtc": "2026-11-17T18:00:02Z"
}
```

The EA must report the actual MT5 trade result, not assume that a
successful HTTP response means the broker accepted the order.

## 8. Command and execution lifecycle

The design must distinguish between:

-   **Trade order status:** the business lifecycle of the requested
    order.
-   **Execution status:** the outcome reported by the MT5
    terminal/broker.

Proposed lifecycle:

``` text
Created
  |
  v
Validated by backend
  |
  v
Queued
  |
  v
Delivered to EA
  |
  v
Acknowledged
  |
  v
Execution attempted
  |
  +----> Filled
  |
  +----> Partially filled
  |
  +----> Rejected
  |
  +----> Failed / Unknown
```

The exact status enum values must be reconciled with the Phase 1 enums
before implementation.

A timeout or lost HTTP response must not automatically be treated as a
failed trade. The backend may not know whether MT5 submitted the order.
Such cases need an `Unknown` or reconciliation state and must not
trigger an unsafe blind resubmission.

## 9. Duplicate protection and idempotency

Every trade command must have a unique, immutable command ID generated
by the backend.

Required behavior:

-   The same command may be delivered more than once.
-   The EA must retain or otherwise track command IDs it has already
    attempted.
-   The backend must reject duplicate execution reports or process them
    idempotently.
-   The EA must not submit a second broker order merely because an HTTP
    response was lost.
-   The backend must enforce uniqueness for the relevant
    command/execution relationship.
-   Reconciliation must be possible using the command ID, broker ticket,
    account, symbol, and time.

Do not assume exactly-once delivery is possible over an unreliable
network. Design for at-least-once delivery with idempotent processing
and explicit reconciliation.

## 10. Risk and safety controls

The backend remains the authority for approving commands. The EA is an
execution adapter, not a strategy engine.

Before a command is queued, the backend must apply the approved account
risk profile and existing Phase 1 validation rules.

The EA must independently reject malformed or obviously unsafe commands,
including:

-   Missing or invalid command ID.
-   Wrong account association.
-   Unsupported symbol.
-   Invalid direction.
-   Volume outside broker min/max/step constraints.
-   Invalid price, stop-loss, or take-profit values.
-   Expired command.
-   Execution disabled or emergency stop active.

The backend must support an account-level execution enable/disable
control and a global emergency stop in a later implementation
checkpoint. Exact persistence and endpoint changes must be designed
against the existing Phase 1 model first.

Initial implementation must be demo-only. Do not enable live-account
execution as part of this phase without a separate explicit approval.

## 11. Symbol and broker compatibility

Broker symbols are not guaranteed to be identical. For example, gold may
be named `XAUUSD`, `GOLD`, or a broker-specific variant.

Do not hardcode a single broker's symbol suffix. The design should allow
an account-specific mapping between Boyka's canonical symbol and the
symbol available in that MT5 terminal.

The EA must check that a symbol is available and selected in Market
Watch before attempting execution.

Broker constraints must be read from MT5 where possible, including:

-   Minimum volume.
-   Maximum volume.
-   Volume step.
-   Digits and point size.
-   Minimum stop distance / stop level.
-   Trading availability and symbol trade mode.

## 12. Connectivity, retries, and recovery

The design must account for:

-   MT5 terminal closed.
-   EA removed or disabled.
-   Internet disconnected.
-   Backend unavailable.
-   HTTP timeout.
-   Authentication failure.
-   Invalid JSON or unsupported contract version.
-   Broker market closed.
-   Broker rejection.
-   Partial fill.
-   EA restart while a command is in progress.

Required principles:

-   Use bounded retries with backoff for transient network failures.
-   Do not retry a broker order blindly after an ambiguous result.
-   Record heartbeat and last-seen time.
-   Keep command state persistent in the backend database.
-   Allow an EA to reconnect and resume polling.
-   Log failures with correlation IDs and timestamps.
-   Never log API secrets, access tokens, or broker passwords.

## 13. Security design

Minimum requirements:

1.  HTTPS for backend communication.
2.  A unique integration credential per EA installation/account.
3.  Secure credential provisioning and revocation.
4.  Store credential hashes where practical; do not store plaintext
    secrets unnecessarily.
5.  Keep credentials out of source control, logs, screenshots, and API
    responses.
6.  Validate every request server-side.
7.  Apply rate limits to integration endpoints.
8.  Use request correlation IDs and audit important actions.
9.  Prevent cross-account command access.
10. Keep broker login credentials inside the MT5 terminal; never send
    them to the mobile app or Boyka API.
11. Do not expose SQL Server directly to the internet.
12. Do not use a hardcoded shared secret in committed MQL5 source code.

The exact authentication mechanism and provisioning flow must be
selected before Checkpoint 2.2 implementation.

## 14. Local development deployment

Initial development environment:

-   Windows 11 development PC.
-   Boyka ASP.NET Core API running locally.
-   SQL Server development database.
-   One MT5 terminal.
-   One MT5 demo trading account.
-   One EA installation.

The API and MT5 terminal can run on the same PC for the first
integration test. The MT5 terminal must be configured to allow
WebRequest calls to the API base URL.

If testing from a different device or network, use a secured private
tunnel/VPN approach. Do not expose the development API or SQL Server
publicly without an explicit security design.

No paid hosting is required for this initial local demo setup. The PC
must remain on and connected while the local integration is being
tested.

## 15. Observability and audit

The integration should make it possible to answer:

-   Which account and EA installation requested a command?
-   When was the command created, delivered, acknowledged, and
    attempted?
-   Which broker ticket was returned?
-   What was the actual execution price and volume?
-   Was the command rejected, partially filled, failed, or left in an
    unknown state?
-   What error or broker return code occurred?
-   Was the command retried, and why?

Use structured logs and correlation IDs. Persist business-critical
command and execution state in SQL Server rather than relying only on
console logs.

## 16. Testing strategy

The later implementation checkpoints must include tests for:

-   Valid demo account registration.
-   Unauthorized EA identity.
-   EA identity attempting to access another account.
-   No pending command.
-   Valid command retrieval.
-   Expired command.
-   Duplicate command delivery.
-   Duplicate execution report.
-   Invalid direction and volume.
-   Unsupported symbol.
-   Broker rejection.
-   Network timeout and recovery.
-   EA restart during command processing.
-   Partial fill and unknown execution result.
-   Account-level execution disabled.
-   Global emergency stop.

Do not claim broker execution tests have passed unless an actual MT5
demo terminal and broker demo account were used. Unit tests and
simulated HTTP tests must be labelled separately from real MT5 demo
integration tests.

## 17. Decisions required before Checkpoint 2.2

The following decisions must be confirmed before writing the EA:

1.  **Initial account:** one MT5 demo account only.
2.  **Communication:** EA-initiated HTTPS polling.
3.  **Deployment:** local Windows PC for initial development.
4.  **Execution:** demo-only; no live trading.
5.  **Command type:** confirm whether the first version supports market
    BUY/SELL only.
6.  **Protection:** confirm whether every order must include both
    stop-loss and take-profit.
7.  **Polling interval:** select an initial interval and make it
    configurable.
8.  **Authentication:** approve a per-installation credential and
    provisioning/revocation design.
9.  **Symbols:** confirm the initial demo broker's exact gold symbol and
    whether other symbols are required.

Do not silently choose answers to decisions that affect trading behavior
or security.

## 18. Separate workstream: Liquidity Supply/Demand indicator

The uploaded `Liquidity Supply Demand Indicator.txt` is **not part of
Phase 2.1 MT5 connectivity** and must not be mixed into the EA
integration work.

It discusses the distinction between a zone break and a meaningful
structure break, including:

-   Demand Broken when price closes below demand.
-   Supply Broken when price closes above supply.
-   The risk that weak breaks may be liquidity sweeps rather than
    genuine structure breaks.
-   Filtering with candle displacement relative to ATR.
-   Waiting for a cleaner BOS and retest rather than reacting to every
    label.

Treat this as a separate future indicator/strategy-analysis workstream.
Do not convert these suggestions into trading rules or execute trades
from them without a separate specification, review, and explicit
approval.

## 19. Checkpoint 2.1 deliverables

Checkpoint 2.1 is complete only when:

-   The architecture is reviewed and approved.
-   Component responsibilities are clear.
-   The polling and reporting flow is agreed.
-   Account identity and isolation are agreed.
-   Proposed API contracts are reconciled with Phase 1.
-   Command lifecycle and duplicate protection are agreed.
-   Security and recovery requirements are documented.
-   The decisions in Section 17 are answered or explicitly deferred.

No source code changes are required for this checkpoint.

## 20. Approval gate

**STOP HERE.**

Do not begin Checkpoint 2.2 or write MQL5 code until the user explicitly
approves Checkpoint 2.1.

After review, report:

-   Decisions confirmed.
-   Decisions still open.
-   Any conflicts with the actual Phase 1 implementation.
-   Recommended adjustments and their reasons.

Wait for explicit approval before continuing.
