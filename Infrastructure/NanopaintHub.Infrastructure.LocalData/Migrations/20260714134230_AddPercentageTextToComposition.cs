using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class AddPercentageTextToComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyEdited",
                table: "SupplierProductCompositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PercentageText",
                table: "SupplierProductCompositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalPercentageText",
                table: "SupplierProductCompositions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManuallyEdited",
                table: "SupplierProductCompositions");

            migrationBuilder.DropColumn(
                name: "PercentageText",
                table: "SupplierProductCompositions");

            migrationBuilder.DropColumn(
                name: "OriginalPercentageText",
                table: "SupplierProductCompositions");
        }
    }
}
