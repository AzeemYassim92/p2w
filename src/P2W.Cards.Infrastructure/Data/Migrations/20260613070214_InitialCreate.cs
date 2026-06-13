using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace P2W.Cards.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Game = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SetCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rarity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PriorityRank = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marketplaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFoil = table.Column<bool>(type: "bit", nullable: false),
                    IsReverseHolo = table.Column<bool>(type: "bit", nullable: false),
                    IsFirstEdition = table.Column<bool>(type: "bit", nullable: false),
                    IsGraded = table.Column<bool>(type: "bit", nullable: false),
                    GradingCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grade = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardVariants_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalCardMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalSlug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastVerifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCardMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalCardMappings_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarketplaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalListingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawCondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ListingUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAuction = table.Column<bool>(type: "bit", nullable: false),
                    AuctionEndsUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ListedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RawSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Listings_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Listings_Marketplaces_MarketplaceId",
                        column: x => x.MarketplaceId,
                        principalTable: "Marketplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HasTriggered = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceReferenceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PriceReferenceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceReferenceSnapshots_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PriceReferenceSnapshots_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarketplaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LowestPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MedianPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ListingCount = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_Marketplaces_MarketplaceId",
                        column: x => x.MarketplaceId,
                        principalTable: "Marketplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchlistItems_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WatchlistItems_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Cards",
                columns: new[] { "Id", "Artist", "CardNumber", "CreatedUtc", "Game", "ImageUrl", "Name", "Rarity", "SetCode", "SetName", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), "Mitsuhiro Arita", "4/102", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Charizard", "Charizard", "Rare Holo", "BASE", "Base Set", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000002"), "Mitsuhiro Arita", "60/64", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Pikachu", "Pikachu", "Common", "JUNGLE", "Jungle", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "Ken Sugimori", "2/102", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Blastoise", "Blastoise", "Rare Holo", "BASE", "Base Set", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000004"), "Ken Sugimori", "10/102", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Mewtwo", "Mewtwo", "Rare Holo", "BASE", "Base Set", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000005"), "Keiji Kinebuchi", "5/62", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Gengar", "Gengar", "Rare Holo", "FOSSIL", "Fossil", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000006"), "Mitsuhiro Arita", "22/107", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Rayquaza", "Rayquaza", "Rare Holo", "EX", "EX Deoxys", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000007"), "Hironobu Yoshida", "9/111", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Lugia", "Lugia", "Rare Holo", "NEO", "Neo Genesis", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000008"), "Ken Sugimori", "13/75", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pokemon", "https://placehold.co/245x342?text=Umbreon", "Umbreon", "Rare Holo", "NEO", "Neo Discovery", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000001"), "Christopher Rush", "232", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Black%20Lotus", "Black Lotus", "Rare", "LIMITED", "Limited Edition Alpha", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000002"), "Mark Tedin", "276", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Sol%20Ring", "Sol Ring", "Uncommon", "COMMANDER", "Commander", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000003"), "Christopher Moeller", "146", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Lightning%20Bolt", "Lightning Bolt", "Common", "MAGIC", "Magic 2011", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000004"), "Hannibal King", "67", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Counterspell", "Counterspell", "Common", "SEVENTH", "Seventh Edition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000005"), "Dan Frazier", "138", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Mox%20Diamond", "Mox Diamond", "Rare", "STRONGHOLD", "Stronghold", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000006"), "Matt Stewart", "225", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Mana%20Crypt", "Mana Crypt", "Mythic Rare", "ETERNAL", "Eternal Masters", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000007"), "Terese Nielsen", "45", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Rhystic%20Study", "Rhystic Study", "Common", "PROPHECY", "Prophecy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000008"), "Forrest Imel", "24", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magic", "https://placehold.co/245x342?text=Dockside%20Extortionist", "Dockside Extortionist", "Rare", "COMMANDER", "Commander 2019", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ExternalSources",
                columns: new[] { "Id", "CreatedUtc", "IsActive", "Name", "PriorityRank", "ProviderType" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Mock", 1, "PriceReference" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "TCGplayer", 2, "PriceReference" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "eBay", 3, "MarketplaceListing" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Scryfall", 4, "Catalog" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "MTGJSON", 5, "PriceReference" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "PokemonTCG", 6, "Catalog" },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "PriceCharting", 7, "PriceReference" },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "CardKingdom", 8, "PriceReference" },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Cardmarket", 9, "MarketplaceListing" }
                });

            migrationBuilder.InsertData(
                table: "Marketplaces",
                columns: new[] { "Id", "BaseUrl", "CreatedUtc", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "https://example.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "MockMarket" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "eBay" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TCGplayer" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Card Kingdom" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "PriceCharting" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Cardmarket" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Game",
                table: "Cards",
                column: "Game");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Game_Name_SetName",
                table: "Cards",
                columns: new[] { "Game", "Name", "SetName" });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Name",
                table: "Cards",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_SetName",
                table: "Cards",
                column: "SetName");

            migrationBuilder.CreateIndex(
                name: "IX_CardVariants_CardId",
                table: "CardVariants",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCardMappings_CardId",
                table: "ExternalCardMappings",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCardMappings_SourceName_ExternalId",
                table: "ExternalCardMappings",
                columns: new[] { "SourceName", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSources_Name",
                table: "ExternalSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CapturedAtUtc",
                table: "Listings",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CardId",
                table: "Listings",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CardVariantId",
                table: "Listings",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_MarketplaceId",
                table: "Listings",
                column: "MarketplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_MarketplaceId_ExternalListingId",
                table: "Listings",
                columns: new[] { "MarketplaceId", "ExternalListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_SourceName",
                table: "Listings",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplaces_Name",
                table: "Marketplaces",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_CardId",
                table: "PriceAlerts",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_CardVariantId",
                table: "PriceAlerts",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_IsActive",
                table: "PriceAlerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceReferenceSnapshots_CapturedAtUtc",
                table: "PriceReferenceSnapshots",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PriceReferenceSnapshots_CardId",
                table: "PriceReferenceSnapshots",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceReferenceSnapshots_CardVariantId",
                table: "PriceReferenceSnapshots",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceReferenceSnapshots_SourceName",
                table: "PriceReferenceSnapshots",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_CardId_CardVariantId_CapturedAtUtc",
                table: "PriceSnapshots",
                columns: new[] { "CardId", "CardVariantId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_CardVariantId",
                table: "PriceSnapshots",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_MarketplaceId",
                table: "PriceSnapshots",
                column: "MarketplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_CardId",
                table: "WatchlistItems",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_CardVariantId",
                table: "WatchlistItems",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_UserId",
                table: "WatchlistItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_UserId_CardId_CardVariantId",
                table: "WatchlistItems",
                columns: new[] { "UserId", "CardId", "CardVariantId" },
                unique: true,
                filter: "[CardVariantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalCardMappings");

            migrationBuilder.DropTable(
                name: "ExternalSources");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "PriceReferenceSnapshots");

            migrationBuilder.DropTable(
                name: "PriceSnapshots");

            migrationBuilder.DropTable(
                name: "WatchlistItems");

            migrationBuilder.DropTable(
                name: "Marketplaces");

            migrationBuilder.DropTable(
                name: "CardVariants");

            migrationBuilder.DropTable(
                name: "Cards");
        }
    }
}
