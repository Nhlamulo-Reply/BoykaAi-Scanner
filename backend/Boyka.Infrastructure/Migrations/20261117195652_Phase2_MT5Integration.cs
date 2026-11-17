using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boyka.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_MT5Integration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoTradingEnabled",
                table: "TradingAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MT5CommandId",
                table: "TradeOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EaRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradingAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EaVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaRegistrations_TradingAccounts_TradingAccountId",
                        column: x => x.TradingAccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MT5Commands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradeOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradingAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    StopLoss = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TakeProfit = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MT5Commands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MT5Commands_TradeOrders_TradeOrderId",
                        column: x => x.TradeOrderId,
                        principalTable: "TradeOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MT5Commands_TradingAccounts_TradingAccountId",
                        column: x => x.TradingAccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SymbolMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradingAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanonicalSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BrokerSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SymbolMappings_TradingAccounts_TradingAccountId",
                        column: x => x.TradingAccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "TradingAccounts",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.CreateIndex(
                name: "IX_EaRegistrations_TradingAccountId",
                table: "EaRegistrations",
                column: "TradingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MT5Commands_TradeOrderId",
                table: "MT5Commands",
                column: "TradeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MT5Commands_TradingAccountId",
                table: "MT5Commands",
                column: "TradingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SymbolMappings_TradingAccountId_CanonicalSymbol",
                table: "SymbolMappings",
                columns: new[] { "TradingAccountId", "CanonicalSymbol" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EaRegistrations");

            migrationBuilder.DropTable(
                name: "MT5Commands");

            migrationBuilder.DropTable(
                name: "SymbolMappings");

            migrationBuilder.DropColumn(
                name: "AutoTradingEnabled",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "MT5CommandId",
                table: "TradeOrders");
        }
    }
}
