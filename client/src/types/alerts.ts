export type PriceAlert = {
  priceAlertId: string;
  cardId: string;
  cardName: string;
  targetPrice: number;
  isActive: boolean;
  hasTriggered: boolean;
  triggeredAtUtc?: string;
  createdUtc: string;
};
