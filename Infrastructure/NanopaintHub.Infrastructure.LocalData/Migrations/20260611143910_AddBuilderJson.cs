using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class AddBuilderJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuilderJson",
                table: "DocumentTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuilderJson",
                table: "DocumentTemplates");
        }
    }
}
