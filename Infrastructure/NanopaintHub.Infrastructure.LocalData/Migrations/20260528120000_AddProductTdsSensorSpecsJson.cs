using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations;

public partial class AddProductTdsSensorSpecsJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TdsSensorSpecsJson",
            table: "Products",
            type: "TEXT",
            nullable: false,
            defaultValue: "{}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TdsSensorSpecsJson",
            table: "Products");
    }
}
