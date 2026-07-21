using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class AddSdsComplianceEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HazardProfileHash",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SdsOverrideJson",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ClpPhraseTranslations",
                columns: table => new
                {
                    ClpPhraseCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    SectionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TranslatedText = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClpPhraseTranslations", x => new { x.ClpPhraseCode, x.LanguageCode, x.SectionType });
                    table.ForeignKey(
                        name: "FK_ClpPhraseTranslations_ClpPhrases_ClpPhraseCode",
                        column: x => x.ClpPhraseCode,
                        principalTable: "ClpPhrases",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClpPhraseTranslations");

            migrationBuilder.DropColumn(
                name: "HazardProfileHash",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SdsOverrideJson",
                table: "Products");
        }
    }
}
