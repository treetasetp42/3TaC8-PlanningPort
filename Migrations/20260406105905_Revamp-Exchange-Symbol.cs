using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3TaC8_PlanningPort.Migrations
{
    /// <inheritdoc />
    public partial class RevampExchangeSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StockCaches",
                table: "StockCaches");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Watchlists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "UserLogs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "StockCaches",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "StockCaches",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockCaches",
                table: "StockCaches",
                columns: new[] { "Symbol", "Exchange" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StockCaches",
                table: "StockCaches");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Watchlists");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "StockCaches");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "UserLogs",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "StockCaches",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockCaches",
                table: "StockCaches",
                column: "Symbol");
        }
    }
}
