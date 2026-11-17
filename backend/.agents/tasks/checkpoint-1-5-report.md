# Checkpoint completed: 1.5 — Mock trade execution and risk validation

## Files created or modified

| File | Action | Purpose |
|---|---|---|
| `Boyka.Application/Interfaces/IMockTradeExecutionService.cs` | Created | Interface for the mock execution service; `Task<TradeExecution> ExecuteAsync(TradeOrder)` |
| `Boyka.Infrastructure/Services/MockTradeExecutionService.cs` | Created | Concrete mock: simulates ~90% success, sets `ExecutionPrice`, `ExecutedVolume`, `MT5TicketNumber=null` |
| `Boyka.Application/Services/TradeOrderService.cs` | Modified | Full validated execution path with risk profile checks and two-phase persistence |
| `Boyka.API/Extensions/ServiceCollectionExtensions.cs` | Modified | Added `using Boyka.Infrastructure.Services;` and `AddScoped<IMockTradeExecutionService, MockTradeExecutionService>()` |
| `Boyka.Tests/Boyka.Tests.csproj` | Created | xUnit test project targeting net10.0 with EF Core InMemory, Moq, and project references |
| `Boyka.slnx` | Modified | Added `<Project Path="Boyka.Tests/Boyka.Tests.csproj" />` |
| `Boyka.Tests/MockTradeExecutionServiceTests.cs` | Created | 4 tests for `MockTradeExecutionService` (MT5TicketNumber null, price, volume, status range) |
| `Boyka.Tests/TradeOrderServiceTests.cs` | Created | 6 tests for `TradeOrderService.ExecuteTradeAsync` validation path |

## Implementation summary

`IMockTradeExecutionService` is placed in `Boyka.Application.Interfaces` to match the convention that all service interfaces live there. `MockTradeExecutionService` goes in `Boyka.Infrastructure.Services` as the first file in that directory, using a `private static readonly Random _rng` to avoid correlated seeds on rapid successive calls.

`TradeOrderService.ExecuteTradeAsync` now runs a full 15-step pipeline:
1. Account existence check (KeyNotFoundException)
2. Account active check
3. Direction validity (Buy or Sell only)
4. Volume > 0
5. Volume ≤ 100
6. All three prices > 0
7. Directional SL/TP validation (see Assumption below)
8. Optional risk profile checks (MaximumLotSize, MaximumOpenPositions)
9. Create order with `Accepted` status and persist
10. Call `_mockExecutionService.ExecuteAsync`, set `execution.TradeOrderId`
11. Persist execution
12. Update order status to `Executed` or `Rejected` based on `ExecutionStatus`
13. Final `SaveChangesAsync`
14. Return `MapToResponse(order)`

The test suite uses a hand-written `FakeExecutionService` (file-scoped class, always returns Success) to keep `TradeOrderServiceTests` fully deterministic. Each test allocates a fresh `InMemoryDatabase` keyed on `Guid.NewGuid()` to prevent state bleed between tests.

## Assumption documented

For **BUY** orders, StopLoss must be **strictly below** EntryPrice and TakeProfit must be **strictly above** EntryPrice (otherwise the order has no profit target or the stop-loss would trigger immediately). For **SELL** orders, StopLoss must be **strictly above** EntryPrice and TakeProfit must be **strictly below** EntryPrice. This is the standard directional SL/TP rule for retail forex/CFD trading. Guard conditions:

- BUY: `StopLoss >= EntryPrice || TakeProfit <= EntryPrice` → throw
- SELL: `StopLoss <= EntryPrice || TakeProfit >= EntryPrice` → throw

## Build and test results

### `dotnet build Boyka.slnx`

```
Build succeeded with 12 warning(s) in 4.8s
```

All 5 projects compiled. 12 warnings are pre-existing NU1902/NU1903 vulnerability notices for the transitive package `System.Security.Cryptography.Pkcs 9.0.4` (brought in by EF Core tooling). Zero new warnings were introduced by this checkpoint.

### `dotnet test Boyka.slnx --no-build`

```
Test summary: total: 10, failed: 0, succeeded: 10, skipped: 0, duration: 2.9s
Build succeeded in 3.1s
```

Tests passing:
- `ExecuteTradeAsync_AccountNotFound_ThrowsKeyNotFoundException`
- `ExecuteTradeAsync_AccountInactive_ThrowsInvalidOperationException`
- `ExecuteTradeAsync_VolumeZero_ThrowsInvalidOperationException`
- `ExecuteTradeAsync_VolumeNegative_ThrowsInvalidOperationException`
- `ExecuteTradeAsync_BuyInvalidStopLoss_ThrowsInvalidOperationException`
- `ExecuteTradeAsync_SellInvalidStopLoss_ThrowsInvalidOperationException`
- `ExecuteAsync_MT5TicketNumberIsNull`
- `ExecuteAsync_ExecutionPriceEqualsEntryPrice`
- `ExecuteAsync_ExecutedVolumeEqualsOrderVolume`
- `ExecuteAsync_StatusIsSuccessOrFailed`

## Outstanding issues

None.

## Next checkpoint

1.6 — Completion and documentation

---

STOPPED — waiting for your approval before starting the next checkpoint.
