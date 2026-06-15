export type MarketAggregationResult = {
  runId?: string;
  status: string;
  productsQueued: number;
  productsProcessed: number;
  productsSkipped: number;
  listingsCreated: number;
  listingsUpdated: number;
  referencePricesCreated: number;
  snapshotsCreated: number;
  metricsComputed: number;
  errors: number;
  notes?: string;
  diagnosticEvents: MarketDiagnosticEvent[];
};

export type MarketDiagnosticEvent = {
  atUtc: string;
  level: string;
  stage: string;
  message: string;
  data?: string;
};

export type ProductMarketSummary = {
  catalogProductId: string;
  productName: string;
  gameName: string;
  setName?: string;
  cardNumber?: string;
  imageUrl?: string;
  currentMarketPrice?: number;
  previousMarketPrice?: number;
  priceChangeAmount?: number;
  priceChangePercent?: number;
  lowPrice?: number;
  highPrice?: number;
  listingCount: number;
  soldCount: number;
  salesVolume?: number;
  estimatedGrossMargin?: number;
  estimatedNetMargin?: number;
  estimatedRoiPercent?: number;
  dealScore?: number;
  opportunityScore?: number;
  confidenceLabel: string;
  confidenceScore?: number;
  freshnessLabel: string;
  lastUpdatedUtc?: string;
  includedComparableCount: number;
  excludedComparableCount: number;
  hasMarketData: boolean;
  dataStatus: string;
  dataQualityMessage: string;
  isDemoData: boolean;
};

export type MarketplaceComparison = {
  catalogProductId: string;
  rows: MarketplaceComparisonRow[];
};

export type MarketplaceComparisonRow = {
  sourceName: string;
  referenceMarketPrice?: number;
  lowestActiveListing?: number;
  medianActiveListing?: number;
  lastSoldPrice?: number;
  salesVolume?: number;
  spreadAmount?: number;
  spreadPercent?: number;
  listingCount: number;
  soldCount: number;
  freshnessLabel: string;
  confidenceLabel: string;
  externalUrl?: string;
  isDemoData: boolean;
};

export type MarketChart = {
  catalogProductId: string;
  priceSeries: MarketChartPoint[];
  volumeSeries: MarketVolumePoint[];
  distributionBuckets: MarketDistributionBucket[];
  percentileMarkers: MarketPercentileMarker[];
  isDemoData: boolean;
};

export type MarketChartPoint = {
  dateUtc: string;
  referencePrice?: number;
  medianActiveListing?: number;
  lowestActiveListing?: number;
  medianSoldPrice?: number;
  lowPrice?: number;
  highPrice?: number;
};

export type MarketVolumePoint = {
  dateUtc: string;
  listingCount: number;
  soldCount: number;
  salesVolume?: number;
};

export type MarketDistributionBucket = {
  minPrice: number;
  maxPrice: number;
  count: number;
};

export type MarketPercentileMarker = {
  label: string;
  price: number;
};

export type MarketConfidence = {
  catalogProductId: string;
  score: number;
  activeListingCount: number;
  referenceSourceCount: number;
  soldCompCount: number;
  lastUpdatedUtc?: string;
  label: string;
  notes: string;
};

export type DealOpportunity = {
  listingId?: string;
  catalogProductId: string;
  productName: string;
  gameName: string;
  setName?: string;
  cardSetId?: string;
  cardNumber?: string;
  imageUrl?: string;
  sourceName: string;
  title: string;
  itemPrice?: number;
  shippingPrice?: number;
  listingPrice: number;
  trustedMarketPrice?: number;
  expectedMarketValue?: number;
  estimatedFees?: number;
  estimatedNetProfit?: number;
  estimatedRoiPercent?: number;
  liquidityScore?: number;
  matchConfidence?: number;
  discountAmount?: number;
  discountPercent?: number;
  dealScore?: number;
  dealLabel: string;
  reason?: string;
  listingUrl: string;
  isDemoData: boolean;
};

export type SetMarketDashboard = {
  cardSetId: string;
  setName: string;
  gameName: string;
  products: SetMarketDashboardProduct[];
  topMovers: SetMarketDashboardProduct[];
  highestVolume: SetMarketDashboardProduct[];
  bestDeals: DealOpportunity[];
  highestOpportunity: SetMarketDashboardProduct[];
  mostListed: SetMarketDashboardProduct[];
  lowestConfidence: SetMarketDashboardProduct[];
};

export type SetMarketDashboardProduct = {
  catalogProductId: string;
  productName: string;
  gameName: string;
  setName?: string;
  cardNumber?: string;
  categoryName?: string;
  imageUrl?: string;
  currentMarketPrice?: number;
  priceChangePercent?: number;
  listingCount: number;
  soldCount: number;
  opportunityScore?: number;
  rankingScore?: number;
  signalLabel: string;
  signalDetail: string;
  confidenceLabel: string;
  hasMarketData: boolean;
  isDemoData: boolean;
};

export type WatchlistIntelligence = {
  catalogWatchlistItemId: string;
  catalogProductId: string;
  productName: string;
  gameName: string;
  setName?: string;
  imageUrl?: string;
  currentMarketPrice?: number;
  targetPrice?: number;
  opportunityScore?: number;
  createdUtc: string;
};

export type ProviderHealth = {
  sourceName: string;
  providerType: string;
  isEnabled: boolean;
  isHealthy: boolean;
  status: string;
  message: string;
};
