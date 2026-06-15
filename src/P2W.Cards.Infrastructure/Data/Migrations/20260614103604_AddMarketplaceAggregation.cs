using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace P2W.Cards.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceAggregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcquiredAtUtc",
                table: "SellerInventoryItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionSource",
                table: "SellerInventoryItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostBasis",
                table: "SellerInventoryItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogAggregationCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkloadType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CheckpointValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogAggregationCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogMarketMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WindowName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WindowEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentMarketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousMarketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceChangeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceChangePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LowPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HighPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ListingCount = table.Column<int>(type: "int", nullable: false),
                    SoldCount = table.Column<int>(type: "int", nullable: false),
                    SalesVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalSoldValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AverageSoldValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VolumeScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TrendScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VolatilityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LiquidityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SpreadScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DealScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OpportunityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedFeesPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedShippingCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedGrossMargin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedNetMargin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedRoiPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ComputedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogMarketMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogMarketMetrics_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogMarketMetrics_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CatalogProviderIngestionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkloadType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsCreated = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    CheckpointBefore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckpointAfter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProviderIngestionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogWatchlistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TargetDiscountPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AlertOnVolumeSpike = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnPriceDrop = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnNewDeal = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnDataRefresh = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogWatchlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogWatchlistItems_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogWatchlistItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupportsListings = table.Column<bool>(type: "bit", nullable: false),
                    SupportsSoldComps = table.Column<bool>(type: "bit", nullable: false),
                    SupportsBuylist = table.Column<bool>(type: "bit", nullable: false),
                    SupportsReferencePrices = table.Column<bool>(type: "bit", nullable: false),
                    SupportsBulkCsv = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PriorityRank = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductMarketViewEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMarketViewEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMarketViewEvents_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProviderIngestionErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngestionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkloadType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProviderIngestionErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogProviderIngestionErrors_CatalogProviderIngestionRuns_IngestionRunId",
                        column: x => x.IngestionRunId,
                        principalTable: "CatalogProviderIngestionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogMarketplaceListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarketplaceSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalListingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalSku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectivePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawCondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    SellerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SellerFeedbackScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SellerFeedbackPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SellerLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ListingUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAuction = table.Column<bool>(type: "bit", nullable: false),
                    AuctionEndsUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ListedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MatchConfidence = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MatchStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Matched"),
                    IsExcludedFromMarketValue = table.Column<bool>(type: "bit", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogMarketplaceListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogMarketplaceListings_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogMarketplaceListings_MarketplaceSources_MarketplaceSourceId",
                        column: x => x.MarketplaceSourceId,
                        principalTable: "MarketplaceSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogMarketplaceListings_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CatalogMarketplaceSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarketplaceSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalSaleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalListingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalSku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveSoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawCondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    SellerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoldAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SaleUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchConfidence = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MatchStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Matched"),
                    IsExcludedFromMarketValue = table.Column<bool>(type: "bit", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogMarketplaceSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogMarketplaceSales_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogMarketplaceSales_MarketplaceSources_MarketplaceSourceId",
                        column: x => x.MarketplaceSourceId,
                        principalTable: "MarketplaceSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogMarketplaceSales_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CatalogMarketPriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarketplaceSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LowestListingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MedianListingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AverageListingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HighestListingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastSoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MedianSoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AverageSoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LowestSoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HighestSoldPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceMarketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceLowPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceMidPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceHighPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ListingCount = table.Column<int>(type: "int", nullable: false),
                    SoldCount = table.Column<int>(type: "int", nullable: false),
                    SalesVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogMarketPriceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogMarketPriceSnapshots_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogMarketPriceSnapshots_MarketplaceSources_MarketplaceSourceId",
                        column: x => x.MarketplaceSourceId,
                        principalTable: "MarketplaceSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CatalogMarketPriceSnapshots_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExternalMarketplaceSkuMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MarketplaceSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalSku = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalProductId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchConfidence = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MappingStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "AutoMatched"),
                    MappingNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastVerifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalMarketplaceSkuMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalMarketplaceSkuMappings_CatalogProducts_CatalogProductId",
                        column: x => x.CatalogProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalMarketplaceSkuMappings_MarketplaceSources_MarketplaceSourceId",
                        column: x => x.MarketplaceSourceId,
                        principalTable: "MarketplaceSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalMarketplaceSkuMappings_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "MarketplaceSources",
                columns: new[] { "Id", "BaseUrl", "CreatedUtc", "DefaultCurrency", "IsActive", "Name", "PriorityRank", "Slug", "SupportsBulkCsv", "SupportsBuylist", "SupportsListings", "SupportsReferencePrices", "SupportsSoldComps", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("56000000-0000-0000-0000-000000000001"), "https://www.ebay.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, "eBay", 1, "ebay", false, false, true, false, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("56000000-0000-0000-0000-000000000002"), "https://pokemontcg.io", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, "PokemonTCG", 2, "pokemontcg", false, false, false, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("56000000-0000-0000-0000-000000000003"), "https://www.pricecharting.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, "PriceCharting", 3, "pricecharting", true, false, false, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("56000000-0000-0000-0000-000000000004"), "https://www.tcgplayer.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, "TCGplayer", 4, "tcgplayer", false, false, false, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("56000000-0000-0000-0000-000000000005"), "https://www.cardmarket.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, "Cardmarket", 5, "cardmarket", false, false, false, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("56000000-0000-0000-0000-000000000006"), "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", false, "P2W Internal", 6, "p2w-internal", false, false, true, false, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("56000000-0000-0000-0000-000000000007"), "https://example.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, "MockMarket", 99, "mockmarket", false, false, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogAggregationCheckpoints_SourceName_WorkloadType",
                table: "CatalogAggregationCheckpoints",
                columns: new[] { "SourceName", "WorkloadType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_CatalogProductId",
                table: "CatalogMarketMetrics",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_CatalogProductId_WindowName_Condition_ComputedAtUtc",
                table: "CatalogMarketMetrics",
                columns: new[] { "CatalogProductId", "WindowName", "Condition", "ComputedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_ComputedAtUtc",
                table: "CatalogMarketMetrics",
                column: "ComputedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_Condition",
                table: "CatalogMarketMetrics",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_Currency",
                table: "CatalogMarketMetrics",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_ProductVariantId",
                table: "CatalogMarketMetrics",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketMetrics_WindowName",
                table: "CatalogMarketMetrics",
                column: "WindowName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_CapturedAtUtc",
                table: "CatalogMarketplaceListings",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_CatalogProductId",
                table: "CatalogMarketplaceListings",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_CatalogProductId_SourceName_IsActive",
                table: "CatalogMarketplaceListings",
                columns: new[] { "CatalogProductId", "SourceName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_IsActive",
                table: "CatalogMarketplaceListings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_IsExcludedFromMarketValue",
                table: "CatalogMarketplaceListings",
                column: "IsExcludedFromMarketValue");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_LastSeenUtc",
                table: "CatalogMarketplaceListings",
                column: "LastSeenUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_MarketplaceSourceId",
                table: "CatalogMarketplaceListings",
                column: "MarketplaceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_MarketplaceSourceId_ExternalListingId",
                table: "CatalogMarketplaceListings",
                columns: new[] { "MarketplaceSourceId", "ExternalListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_MatchStatus",
                table: "CatalogMarketplaceListings",
                column: "MatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_ProductVariantId",
                table: "CatalogMarketplaceListings",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceListings_SourceName",
                table: "CatalogMarketplaceListings",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_CapturedAtUtc",
                table: "CatalogMarketplaceSales",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_CatalogProductId",
                table: "CatalogMarketplaceSales",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_CatalogProductId_SourceName_SoldAtUtc",
                table: "CatalogMarketplaceSales",
                columns: new[] { "CatalogProductId", "SourceName", "SoldAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_IsExcludedFromMarketValue",
                table: "CatalogMarketplaceSales",
                column: "IsExcludedFromMarketValue");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_MarketplaceSourceId",
                table: "CatalogMarketplaceSales",
                column: "MarketplaceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_MarketplaceSourceId_ExternalSaleId",
                table: "CatalogMarketplaceSales",
                columns: new[] { "MarketplaceSourceId", "ExternalSaleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_MatchStatus",
                table: "CatalogMarketplaceSales",
                column: "MatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_ProductVariantId",
                table: "CatalogMarketplaceSales",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_SoldAtUtc",
                table: "CatalogMarketplaceSales",
                column: "SoldAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketplaceSales_SourceName",
                table: "CatalogMarketplaceSales",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_CapturedAtUtc",
                table: "CatalogMarketPriceSnapshots",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_CatalogProductId",
                table: "CatalogMarketPriceSnapshots",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_CatalogProductId_SourceName_Condition_CapturedAtUtc",
                table: "CatalogMarketPriceSnapshots",
                columns: new[] { "CatalogProductId", "SourceName", "Condition", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_Condition",
                table: "CatalogMarketPriceSnapshots",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_MarketplaceSourceId",
                table: "CatalogMarketPriceSnapshots",
                column: "MarketplaceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_ProductVariantId",
                table: "CatalogMarketPriceSnapshots",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMarketPriceSnapshots_SourceName",
                table: "CatalogMarketPriceSnapshots",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionErrors_CatalogProductId",
                table: "CatalogProviderIngestionErrors",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionErrors_ExternalId",
                table: "CatalogProviderIngestionErrors",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionErrors_IngestionRunId",
                table: "CatalogProviderIngestionErrors",
                column: "IngestionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionErrors_SourceName",
                table: "CatalogProviderIngestionErrors",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionErrors_WorkloadType",
                table: "CatalogProviderIngestionErrors",
                column: "WorkloadType");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionRuns_SourceName",
                table: "CatalogProviderIngestionRuns",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionRuns_StartedUtc",
                table: "CatalogProviderIngestionRuns",
                column: "StartedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionRuns_Status",
                table: "CatalogProviderIngestionRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderIngestionRuns_WorkloadType",
                table: "CatalogProviderIngestionRuns",
                column: "WorkloadType");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogWatchlistItems_CatalogProductId",
                table: "CatalogWatchlistItems",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogWatchlistItems_ProductVariantId",
                table: "CatalogWatchlistItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogWatchlistItems_UserId",
                table: "CatalogWatchlistItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogWatchlistItems_UserId_CatalogProductId_ProductVariantId",
                table: "CatalogWatchlistItems",
                columns: new[] { "UserId", "CatalogProductId", "ProductVariantId" },
                unique: true,
                filter: "[ProductVariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarketplaceSkuMappings_CatalogProductId",
                table: "ExternalMarketplaceSkuMappings",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarketplaceSkuMappings_MappingStatus",
                table: "ExternalMarketplaceSkuMappings",
                column: "MappingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarketplaceSkuMappings_MarketplaceSourceId",
                table: "ExternalMarketplaceSkuMappings",
                column: "MarketplaceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarketplaceSkuMappings_ProductVariantId",
                table: "ExternalMarketplaceSkuMappings",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarketplaceSkuMappings_SourceName",
                table: "ExternalMarketplaceSkuMappings",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarketplaceSkuMappings_SourceName_ExternalSku",
                table: "ExternalMarketplaceSkuMappings",
                columns: new[] { "SourceName", "ExternalSku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceSources_IsActive",
                table: "MarketplaceSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceSources_Name",
                table: "MarketplaceSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceSources_Slug",
                table: "MarketplaceSources",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketViewEvents_CatalogProductId",
                table: "ProductMarketViewEvents",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketViewEvents_CreatedUtc",
                table: "ProductMarketViewEvents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketViewEvents_EventType",
                table: "ProductMarketViewEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketViewEvents_UserId",
                table: "ProductMarketViewEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogAggregationCheckpoints");

            migrationBuilder.DropTable(
                name: "CatalogMarketMetrics");

            migrationBuilder.DropTable(
                name: "CatalogMarketplaceListings");

            migrationBuilder.DropTable(
                name: "CatalogMarketplaceSales");

            migrationBuilder.DropTable(
                name: "CatalogMarketPriceSnapshots");

            migrationBuilder.DropTable(
                name: "CatalogProviderIngestionErrors");

            migrationBuilder.DropTable(
                name: "CatalogWatchlistItems");

            migrationBuilder.DropTable(
                name: "ExternalMarketplaceSkuMappings");

            migrationBuilder.DropTable(
                name: "ProductMarketViewEvents");

            migrationBuilder.DropTable(
                name: "CatalogProviderIngestionRuns");

            migrationBuilder.DropTable(
                name: "MarketplaceSources");

            migrationBuilder.DropColumn(
                name: "AcquiredAtUtc",
                table: "SellerInventoryItems");

            migrationBuilder.DropColumn(
                name: "AcquisitionSource",
                table: "SellerInventoryItems");

            migrationBuilder.DropColumn(
                name: "CostBasis",
                table: "SellerInventoryItems");
        }
    }
}
