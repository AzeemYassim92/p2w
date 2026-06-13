using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace P2W.Cards.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsCreated = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImportRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimaryFocus = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CatalogImportErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImportErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogImportErrors_CatalogImportRuns_CatalogImportRunId",
                        column: x => x.CatalogImportRunId,
                        principalTable: "CatalogImportRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsUpcoming = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SymbolUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardSets_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rarity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSealed = table.Column<bool>(type: "bit", nullable: false),
                    IsSingleCard = table.Column<bool>(type: "bit", nullable: false),
                    IsGradedEligible = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsTrending = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogProducts_CardSets_CardSetId",
                        column: x => x.CardSetId,
                        principalTable: "CardSets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CatalogProducts_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogProducts_ProductCategories_ProductCategoryId",
                        column: x => x.ProductCategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalProductMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalSlug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastVerifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalProductMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalProductMappings_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFoil = table.Column<bool>(type: "bit", nullable: false),
                    IsReverseHolo = table.Column<bool>(type: "bit", nullable: false),
                    IsFirstEdition = table.Column<bool>(type: "bit", nullable: false),
                    IsPromo = table.Column<bool>(type: "bit", nullable: false),
                    IsSerialized = table.Column<bool>(type: "bit", nullable: false),
                    IsSealedCase = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellerInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    RawConditionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsGraded = table.Column<bool>(type: "bit", nullable: false),
                    GradingCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grade = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CertificationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailableForSale = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerInventoryItems_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerInventoryItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SellerInventoryImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerInventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerInventoryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerInventoryImages_SellerInventoryItems_SellerInventoryItemId",
                        column: x => x.SellerInventoryItemId,
                        principalTable: "SellerInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "CreatedUtc", "Description", "DisplayOrder", "IsActive", "IsPrimaryFocus", "Name", "Slug", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("51000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Magic: The Gathering.", 1, true, true, "Magic: The Gathering", "magic-the-gathering", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Pokemon.", 2, true, true, "Pokemon", "pokemon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for One Piece.", 3, true, true, "One Piece", "one-piece", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Yu-Gi-Oh!.", 4, true, false, "Yu-Gi-Oh!", "yu-gi-oh", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Disney Lorcana.", 5, true, false, "Disney Lorcana", "disney-lorcana", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Digimon.", 6, true, false, "Digimon", "digimon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Star Wars: Unlimited.", 7, true, false, "Star Wars: Unlimited", "star-wars-unlimited", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("51000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Catalog products for Flesh and Blood.", 8, true, false, "Flesh and Blood", "flesh-and-blood", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "CreatedUtc", "Description", "DisplayOrder", "IsActive", "Name", "ParentCategoryId", "Slug", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("52000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Singles.", 1, true, "Singles", null, "singles", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Sealed.", 2, true, "Sealed", null, "sealed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Accessories.", 12, true, "Accessories", null, "accessories", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000013"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Supplies.", 13, true, "Supplies", null, "supplies", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Bulk Lots.", 14, true, "Bulk Lots", null, "bulk-lots", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Complete Sets.", 15, true, "Complete Sets", null, "complete-sets", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Preorders.", 16, true, "Preorders", null, "preorders", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Deals.", 17, true, "Deals", null, "deals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "CardSets",
                columns: new[] { "Id", "Code", "CreatedUtc", "GameId", "IsActive", "IsUpcoming", "LogoUrl", "Name", "ReleaseDate", "Slug", "SymbolUrl", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("53000000-0000-0000-0000-000000000001"), "FIN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000001"), true, false, "https://placehold.co/320x120?text=Final%20Fantasy", "Final Fantasy", new DateTime(2025, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "final-fantasy", "https://placehold.co/64x64?text=FIN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000002"), "TDM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000001"), true, false, "https://placehold.co/320x120?text=Tarkir%3A%20Dragonstorm", "Tarkir: Dragonstorm", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "tarkir-dragonstorm", "https://placehold.co/64x64?text=TDM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000003"), "MFF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000001"), true, false, "https://placehold.co/320x120?text=Magic%3A%20The%20Gathering%20FINAL%20FANTASY", "Magic: The Gathering FINAL FANTASY", new DateTime(2025, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "magic-the-gathering-final-fantasy", "https://placehold.co/64x64?text=MFF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000004"), "MAR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000001"), true, true, "https://placehold.co/320x120?text=Marvel%20Super%20Heroes", "Marvel Super Heroes", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "marvel-super-heroes", "https://placehold.co/64x64?text=MAR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000005"), "PRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000002"), true, false, "https://placehold.co/320x120?text=Prismatic%20Evolutions", "Prismatic Evolutions", new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "prismatic-evolutions", "https://placehold.co/64x64?text=PRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000006"), "DRI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000002"), true, false, "https://placehold.co/320x120?text=Destined%20Rivals", "Destined Rivals", new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "destined-rivals", "https://placehold.co/64x64?text=DRI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000007"), "MEG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000002"), true, true, "https://placehold.co/320x120?text=Mega%20Evolution", "Mega Evolution", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "mega-evolution", "https://placehold.co/64x64?text=MEG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000008"), "CHR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000002"), true, true, "https://placehold.co/320x120?text=Chaos%20Rising", "Chaos Rising", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "chaos-rising", "https://placehold.co/64x64?text=CHR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000009"), "ASH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000002"), true, true, "https://placehold.co/320x120?text=Ascended%20Heroes", "Ascended Heroes", new DateTime(2026, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "ascended-heroes", "https://placehold.co/64x64?text=ASH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000010"), "OP01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000003"), true, false, "https://placehold.co/320x120?text=Romance%20Dawn", "Romance Dawn", new DateTime(2022, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "romance-dawn", "https://placehold.co/64x64?text=OP01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000011"), "OP02", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000003"), true, false, "https://placehold.co/320x120?text=Paramount%20War", "Paramount War", new DateTime(2023, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "paramount-war", "https://placehold.co/64x64?text=OP02", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000012"), "OP03", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000003"), true, false, "https://placehold.co/320x120?text=Pillars%20of%20Strength", "Pillars of Strength", new DateTime(2023, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "pillars-of-strength", "https://placehold.co/64x64?text=OP03", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000013"), "OP04", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000003"), true, false, "https://placehold.co/320x120?text=Kingdoms%20of%20Intrigue", "Kingdoms of Intrigue", new DateTime(2023, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "kingdoms-of-intrigue", "https://placehold.co/64x64?text=OP04", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("53000000-0000-0000-0000-000000000014"), "OP07", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("51000000-0000-0000-0000-000000000003"), true, false, "https://placehold.co/320x120?text=500%20Years%20in%20the%20Future", "500 Years in the Future", new DateTime(2024, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "500-years-in-the-future", "https://placehold.co/64x64?text=OP07", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "CreatedUtc", "Description", "DisplayOrder", "IsActive", "Name", "ParentCategoryId", "Slug", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("52000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Booster Packs.", 3, true, "Booster Packs", new Guid("52000000-0000-0000-0000-000000000002"), "booster-packs", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Booster Boxes.", 4, true, "Booster Boxes", new Guid("52000000-0000-0000-0000-000000000002"), "booster-boxes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Elite Trainer Boxes.", 5, true, "Elite Trainer Boxes", new Guid("52000000-0000-0000-0000-000000000002"), "elite-trainer-boxes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Bundles.", 6, true, "Bundles", new Guid("52000000-0000-0000-0000-000000000002"), "bundles", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Starter Decks.", 7, true, "Starter Decks", new Guid("52000000-0000-0000-0000-000000000002"), "starter-decks", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Commander Decks.", 8, true, "Commander Decks", new Guid("52000000-0000-0000-0000-000000000002"), "commander-decks", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Battle Decks.", 9, true, "Battle Decks", new Guid("52000000-0000-0000-0000-000000000002"), "battle-decks", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Collection Boxes.", 10, true, "Collection Boxes", new Guid("52000000-0000-0000-0000-000000000002"), "collection-boxes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Graded Cards.", 11, true, "Graded Cards", new Guid("52000000-0000-0000-0000-000000000001"), "graded-cards", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Raw Singles.", 18, true, "Raw Singles", new Guid("52000000-0000-0000-0000-000000000001"), "raw-singles", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000019"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Foils.", 19, true, "Foils", new Guid("52000000-0000-0000-0000-000000000001"), "foils", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Reverse Holos.", 20, true, "Reverse Holos", new Guid("52000000-0000-0000-0000-000000000001"), "reverse-holos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52000000-0000-0000-0000-000000000021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Browse Promos.", 21, true, "Promos", new Guid("52000000-0000-0000-0000-000000000001"), "promos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "CatalogProducts",
                columns: new[] { "Id", "Artist", "CardNumber", "CardSetId", "CreatedUtc", "Description", "GameId", "ImageUrl", "IsActive", "IsFeatured", "IsGradedEligible", "IsSealed", "IsSingleCard", "IsTrending", "Name", "ProductCategoryId", "ProductType", "Rarity", "ReleaseDate", "Slug", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("54000000-0000-0000-0000-000000000001"), null, "232", new Guid("53000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Black Lotus catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Black%20Lotus&format=image&version=normal", true, true, true, false, true, true, "Black Lotus", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare", new DateTime(2025, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "black-lotus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000002"), null, "276", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sol Ring catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Sol%20Ring&format=image&version=normal", true, true, true, false, true, true, "Sol Ring", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Uncommon", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "sol-ring", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000003"), null, "146", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lightning Bolt catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Lightning%20Bolt&format=image&version=normal", true, true, true, false, true, true, "Lightning Bolt", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Common", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "lightning-bolt", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000004"), null, "67", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Counterspell catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Counterspell&format=image&version=normal", true, true, true, false, true, false, "Counterspell", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Common", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "counterspell", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000005"), null, "138", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mox Diamond catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Mox%20Diamond&format=image&version=normal", true, true, true, false, true, true, "Mox Diamond", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "mox-diamond", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000006"), null, "225", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mana Crypt catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Mana%20Crypt&format=image&version=normal", true, true, true, false, true, true, "Mana Crypt", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Mythic Rare", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "mana-crypt", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000007"), null, "45", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rhystic Study catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Rhystic%20Study&format=image&version=normal", true, true, true, false, true, true, "Rhystic Study", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Common", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "rhystic-study", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000008"), null, "24", new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dockside Extortionist catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://api.scryfall.com/cards/named?exact=Dockside%20Extortionist&format=image&version=normal", true, true, true, false, true, true, "Dockside Extortionist", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare", new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "dockside-extortionist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000009"), null, null, new Guid("53000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Final Fantasy Booster Pack catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://placehold.co/245x342?text=Final%20Fantasy%20Booster%20Pack", true, true, false, true, false, true, "Final Fantasy Booster Pack", new Guid("52000000-0000-0000-0000-000000000003"), "BoosterPack", null, new DateTime(2025, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "final-fantasy-booster-pack", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000010"), null, null, new Guid("53000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Final Fantasy Booster Box catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://placehold.co/245x342?text=Final%20Fantasy%20Booster%20Box", true, true, false, true, false, true, "Final Fantasy Booster Box", new Guid("52000000-0000-0000-0000-000000000004"), "BoosterBox", null, new DateTime(2025, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "final-fantasy-booster-box", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000011"), null, null, new Guid("53000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Commander Deck Sample catalog product.", new Guid("51000000-0000-0000-0000-000000000001"), "https://placehold.co/245x342?text=Commander%20Deck%20Sample", true, true, false, true, false, false, "Commander Deck Sample", new Guid("52000000-0000-0000-0000-000000000008"), "CommanderDeck", null, new DateTime(2025, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "commander-deck-sample", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000012"), null, "4/102", new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Charizard catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Charizard", true, true, true, false, true, true, "Charizard", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "charizard", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000013"), null, "60/64", new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pikachu catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Pikachu", true, true, true, false, true, true, "Pikachu", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Common", new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "pikachu", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000014"), null, "2/102", new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Blastoise catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Blastoise", true, true, true, false, true, true, "Blastoise", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "blastoise", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000015"), null, "10/102", new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mewtwo catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Mewtwo", true, true, true, false, true, false, "Mewtwo", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "mewtwo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000016"), null, "5/62", new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gengar catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Gengar", true, true, true, false, true, true, "Gengar", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "gengar", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000017"), null, "22/107", new Guid("53000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rayquaza catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Rayquaza", true, true, true, false, true, true, "Rayquaza", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "rayquaza", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000018"), null, "9/111", new Guid("53000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lugia catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Lugia", true, true, true, false, true, true, "Lugia", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "lugia", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000019"), null, "13/75", new Guid("53000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Umbreon catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Umbreon", true, true, true, false, true, true, "Umbreon", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Rare Holo", new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "umbreon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000020"), null, null, new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prismatic Evolutions Booster Pack catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Prismatic%20Evolutions%20Booster%20Pack", true, true, false, true, false, true, "Prismatic Evolutions Booster Pack", new Guid("52000000-0000-0000-0000-000000000003"), "BoosterPack", null, new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "prismatic-evolutions-booster-pack", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000021"), null, null, new Guid("53000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prismatic Evolutions Elite Trainer Box catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Prismatic%20Evolutions%20Elite%20Trainer%20Box", true, true, false, true, false, true, "Prismatic Evolutions Elite Trainer Box", new Guid("52000000-0000-0000-0000-000000000005"), "EliteTrainerBox", null, new DateTime(2025, 1, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "prismatic-evolutions-elite-trainer-box", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000022"), null, null, new Guid("53000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Destined Rivals Booster Pack catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Destined%20Rivals%20Booster%20Pack", true, true, false, true, false, false, "Destined Rivals Booster Pack", new Guid("52000000-0000-0000-0000-000000000003"), "BoosterPack", null, new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "destined-rivals-booster-pack", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000023"), null, null, new Guid("53000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chaos Rising Elite Trainer Box catalog product.", new Guid("51000000-0000-0000-0000-000000000002"), "https://placehold.co/245x342?text=Chaos%20Rising%20Elite%20Trainer%20Box", true, true, false, true, false, true, "Chaos Rising Elite Trainer Box", new Guid("52000000-0000-0000-0000-000000000005"), "EliteTrainerBox", null, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "chaos-rising-elite-trainer-box", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000024"), null, "OP01-001", new Guid("53000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Monkey D. Luffy Sample Card catalog product.", new Guid("51000000-0000-0000-0000-000000000003"), "https://placehold.co/245x342?text=Monkey%20D.%20Luffy%20Sample%20Card", true, true, true, false, true, true, "Monkey D. Luffy Sample Card", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Leader", new DateTime(2022, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "monkey-d-luffy-sample-card", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000025"), null, "OP01-025", new Guid("53000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Roronoa Zoro Sample Card catalog product.", new Guid("51000000-0000-0000-0000-000000000003"), "https://placehold.co/245x342?text=Roronoa%20Zoro%20Sample%20Card", true, true, true, false, true, true, "Roronoa Zoro Sample Card", new Guid("52000000-0000-0000-0000-000000000018"), "SingleCard", "Super Rare", new DateTime(2022, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "roronoa-zoro-sample-card", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000026"), null, null, new Guid("53000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Romance Dawn Booster Pack catalog product.", new Guid("51000000-0000-0000-0000-000000000003"), "https://placehold.co/245x342?text=Romance%20Dawn%20Booster%20Pack", true, true, false, true, false, true, "Romance Dawn Booster Pack", new Guid("52000000-0000-0000-0000-000000000003"), "BoosterPack", null, new DateTime(2022, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "romance-dawn-booster-pack", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000027"), null, null, new Guid("53000000-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Paramount War Booster Box catalog product.", new Guid("51000000-0000-0000-0000-000000000003"), "https://placehold.co/245x342?text=Paramount%20War%20Booster%20Box", true, true, false, true, false, true, "Paramount War Booster Box", new Guid("52000000-0000-0000-0000-000000000004"), "BoosterBox", null, new DateTime(2023, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "paramount-war-booster-box", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("54000000-0000-0000-0000-000000000028"), null, null, new Guid("53000000-0000-0000-0000-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "500 Years in the Future Booster Pack catalog product.", new Guid("51000000-0000-0000-0000-000000000003"), "https://placehold.co/245x342?text=500%20Years%20in%20the%20Future%20Booster%20Pack", true, true, false, true, false, false, "500 Years in the Future Booster Pack", new Guid("52000000-0000-0000-0000-000000000003"), "BoosterPack", null, new DateTime(2024, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "500-years-in-the-future-booster-pack", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProductVariants",
                columns: new[] { "Id", "CatalogProductId", "CreatedUtc", "IsFirstEdition", "IsFoil", "IsPromo", "IsReverseHolo", "IsSealedCase", "IsSerialized", "Language", "VariantName" },
                values: new object[,]
                {
                    { new Guid("55000000-0000-0000-0000-000000000001"), new Guid("54000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000002"), new Guid("54000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000003"), new Guid("54000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000004"), new Guid("54000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000005"), new Guid("54000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000006"), new Guid("54000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000007"), new Guid("54000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000008"), new Guid("54000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000009"), new Guid("54000000-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000010"), new Guid("54000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000011"), new Guid("54000000-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" },
                    { new Guid("55000000-0000-0000-0000-000000000012"), new Guid("54000000-0000-0000-0000-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, false, false, false, "English", "Normal" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardSets_GameId",
                table: "CardSets",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_CardSets_GameId_Code",
                table: "CardSets",
                columns: new[] { "GameId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_CardSets_GameId_Name",
                table: "CardSets",
                columns: new[] { "GameId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardSets_Slug",
                table: "CardSets",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportErrors_CatalogImportRunId",
                table: "CatalogImportErrors",
                column: "CatalogImportRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportErrors_ExternalId",
                table: "CatalogImportErrors",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportErrors_SourceName",
                table: "CatalogImportErrors",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_ImportType",
                table: "CatalogImportRuns",
                column: "ImportType");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_SourceName",
                table: "CatalogImportRuns",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_StartedUtc",
                table: "CatalogImportRuns",
                column: "StartedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_Status",
                table: "CatalogImportRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_CardSetId",
                table: "CatalogProducts",
                column: "CardSetId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_GameId",
                table: "CatalogProducts",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_GameId_Name_CardSetId",
                table: "CatalogProducts",
                columns: new[] { "GameId", "Name", "CardSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_IsFeatured",
                table: "CatalogProducts",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_IsTrending",
                table: "CatalogProducts",
                column: "IsTrending");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_Name",
                table: "CatalogProducts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_ProductCategoryId",
                table: "CatalogProducts",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_ProductType",
                table: "CatalogProducts",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_Slug",
                table: "CatalogProducts",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalProductMappings_CatalogProductId",
                table: "ExternalProductMappings",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalProductMappings_SourceName_ExternalId",
                table: "ExternalProductMappings",
                columns: new[] { "SourceName", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Name",
                table: "Games",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Slug",
                table: "Games",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentCategoryId",
                table: "ProductCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Slug",
                table: "ProductCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CatalogProductId",
                table: "ProductVariants",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CatalogProductId_VariantName",
                table: "ProductVariants",
                columns: new[] { "CatalogProductId", "VariantName" });

            migrationBuilder.CreateIndex(
                name: "IX_SellerInventoryImages_SellerInventoryItemId",
                table: "SellerInventoryImages",
                column: "SellerInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerInventoryItems_CatalogProductId",
                table: "SellerInventoryItems",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerInventoryItems_Condition",
                table: "SellerInventoryItems",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_SellerInventoryItems_IsAvailableForSale",
                table: "SellerInventoryItems",
                column: "IsAvailableForSale");

            migrationBuilder.CreateIndex(
                name: "IX_SellerInventoryItems_ProductVariantId",
                table: "SellerInventoryItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerInventoryItems_SellerUserId",
                table: "SellerInventoryItems",
                column: "SellerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogImportErrors");

            migrationBuilder.DropTable(
                name: "ExternalProductMappings");

            migrationBuilder.DropTable(
                name: "SellerInventoryImages");

            migrationBuilder.DropTable(
                name: "CatalogImportRuns");

            migrationBuilder.DropTable(
                name: "SellerInventoryItems");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "CatalogProducts");

            migrationBuilder.DropTable(
                name: "CardSets");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
