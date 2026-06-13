export type CardSearchResult = {
  cardId: string;
  name: string;
  game: string;
  setName: string;
  cardNumber?: string;
  rarity?: string;
  imageUrl?: string;
  lowestListingPrice?: number;
  marketReferencePrice?: number;
};

export type CardDetail = CardSearchResult & {
  setCode?: string;
  artist?: string;
  variants: CardVariant[];
};

export type CardVariant = {
  cardVariantId: string;
  variantName: string;
  language?: string;
  isFoil: boolean;
  isReverseHolo: boolean;
  isFirstEdition: boolean;
  isGraded: boolean;
  gradingCompany?: string;
  grade?: number;
};

export type MarketplaceProduct = {
  cardId: string;
  productType: string;
  name: string;
  game: string;
  setName: string;
  cardNumber?: string;
  rarity?: string;
  imageUrl?: string;
  price: number;
  currency: string;
  sourceName: string;
  sourceUrl: string;
};
