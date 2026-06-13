export function money(value?: number, currency = 'USD') {
  if (value === undefined || value === null) {
    return '-';
  }

  return new Intl.NumberFormat('en-US', { style: 'currency', currency, maximumFractionDigits: 2 }).format(value);
}
