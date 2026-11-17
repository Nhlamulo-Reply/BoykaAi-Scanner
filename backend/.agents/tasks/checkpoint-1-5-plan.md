# Implementation Plan — Checkpoint 1.5: Mock Trade Execution and Risk Validation

## Context from exploration

- **Solution**: `Boyka.slnx` at `c:\Users\OEM\Documents\BoykaAI\backend\`
- **Target framework**: `net10.0` across all projects
- **EF Core version**: `10.0.0` (`Microsoft.EntityFrameworkCore.SqlServer`)
- **`IBoykaDbContext`**: defined in `Boyka.Application/Interfaces/IBoykaDbContext.cs`; exposes `DbSet<T>` for all five entities plus `SaveChangesAsync(CancellationToken)`.
- **`TradeOrderService`**: currently creates an order with `Pending` status and saves — `ExecuteTradeAsync` needs full replacement.
- **`ServiceCollectionExtensions`**: registers four services; needs one addition for `IMockTradeExecutionService`.
- **`Boyka.Tests`**: does not exist — must be created as an xUnit project, added to `Boyka.slnx`, and given package + project references.
- **`Boyka.Infrastructure/Services/`**: directory exists but is empty; `MockTradeExecutionService` is the first file placed there.

## Design decisions

1. **Interface placement**: `IMockTradeExecutionService` goes in `Boyka.Application/Interfaces/` because all service interfaces live there. The concrete `MockTradeExecutionService` goes in `Boyka.Infrastructure/Services/` to match the existing pattern for implementations.

2. **`Random` instance scope**: use `private static readonly Random _rng = new()` as a class-level field in `MockTradeExecutionService`. A new instance per call risks correlated seeds on rapid successive calls; a static instance avoids that without introducing thread-safety risk in a non-concurrent mock context.

3. **Risk profile query**: `_context.RiskProfiles.FirstOrDefaultAsync(r => r.TradingAccountId == request.TradingAccountId && r.IsEnabled)`. The spec says "if enabled risk profile exists" — if multiple enabled profiles exist, the first found applies. This is documented as an assumption.

4. **Open positions count**: `_context.TradeOrders.CountAsync(o => o.TradingAccountId == request.TradingAccountId && (o.Status == TradeOrderStatus.Pending || o.Status == TradeOrderStatus.Accepted))`. Compare `>= MaximumOpenPositions` before adding the new order.

5. **Test isolation**: use `Microsoft.EntityFrameworkCore.InMemory` to create a real `BoykaDbContext` with a unique in-memory database name per test. For `IMockTradeExecutionService` in `TradeOrderServiceTests`, use a hand-written fake — simpler and no extra package dependency. Moq is included per spec but not required.

6. **`Boyka.slnx` format**: the existing file uses the XML format (`<Solution>` / `<Project Path="..."/>`). The new test project is added with the same format.

---

## Steps

- [ ] 1. Create `IMockTradeExecutionService` interface.
      Single method `Task<TradeExecution> ExecuteAsync(TradeOrder order)` in namespace `Boyka.Application.Interfaces`, with `using Boyka.Domain.Entities;`.
      Files: `Boyka.Application/Interfaces/IMockTradeExecutionService.cs`
      Verify: `dotnet build c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — 0 errors.

- [ ] 2. Create `MockTradeExecutionService` in Infrastructure.
      Implements `IMockTradeExecutionService` in namespace `Boyka.Infrastructure.Services`. XML doc comment: "Mock trade execution service. Does not connect to any real broker or MT5 instance."
      Class-level `private static readonly Random _rng = new()`. In `ExecuteAsync`: build a `TradeExecution` with `TradeOrderId = order.Id`, `MT5TicketNumber = null`, `ExecutionPrice = order.EntryPrice`, `ExecutedVolume = order.Volume`, `ExecutedAt = DateTime.UtcNow`. Set `ExecutionStatus = Success` if `_rng.NextDouble() < 0.9`; else `Failed` with `ErrorMessage = "[MOCK] Simulated execution failure."`. Return entity without persisting.
      Files: `Boyka.Infrastructure/Services/MockTradeExecutionService.cs`
      Verify: `dotnet build c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — 0 errors.

- [ ] 3. Replace `ExecuteTradeAsync` in `TradeOrderService`; add `IMockTradeExecutionService` constructor parameter.
      Depends on steps 1 and 2.
      Add `private readonly IMockTradeExecutionService _mockExecutionService` and inject via constructor.
      Replace `ExecuteTradeAsync` body with the full execution path in this order:
        a. Load account via `FirstOrDefaultAsync`; throw `KeyNotFoundException($"Trading account {request.TradingAccountId} not found.")` if null.
        b. `InvalidOperationException($"Trading account {account.Id} is not active.")` if `!account.IsActive`.
        c. `InvalidOperationException("Invalid trade direction.")` if direction is neither `Buy` nor `Sell`.
        d. `InvalidOperationException("Volume must be greater than zero.")` if `request.Volume <= 0`.
        e. `InvalidOperationException("Volume exceeds the maximum allowed per order.")` if `request.Volume > 100`.
        f. `InvalidOperationException` if `request.EntryPrice <= 0`, `request.StopLoss <= 0`, or `request.TakeProfit <= 0` (DTO already enforces via annotations, service layer adds defence in depth).
        g. BUY: throw `InvalidOperationException("Invalid stop-loss or take-profit for the given direction.")` if `StopLoss >= EntryPrice || TakeProfit <= EntryPrice`.
        h. SELL: same message if `StopLoss <= EntryPrice || TakeProfit >= EntryPrice`.
        i. Load first enabled `RiskProfile`; if found: throw if `Volume > MaximumLotSize`; count Pending/Accepted orders; throw if count `>= MaximumOpenPositions`.
        j. Create `TradeOrder` with `Status = Accepted`, `Id = Guid.NewGuid()`, `CreatedAt = DateTime.UtcNow`; add and `SaveChangesAsync`.
        k. Await `_mockExecutionService.ExecuteAsync(order)`; set `execution.TradeOrderId = order.Id`; add to context; `SaveChangesAsync`.
        l. Set `order.Status = Executed` if `execution.ExecutionStatus == Success`, else `Rejected`; `SaveChangesAsync`.
        m. Return `MapToResponse(order)`.
      Files: `Boyka.Application/Services/TradeOrderService.cs`
      Verify: `dotnet build c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — 0 errors.

- [ ] 4. Register `IMockTradeExecutionService` in the DI container.
      Add `using Boyka.Infrastructure.Services;` and `using Boyka.Application.Interfaces;` (already present — confirm). Add `services.AddScoped<IMockTradeExecutionService, MockTradeExecutionService>();` after the existing four registrations.
      Files: `Boyka.API/Extensions/ServiceCollectionExtensions.cs`
      Verify: `dotnet build c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — 0 errors.

- [ ] 5. Create the `Boyka.Tests` xUnit project and wire it into the solution.
      Run `dotnet new xunit -n Boyka.Tests -f net10.0 --output c:\Users\OEM\Documents\BoykaAI\backend\Boyka.Tests`.
      Edit the generated `Boyka.Tests.csproj`:
        - Add `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />`
        - Add `<PackageReference Include="Moq" Version="4.20.72" />`
        - Confirm `coverlet.collector` is already present (xunit template includes it).
        - Add project references to `Boyka.Domain`, `Boyka.Application`, `Boyka.Infrastructure`.
      Add `<Project Path="Boyka.Tests/Boyka.Tests.csproj" />` to `Boyka.slnx`.
      Delete the template-generated `UnitTest1.cs`.
      Files: `Boyka.Tests/Boyka.Tests.csproj`, `Boyka.slnx`
      Verify: `dotnet build c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — all 5 projects build, 0 errors.

- [ ] 6. Write `MockTradeExecutionServiceTests`.
      Depends on steps 2 and 5.
      Four `[Fact]` tests in `Boyka.Tests/MockTradeExecutionServiceTests.cs` (namespace `Boyka.Tests`):
        - `ExecuteAsync_ReturnsMT5TicketNumber_AsNull`: build minimal `TradeOrder`, call `ExecuteAsync`, assert `result.MT5TicketNumber == null`.
        - `ExecuteAsync_ReturnsExecutionPrice_EqualToOrderEntryPrice`: assert `result.ExecutionPrice == order.EntryPrice`.
        - `ExecuteAsync_ReturnsExecutedVolume_EqualToOrderVolume`: assert `result.ExecutedVolume == order.Volume`.
        - `ExecuteAsync_ReturnsStatus_SuccessOrFailed`: assert `result.ExecutionStatus == TradeExecutionStatus.Success || result.ExecutionStatus == TradeExecutionStatus.Failed`.
      Each test instantiates `MockTradeExecutionService` directly — no DB needed.
      Files: `Boyka.Tests/MockTradeExecutionServiceTests.cs`
      Verify: `dotnet test c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — 4 tests pass, 0 failures.

- [ ] 7. Write `TradeOrderServiceTests`.
      Depends on steps 3, 5, and 6.
      Helper: `FakeExecutionService` (private nested class or file-scope class) implementing `IMockTradeExecutionService`, always returns `ExecutionStatus = Success`.
      Helper factory: `CreateContext(string dbName)` returns a `BoykaDbContext` using `options.UseInMemoryDatabase(dbName)`.
      Six `[Fact]` tests in `Boyka.Tests/TradeOrderServiceTests.cs`:
        - `ExecuteTradeAsync_AccountNotFound_ThrowsKeyNotFoundException`: no account seeded; assert `await Assert.ThrowsAsync<KeyNotFoundException>(...)`.
        - `ExecuteTradeAsync_AccountInactive_ThrowsInvalidOperationException`: seed inactive account; assert `InvalidOperationException`.
        - `ExecuteTradeAsync_VolumeZero_ThrowsInvalidOperationException`: seed active account; request with `Volume = 0`; assert `InvalidOperationException`.
        - `ExecuteTradeAsync_VolumeNegative_ThrowsInvalidOperationException`: request with `Volume = -1`.
        - `ExecuteTradeAsync_BuyInvalidStopLoss_ThrowsInvalidOperationException`: BUY with `StopLoss >= EntryPrice` (e.g. SL=1.1, EP=1.0).
        - `ExecuteTradeAsync_SellInvalidStopLoss_ThrowsInvalidOperationException`: SELL with `StopLoss <= EntryPrice` (e.g. SL=0.9, EP=1.0).
      Each test uses a fresh `Guid.NewGuid().ToString()` database name. Pass the in-memory context (which implements `IBoykaDbContext`) and `FakeExecutionService` to `TradeOrderService` constructor.
      Files: `Boyka.Tests/TradeOrderServiceTests.cs`
      Verify: `dotnet test c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` — all 10 tests (4 + 6) pass, 0 failures.

- [ ] 8. Full build and test run; fix all errors.
      Run `dotnet build c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx` then `dotnet test c:\Users\OEM\Documents\BoykaAI\backend\Boyka.slnx --no-build`. Fix any compiler errors or test failures before moving on.
      Files: any files producing errors
      Verify: both commands exit with code 0, 0 test failures.

- [ ] 9. Write the checkpoint report.
      Create `c:\Users\OEM\Documents\BoykaAI\backend\.agents\tasks\checkpoint-1-5-report.md` containing:
        - Heading: "Checkpoint completed: 1.5 — Mock trade execution and risk validation"
        - Files created or modified with one-line purpose for each
        - Implementation summary (what was built and how it fits the architecture)
        - Assumption documented: BUY/SELL SL-TP price validation rule (exact guards and rationale)
        - Build and test results (exact console output)
        - Outstanding issues (or "none")
        - Next checkpoint: "1.6 — Completion and documentation"
        - Final line: "STOPPED — waiting for your approval before starting the next checkpoint."
      Files: `c:\Users\OEM\Documents\BoykaAI\backend\.agents\tasks\checkpoint-1-5-report.md`
      Verify: file exists and is readable.
