using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSuppliersAndCompositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "EchaSubstanceId",
                table: "ProductCompositions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierProductId",
                table: "ProductCompositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierProducts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierProductCompositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EchaSubstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Percentage = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProductCompositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierProductCompositions_EchaSubstances_EchaSubstanceId",
                        column: x => x.EchaSubstanceId,
                        principalTable: "EchaSubstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProductCompositions_SupplierProducts_SupplierProductId",
                        column: x => x.SupplierProductId,
                        principalTable: "SupplierProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCompositions_SupplierProductId",
                table: "ProductCompositions",
                column: "SupplierProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductCompositions_EchaSubstanceId",
                table: "SupplierProductCompositions",
                column: "EchaSubstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductCompositions_SupplierProductId",
                table: "SupplierProductCompositions",
                column: "SupplierProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_SupplierId",
                table: "SupplierProducts",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCompositions_SupplierProducts_SupplierProductId",
                table: "ProductCompositions",
                column: "SupplierProductId",
                principalTable: "SupplierProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCompositions_SupplierProducts_SupplierProductId",
                table: "ProductCompositions");

            migrationBuilder.DropTable(
                name: "SupplierProductCompositions");

            migrationBuilder.DropTable(
                name: "SupplierProducts");

            migrationBuilder.DropIndex(
                name: "IX_ProductCompositions_SupplierProductId",
                table: "ProductCompositions");

            migrationBuilder.DropColumn(
                name: "SupplierProductId",
                table: "ProductCompositions");

            migrationBuilder.AlterColumn<Guid>(
                name: "EchaSubstanceId",
                table: "ProductCompositions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
