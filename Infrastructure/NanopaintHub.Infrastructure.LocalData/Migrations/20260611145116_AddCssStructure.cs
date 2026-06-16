using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class AddCssStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CssStructure",
                table: "DocumentTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CssStructure",
                table: "DocumentTemplates");
        }
    }
}
