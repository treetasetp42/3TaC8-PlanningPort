using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3TaC8_PlanningPort.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBalanceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalDeposited = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalWithdrawn = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalRealizedProfit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashWallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashWallets_UserId",
                table: "CashWallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashWallets");
        }
    }
}
