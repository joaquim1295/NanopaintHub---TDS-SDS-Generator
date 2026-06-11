using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class AddTestBatchAndSupplierComponentForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TestBatches_ProductId",
                table: "TestBatches",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierComponents_SupplierId",
                table: "SupplierComponents",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierComponents_Suppliers_SupplierId",
                table: "SupplierComponents",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TestBatches_Products_ProductId",
                table: "TestBatches",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierComponents_Suppliers_SupplierId",
                table: "SupplierComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_TestBatches_Products_ProductId",
                table: "TestBatches");

            migrationBuilder.DropIndex(
                name: "IX_TestBatches_ProductId",
                table: "TestBatches");

            migrationBuilder.DropIndex(
                name: "IX_SupplierComponents_SupplierId",
                table: "SupplierComponents");
        }
    }
}
