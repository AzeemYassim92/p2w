using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace P2W.Cards.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MappingNotes",
                table: "ExternalProductMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MappingStatus",
                table: "ExternalProductMappings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "AutoMatched");

            migrationBuilder.AlterColumn<string>(
                name: "CardNumber",
                table: "CatalogProducts",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "CatalogProducts",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "CardSets",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CatalogImportCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CheckpointValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImportCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogPriceReferenceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MarketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LowPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MidPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HighPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UngradedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Grade7Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Grade8Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Grade9Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Grade10Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BuylistPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RetailPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogPriceReferenceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogPriceReferenceSnapshots_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogPriceReferenceSnapshots_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000001"),
                column: "NormalizedName",
                value: "final fantasy");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000002"),
                column: "NormalizedName",
                value: "tarkir dragonstorm");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000003"),
                column: "NormalizedName",
                value: "mtg final fantasy");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000004"),
                column: "NormalizedName",
                value: "marvel super heroes");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000005"),
                column: "NormalizedName",
                value: "prismatic evolutions");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000006"),
                column: "NormalizedName",
                value: "destined rivals");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000007"),
                column: "NormalizedName",
                value: "mega evolution");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000008"),
                column: "NormalizedName",
                value: "chaos rising");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000009"),
                column: "NormalizedName",
                value: "ascended heroes");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000010"),
                column: "NormalizedName",
                value: "romance dawn");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000011"),
                column: "NormalizedName",
                value: "paramount war");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000012"),
                column: "NormalizedName",
                value: "pillars of strength");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000013"),
                column: "NormalizedName",
                value: "kingdoms of intrigue");

            migrationBuilder.UpdateData(
                table: "CardSets",
                keyColumn: "Id",
                keyValue: new Guid("53000000-0000-0000-0000-000000000014"),
                column: "NormalizedName",
                value: "500 years in the future");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000001"),
                column: "NormalizedName",
                value: "black lotus");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000002"),
                column: "NormalizedName",
                value: "sol ring");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000003"),
                column: "NormalizedName",
                value: "lightning bolt");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000004"),
                column: "NormalizedName",
                value: "counterspell");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000005"),
                column: "NormalizedName",
                value: "mox diamond");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000006"),
                column: "NormalizedName",
                value: "mana crypt");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000007"),
                column: "NormalizedName",
                value: "rhystic study");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000008"),
                column: "NormalizedName",
                value: "dockside extortionist");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000009"),
                column: "NormalizedName",
                value: "final fantasy booster pack");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000010"),
                column: "NormalizedName",
                value: "final fantasy booster box");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000011"),
                column: "NormalizedName",
                value: "commander deck sample");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000012"),
                column: "NormalizedName",
                value: "charizard");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000013"),
                column: "NormalizedName",
                value: "pikachu");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000014"),
                column: "NormalizedName",
                value: "blastoise");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000015"),
                column: "NormalizedName",
                value: "mewtwo");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000016"),
                column: "NormalizedName",
                value: "gengar");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000017"),
                column: "NormalizedName",
                value: "rayquaza");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000018"),
                column: "NormalizedName",
                value: "lugia");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000019"),
                column: "NormalizedName",
                value: "umbreon");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000020"),
                column: "NormalizedName",
                value: "prismatic evolutions booster pack");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000021"),
                column: "NormalizedName",
                value: "prismatic evolutions elite trainer box");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000022"),
                column: "NormalizedName",
                value: "destined rivals booster pack");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000023"),
                column: "NormalizedName",
                value: "chaos rising elite trainer box");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000024"),
                column: "NormalizedName",
                value: "monkey d luffy sample card");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000025"),
                column: "NormalizedName",
                value: "roronoa zoro sample card");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000026"),
                column: "NormalizedName",
                value: "romance dawn booster pack");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000027"),
                column: "NormalizedName",
                value: "paramount war booster box");

            migrationBuilder.UpdateData(
                table: "CatalogProducts",
                keyColumn: "Id",
                keyValue: new Guid("54000000-0000-0000-0000-000000000028"),
                column: "NormalizedName",
                value: "500 years in the future booster pack");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalProductMappings_MappingStatus",
                table: "ExternalProductMappings",
                column: "MappingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_GameId_CardSetId_CardNumber_NormalizedName",
                table: "CatalogProducts",
                columns: new[] { "GameId", "CardSetId", "CardNumber", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_NormalizedName",
                table: "CatalogProducts",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_CardSets_GameId_NormalizedName",
                table: "CardSets",
                columns: new[] { "GameId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportCheckpoints_SourceName_ImportType",
                table: "CatalogImportCheckpoints",
                columns: new[] { "SourceName", "ImportType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPriceReferenceSnapshots_CapturedAtUtc",
                table: "CatalogPriceReferenceSnapshots",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPriceReferenceSnapshots_CatalogProductId",
                table: "CatalogPriceReferenceSnapshots",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPriceReferenceSnapshots_CatalogProductId_SourceName_CapturedAtUtc",
                table: "CatalogPriceReferenceSnapshots",
                columns: new[] { "CatalogProductId", "SourceName", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPriceReferenceSnapshots_ProductVariantId",
                table: "CatalogPriceReferenceSnapshots",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogPriceReferenceSnapshots_SourceName",
                table: "CatalogPriceReferenceSnapshots",
                column: "SourceName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogImportCheckpoints");

            migrationBuilder.DropTable(
                name: "CatalogPriceReferenceSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_ExternalProductMappings_MappingStatus",
                table: "ExternalProductMappings");

            migrationBuilder.DropIndex(
                name: "IX_CatalogProducts_GameId_CardSetId_CardNumber_NormalizedName",
                table: "CatalogProducts");

            migrationBuilder.DropIndex(
                name: "IX_CatalogProducts_NormalizedName",
                table: "CatalogProducts");

            migrationBuilder.DropIndex(
                name: "IX_CardSets_GameId_NormalizedName",
                table: "CardSets");

            migrationBuilder.DropColumn(
                name: "MappingNotes",
                table: "ExternalProductMappings");

            migrationBuilder.DropColumn(
                name: "MappingStatus",
                table: "ExternalProductMappings");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "CatalogProducts");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "CardSets");

            migrationBuilder.AlterColumn<string>(
                name: "CardNumber",
                table: "CatalogProducts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
