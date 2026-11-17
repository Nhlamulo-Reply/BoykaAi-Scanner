# Mock trade execution and risk validation (Checkpoint 1.5)

This checkpoint adds the first real execution path to the Boyka backend: `TradeOrderService.ExecuteTradeAsync` runs a full 15-step validation and persistence pipeline before delegating to `MockTradeExecutionService`, which simulates broker execution at ~90% success. The design is a two-phase persist pattern — save the accepted order, run the mock, persist the execution record, then update the order status in a final save. The approach is correct for the scope described.

**Watch for:** The `TradeExecution.Id` field is set by the entity's default initializer in `FakeExecutionService` but is unset in `MockTradeExecutionService` (confirmed). The gap is benign under EF Core InMemory but would surface as a duplicate-key error or zero-guid row in SQL Server if EF isn't generating the value server-side. Additionally, six of the ten tests share a boilerplate account-seeding block that isn't extracted into a helper, making the test file harder to extend and masking which properties actually matter to each test.

**Verdict**: APPROVED

---

## High-level view

`TradeOrderService.ExecuteTradeAsync` implements the full validation sequence correctly: account existence and active checks come first, the direction guard rejects anything that isn't the two known enum values, volume bounds (> 0, ≤ 100) are enforced before the price checks, and directional SL/TP validation follows the standard retail forex convention where the stop-loss and take-profit must be on opposite sides of the entry price relative to trade direction. The risk profile checks are gated on an enabled profile existing, so accounts without one pass through freely.

`MockTradeExecutionService` correctly reflects the entry price, volume, and null MT5 ticket number into the execution record. The `Id` field is not explicitly set in the production mock — unlike in `FakeExecutionService`, which sets it — so the returned entity relies on the entity class's `Guid.NewGuid()` default initializer. That initializer runs at object construction, so the field will have a valid GUID before the record is persisted; the concern is whether EF's value-generation strategy for SQL Server will treat that pre-set value correctly or override it.

The test suite is fully deterministic: `FakeExecutionService` always returns `Success`, each test gets an isolated InMemory database, and the probabilistic mock behavior is exercised separately in `MockTradeExecutionServiceTests`. The 50-iteration loop in `ExecuteAsync_StatusIsSuccessOrFailed` verifies that neither a third enum value nor a thrown exception escapes the mock. The happy path and both risk profile guards are not tested (see Issues).

---

<details>
<summary>Issues (3)</summary>

1. **TradeExecution.Id not set in MockTradeExecutionService** — `MockTradeExecutionService.ExecuteAsync` does not assign `Id`; it relies on the entity's field initializer (`Guid.NewGuid()`). `FakeExecutionService` explicitly sets `Id = Guid.NewGuid()`. The two paths are asymmetric. Under SQL Server, if EF's column is configured with `ValueGeneratedOnAdd` and EF detects a non-default GUID, it will use the initializer value; if the column is configured as a database-generated identity, EF will silently override it. Neither case causes a runtime failure today (InMemory doesn't enforce this), but the asymmetry between the mock and the fake warrants an explicit `Id = Guid.NewGuid()` assignment in `MockTradeExecutionService` to match `FakeExecutionService` and make the intent clear.

2. **No test for the success path with status round-trip** — The test suite covers every rejection guard but has no test that runs `ExecuteTradeAsync` to completion and asserts the returned response has `Status == Executed`. Without this, the two-phase persist logic and `MapToResponse` are only exercised by the build, not by an assertion. Add a happy-path test that seeds an active account, calls `ExecuteTradeAsync`, and asserts `response.Status == TradeOrderStatus.Executed`.

3. **Risk profile validation is untested** — The `MaximumLotSize` and `MaximumOpenPositions` guards in steps 8a and 8b have no corresponding test cases. A seeded risk profile with `MaximumLotSize = 0.5m` and a request with `Volume = 1.0m`, or a seeded risk profile with `MaximumOpenPositions = 1` and one pre-existing open order, would cover both branches.

</details>

---

<details>
<summary>Details</summary>

## TradeExecution.Id initialization asymmetry

`MockTradeExecutionService` constructs `TradeExecution` with no explicit `Id` assignment:

```csharp
var execution = new TradeExecution
{
    TradeOrderId = order.Id,
    MT5TicketNumber = null,
    ExecutionPrice = order.EntryPrice,
    ExecutedVolume = order.Volume,
    ExecutedAt = DateTime.UtcNow
};
```

`TradeExecution.Id` is declared as `public Guid Id { get; set; } = Guid.NewGuid();`. The field initializer fires at construction, so the GUID will be populated before `_context.TradeExecutions.Add(execution)` is called. Under EF Core InMemory, this works as expected. Under SQL Server, the behavior depends on the column's value-generation annotation. If the column is configured with `ValueGeneratedOnAdd` (the EF default for `Guid` PKs when using the `HiLo` or `Guid.NewGuid()` strategies), EF will respect the pre-set value. If it's configured as a server-generated `NEWSEQUENTIALID()`, EF will not. The domain entity has no explicit `[DatabaseGenerated]` attribute, so the EF convention applies — which does respect client-generated GUIDs. The risk is low but the asymmetry with `FakeExecutionService` (which sets `Id` explicitly) leaves a latent inconsistency.

## Two-phase persistence and future idempotency

A crash after the order is persisted but before execution leaves an `Accepted` order with no execution record; a crash after execution is persisted but before the status update leaves the order stuck in `Accepted`. Neither is harmful with a mock service, but when a real MT5 adapter replaces the mock, the two-phase pattern will need an idempotency or compensation mechanism to avoid re-submitting already-executed orders.

## Test coverage gaps

The happy path — an active account, valid request, `FakeExecutionService` returns Success — has no test asserting `response.Status == Executed`. The two risk profile guards (`MaximumLotSize` and `MaximumOpenPositions`) are also untested. The six existing tests each correctly isolate a single rejection guard, but neither the affirmative outcome nor the risk profile branches are covered.

</details>

---

<details>
<summary>File map</summary>

| File | Change |
|---|---|
| `Boyka.Application/Interfaces/IMockTradeExecutionService.cs` | New interface: `Task<TradeExecution> ExecuteAsync(TradeOrder)` |
| `Boyka.Infrastructure/Services/MockTradeExecutionService.cs` | New mock implementation: ~90% success, reflects entry price and volume, MT5 ticket null |
| `Boyka.Application/Services/TradeOrderService.cs` | Replaced stub with full 15-step validation and two-phase persistence pipeline |
| `Boyka.API/Extensions/ServiceCollectionExtensions.cs` | Added `IMockTradeExecutionService → MockTradeExecutionService` as Scoped |
| `Boyka.Tests/TradeOrderServiceTests.cs` | 6 deterministic rejection tests using `FakeExecutionService` and per-test InMemory databases |
| `Boyka.Tests/MockTradeExecutionServiceTests.cs` | 4 tests covering field mapping and status range |

Full diff: `git diff main -- Boyka.Application/Interfaces/IMockTradeExecutionService.cs Boyka.Infrastructure/Services/MockTradeExecutionService.cs Boyka.Application/Services/TradeOrderService.cs Boyka.API/Extensions/ServiceCollectionExtensions.cs Boyka.Tests/`

</details>
