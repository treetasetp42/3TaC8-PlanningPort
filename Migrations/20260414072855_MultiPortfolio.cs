using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3TaC8_PlanningPort.Migrations
{
    /// <inheritdoc />
    public partial class MultiPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashWallets_Users_UserId",
                table: "CashWallets");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Transactions",
                newName: "PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                newName: "IX_Transactions_PortfolioId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CashWallets",
                newName: "PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_CashWallets_UserId",
                table: "CashWallets",
                newName: "IX_CashWallets_PortfolioId");

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ColorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portfolios_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // --- Custom Data Migration Logic ---
            // Create a default Portfolio for each existing User.
            // By making Portfolio.Id exactly equal to User.Id, the re-named Transactions/CashWallets (UserId -> PortfolioId) 
            // will automatically link to this new default Portfolio without needing to update every row!
            migrationBuilder.Sql(@"
                INSERT INTO Portfolios (Id, UserId, Name, Description, ColorCode, CreatedAt)
                SELECT Id, Id, 'Main Portfolio', 'Auto-generated during platform upgrade', '#6C5DD3', GETUTCDATE()
                FROM Users
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_UserId",
                table: "Portfolios",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashWallets_Portfolios_PortfolioId",
                table: "CashWallets",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Portfolios_PortfolioId",
                table: "Transactions",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashWallets_Portfolios_PortfolioId",
                table: "CashWallets");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Portfolios_PortfolioId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.RenameColumn(
                name: "PortfolioId",
                table: "Transactions",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_PortfolioId",
                table: "Transactions",
                newName: "IX_Transactions_UserId");

            migrationBuilder.RenameColumn(
                name: "PortfolioId",
                table: "CashWallets",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CashWallets_PortfolioId",
                table: "CashWallets",
                newName: "IX_CashWallets_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashWallets_Users_UserId",
                table: "CashWallets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
