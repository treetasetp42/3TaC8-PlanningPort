using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3TaC8_PlanningPort.Migrations
{
    /// <inheritdoc />
    public partial class ExchangeforTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Transactions");
        }
    }
}
