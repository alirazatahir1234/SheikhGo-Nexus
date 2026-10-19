using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheikhGoNexusERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSweetsBarcodePhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "inventory",
                table: "Products",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SellByWeight",
                schema: "inventory",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                schema: "pos",
                table: "PosSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "pos",
                table: "PosSales",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SaleReturnId",
                schema: "pos",
                table: "PosSales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Barcode",
                schema: "inventory",
                table: "Products",
                columns: new[] { "TenantId", "Barcode" },
                unique: true,
                filter: "[Barcode] IS NOT NULL AND [Barcode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_TenantId_IdempotencyKey",
                schema: "pos",
                table: "PosSales",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL AND [IdempotencyKey] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Barcode",
                schema: "inventory",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PosSales_TenantId_IdempotencyKey",
                schema: "pos",
                table: "PosSales");

            migrationBuilder.DropColumn(
                name: "Barcode",
                schema: "inventory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SellByWeight",
                schema: "inventory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Discount",
                schema: "pos",
                table: "PosSales");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "pos",
                table: "PosSales");

            migrationBuilder.DropColumn(
                name: "SaleReturnId",
                schema: "pos",
                table: "PosSales");
        }
    }
}
