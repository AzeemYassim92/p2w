export type Listing = {
  listingId: string;
  marketplaceName: string;
  sourceName: string;
  title: string;
  price: number;
  shippingPrice?: number;
  effectivePrice: number;
  currency: string;
  condition: string;
  rawCondition?: string;
  listingUrl: string;
  imageUrl?: string;
  isAuction: boolean;
  auctionEndsUtc?: string;
  capturedAtUtc: string;
};
