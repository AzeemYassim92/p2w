export type PriceSnapshot = {
  cardId: string;
  sourceName: string;
  lowestPrice: number;
  averagePrice: number;
  medianPrice: number;
  listingCount: number;
  currency: string;
  capturedAtUtc: string;
};

export type PriceReferenceSnapshot = {
  cardId: string;
  sourceName: string;
  marketPrice?: number;
  ungradedPrice?: number;
  grade7Price?: number;
  grade8Price?: number;
  grade9Price?: number;
  grade10Price?: number;
  buylistPrice?: number;
  retailPrice?: number;
  currency: string;
  capturedAtUtc: string;
};
