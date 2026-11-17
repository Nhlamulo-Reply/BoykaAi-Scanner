


BOYKA AI -- MVP DEVELOPMENT
Phase 1: Backend Foundation
Master implementation specification for Kiro / GitHub Copilot
1. Role and working instructions
Act as a senior C# software architect and ASP.NET Core developer.

You are working in an existing Boyka AI repository. Inspect the current
solution, project files, installed packages, and existing code before
making changes. Preserve working code and avoid replacing files
unnecessarily.

Implement this specification incrementally using the checkpoints in
Section 10.

Approval gate is mandatory: Complete only the current checkpoint.
After reporting its result, stop and wait for the developer's explicit
approval before starting the next checkpoint. Do not interpret silence,
a successful build, or a request to fix the current checkpoint as
approval to move forward.

Do not generate all phases/checkpoints in one pass.

2. Project objective
Build the backend foundation for Boyka AI, a simplified mobile forex
trading application that will eventually connect to MetaTrader 5.

The application will eventually allow users to:

Connect MT5 trading accounts.
View account balances and equity.
Receive trading signals.
Execute BUY and SELL orders.
Monitor open positions.
View trade history.
Switch between assisted and automatic trading.
For this phase, implement only the backend foundation. Do not build
the mobile application or connect to a real broker.

3. Technology stack
C#
ASP.NET Core Web API
.NET 10 (net10.0)
Entity Framework Core 10
SQL Server Express
Swagger / OpenAPI
Dependency Injection
Before installing or changing packages, inspect the current .csproj
files and package versions. Keep Microsoft Entity Framework Core
packages on a compatible major version (10.x for this .NET 10 project).
Do not mix EF Core 11 packages into this solution.

Use a Swagger UI package version compatible with the project's .NET 10
OpenAPI dependencies. Do not install arbitrary versions or force
incompatible Microsoft.OpenApi dependencies. Keep the existing working
Swagger setup if one is already configured.

4. Architecture
Use a clean, maintainable layered architecture:

Boyka/
├── Boyka.API/
├── Boyka.Application/
├── Boyka.Domain/
└── Boyka.Infrastructure/
Responsibilities:

Boyka.API
Controllers
Middleware
API configuration
Dependency injection registration
Swagger configuration
Boyka.Application
Services
Interfaces
DTOs
Business logic / application orchestration
Boyka.Domain
Entities
Enums
Domain rules
Boyka.Infrastructure
Entity Framework Core
Database context
Repository implementations, if needed
Database configurations
Seed data configuration, if appropriate
Respect dependency direction and existing project references. Do not
introduce microservices or unnecessary abstractions.

5. Database entities
Create and persist these entities.

TradingAccount
Id
AccountName
BrokerName
AccountNumber (string)
ServerName
AccountCurrency
IsDemo
ExecutionMode
IsActive
CreatedAt
TradeSignal
Id
Symbol
Direction (BUY / SELL)
EntryPrice
StopLoss
TakeProfit
Status
CreatedAt
TradeOrder
Id
TradingAccountId
TradeSignalId (nullable)
Symbol
Direction
Volume
EntryPrice
StopLoss
TakeProfit
Status
CreatedAt
TradeExecution
Id
TradeOrderId
MT5TicketNumber
ExecutionPrice
ExecutedVolume
ExecutionStatus
ErrorMessage
ExecutedAt
RiskProfile
Id
TradingAccountId
RiskPercentage
MaximumLotSize
MaximumDailyLoss
MaximumOpenPositions
IsEnabled
Use Guid identifiers unless the existing repository has a documented,
justified convention. Use decimal for prices, volume, percentages, and
monetary values. Use UTC timestamps. Account numbers must remain strings
so leading zeros are preserved.

Use enums for direction, execution mode, order status, and execution
status. Do not expose persistence entities directly from API endpoints.

6. API endpoints
Implement these endpoints:

Method Route Purpose

GET /api/accounts List trading accounts

POST /api/accounts Create a trading
account

GET /api/accounts/{id} Get an account

GET /api/signals List trading signals

POST /api/signals Create a trading signal

GET /api/trades List trade orders

POST /api/trades/execute Submit an order to the
mock execution workflow

GET /api/trades/{id} Get a trade order

GET /api/trades/{id}/execution Get execution records
for an order

GET /api/risk/{accountId} Get account risk
profile

PUT /api/risk/{accountId} Create or update
account risk profile
Use request and response DTOs. Validate input and return appropriate
HTTP status codes and useful error responses. Keep controller actions
thin and put application logic in services.

7. Important business rules
Validate all incoming requests.
A trade must belong to a registered trading account.
Only active accounts can receive trade orders.
Validate BUY and SELL directions.
Validate volume and stop-loss/take-profit inputs.
Each order must have a unique identifier.
Store execution status separately from order status.
Use asynchronous database operations.
Implement centralized exception handling.
Use dependency injection.
Do not expose broker passwords or sensitive credentials through API
responses.
Do not store broker passwords or investor passwords in the
TradingAccount entity.
A failed mock execution must not be represented as a successful
execution.
Do not claim an order was executed by a broker: this phase uses a
mock service only.
Where a business rule is ambiguous (for example, exact BUY/SELL
stop-loss and take-profit price validation), make the smallest safe
assumption, document it, and do not invent advanced trading rules.

8. Mock execution restriction
For this phase, trade execution must use a mock service.

Do not connect to a real broker.
Do not use the MetaTrader 5 Python package.
Do not create or implement an MQL5 Expert Advisor.
Do not send real BUY or SELL orders.
Clearly label mock execution in code, API documentation, and README.
The mock service should create an order and a corresponding
execution record with a clear mock result. It must not imply a real
MT5 ticket exists.
9. Development requirements
Use interfaces for application services.
Use DTOs instead of exposing database entities directly.
Configure Entity Framework Core relationships.
Use EF Core migrations.
Add Swagger UI / OpenAPI documentation.
Include sample seed data for one demo account.
Include clear local setup instructions in README.md.
Use asynchronous database operations.
Keep configuration and connection strings out of source-controlled
secrets where practical. Provide a safe local-development example
using user secrets or configuration placeholders.
Ensure the project builds successfully.
Add focused tests for important validation and mock execution
behaviour where practical within the current checkpoint.
10. Checkpoint plan and approval gates
All checkpoints below belong to Phase 1: Backend Foundation. They
are not separate product phases.

Checkpoint 1.1 --- Domain model
Inspect current solution and Boyka.Domain.
Create the five domain entities and relevant enums.
Add sensible defaults, nullability, and navigation properties.
Keep the Domain project free of EF Core dependencies and persistence
attributes.
Build the solution.
Report files created/changed and build result.
STOP for approval.
Checkpoint 1.2 --- Persistence and database
Only after explicit approval of 1.1: - Configure EF Core 10 and SQL
Server Express. - Add BoykaDbContext in Infrastructure. - Configure
entity relationships and constraints. - Configure decimal precision for
prices, volume, and risk values. - Add migration support and create the
initial migration. - Add one demo account seed record, without
credentials. - Provide database creation/update commands. - Build and
test. - STOP for approval.

Checkpoint 1.3 --- Application layer
Only after explicit approval of 1.2: - Create service interfaces, DTOs,
and application services for accounts, signals, trades, and risk
profiles. - Add validation for request DTOs. - Keep application logic
out of controllers. - Build and test. - STOP for approval.

Checkpoint 1.4 --- API layer
Only after explicit approval of 1.3: - Implement the required
controllers and routes. - Wire services through dependency injection. -
Add centralized exception handling and consistent error responses. -
Configure Swagger UI / OpenAPI without breaking the existing package
setup. - Build and test endpoints. - STOP for approval.

Checkpoint 1.5 --- Mock trade execution and risk validation
Only after explicit approval of 1.4: - Implement mock execution
service. - Validate account existence and active status. - Validate
order direction, volume, and SL/TP inputs. - Enforce configured
risk-profile limits. - Persist order and execution statuses
separately. - Ensure mock outcomes are clearly identified as
simulated. - Add focused tests. - Build and test. - STOP for
approval.

Checkpoint 1.6 --- Completion and documentation
Only after explicit approval of 1.5: - Verify all required endpoints. -
Add or complete one demo account seed record. - Write README.md with
prerequisites, local configuration, database setup, migrations, run
commands, Swagger URL discovery, endpoint list, and sample
requests/responses. - Run a clean restore, build, and relevant tests. -
Report final project structure, database schema, migration instructions,
API documentation, local run instructions, build and test results, and
known limitations. - STOP. Phase 1 is complete. Wait for explicit
approval before any later product phase.

11. Restrictions
Do not implement the mobile application yet.
Do not implement Python or LangGraph yet.
Do not implement MQL5 or MT5 connectivity yet.
Do not implement real trading execution.
Do not introduce unnecessary microservices.
Do not proceed beyond the currently approved checkpoint.
Do not silently change the technology stack or target framework.
Do not delete or overwrite existing working project files without
explaining why.
12. Completion reporting format
At the end of each checkpoint, report:

Checkpoint completed: number and name.
Files created or modified: list with short purpose.
Implementation summary: concise explanation of what changed.
Commands run: exact build/test/migration commands actually run.
Results: actual success/failure and warnings; never invent
successful results.
Outstanding issues: anything unresolved.
Next checkpoint: name only, without implementing it.
Then explicitly state:

"STOPPED --- waiting for your approval before starting the next
checkpoint."

First instruction to the coding assistant
Start with Checkpoint 1.1 --- Domain model only.

Inspect the existing repository first. Do not implement Checkpoint 1.2
or any later checkpoint. Complete 1.1, build the solution, report the
actual result, and wait for explicit approval.




