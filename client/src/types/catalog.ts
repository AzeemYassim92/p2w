export type Game = {
  gameId: string;
  name: string;
  slug: string;
  description?: string;
  isPrimaryFocus: boolean;
  isActive: boolean;
  displayOrder: number;
};

export type CardSet = {
  cardSetId: string;
  gameId: string;
  gameName: string;
  name: string;
  slug: string;
  code?: string;
  releaseDate?: string;
  isUpcoming: boolean;
  logoUrl?: string;
  symbolUrl?: string;
};

export type ProductCategory = {
  productCategoryId: string;
  parentCategoryId?: string;
  name: string;
  slug: string;
  description?: string;
  displayOrder: number;
  children: ProductCategory[];
};

export type CatalogProduct = {
  catalogProductId: string;
  gameId: string;
  gameName: string;
  cardSetId?: string;
  setName?: string;
  setCode?: string;
  productCategoryId: string;
  categoryName: string;
  categorySlug: string;
  name: string;
  slug: string;
  productType: string;
  cardNumber?: string;
  rarity?: string;
  imageUrl?: string;
  estimatedMarketPrice?: number;
  primarySourceName?: string;
  primarySourceUrl?: string;
  releaseDate?: string;
  isSealed: boolean;
  isSingleCard: boolean;
  isFeatured: boolean;
  isTrending: boolean;
};

export type ProviderCapability = {
  sourceName: string;
  supportsMagic: boolean;
  supportsPokemon: boolean;
  supportsOnePiece: boolean;
  supportsCatalogSearch: boolean;
  supportsMarketplaceListings: boolean;
  supportsPriceReference: boolean;
  isConfigured: boolean;
  notes: string;
};

export type MarketplaceHome = {
  primaryGames: Game[];
  categories: ProductCategory[];
  trendingProducts: CatalogProduct[];
  featuredProducts: CatalogProduct[];
  latestSets: CardSet[];
  upcomingSets: CardSet[];
  providerCapabilities: ProviderCapability[];
};

export type SellerInventoryItem = {
  sellerInventoryItemId: string;
  catalogProductId: string;
  productName: string;
  gameName: string;
  setName?: string;
  variantName?: string;
  condition: number;
  quantity: number;
  askingPrice?: number;
  currency: string;
  isAvailableForSale: boolean;
  imageUrls: string[];
};
