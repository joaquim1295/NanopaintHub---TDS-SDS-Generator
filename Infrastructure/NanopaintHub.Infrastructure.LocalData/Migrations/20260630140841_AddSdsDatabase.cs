using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanopaintHub.Infrastructure.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class AddSdsDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClpPhrases",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PhraseType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClpPhrases", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "EchaSubstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChemicalName = table.Column<string>(type: "TEXT", nullable: false),
                    CasNumber = table.Column<string>(type: "TEXT", nullable: false),
                    EcNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EchaSubstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCompositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EchaSubstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Percentage = table.Column<double>(type: "REAL", nullable: false),
                    ExtraMetadataJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCompositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCompositions_EchaSubstances_EchaSubstanceId",
                        column: x => x.EchaSubstanceId,
                        principalTable: "EchaSubstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductCompositions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubstanceHazards",
                columns: table => new
                {
                    SubstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PhraseCode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstanceHazards", x => new { x.SubstanceId, x.PhraseCode });
                    table.ForeignKey(
                        name: "FK_SubstanceHazards_ClpPhrases_PhraseCode",
                        column: x => x.PhraseCode,
                        principalTable: "ClpPhrases",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubstanceHazards_EchaSubstances_SubstanceId",
                        column: x => x.SubstanceId,
                        principalTable: "EchaSubstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EchaSubstances_CasNumber",
                table: "EchaSubstances",
                column: "CasNumber");

            migrationBuilder.CreateIndex(
                name: "IX_EchaSubstances_ChemicalName",
                table: "EchaSubstances",
                column: "ChemicalName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCompositions_EchaSubstanceId",
                table: "ProductCompositions",
                column: "EchaSubstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCompositions_ProductId",
                table: "ProductCompositions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SubstanceHazards_PhraseCode",
                table: "SubstanceHazards",
                column: "PhraseCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCompositions");

            migrationBuilder.DropTable(
                name: "SubstanceHazards");

            migrationBuilder.DropTable(
                name: "ClpPhrases");

            migrationBuilder.DropTable(
                name: "EchaSubstances");
        }
    }
}
