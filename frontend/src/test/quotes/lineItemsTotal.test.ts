/**
 * Tests for the line items total computation logic.
 * This mirrors the `lineItemsTotal` reduce used in QuoteSubmitPage.
 */

type LineItemFormValues = {
  description: string;
  quantity: string;
  unitPrice: string;
};

function computeLineItemsTotal(items: LineItemFormValues[]): number {
  return items.reduce((sum, item) => {
    const qty = parseFloat(item.quantity) || 0;
    const up = parseFloat(item.unitPrice) || 0;
    return sum + qty * up;
  }, 0);
}

describe('computeLineItemsTotal', () => {
  it('returns 0 for an empty array', () => {
    expect(computeLineItemsTotal([])).toBe(0);
  });

  it('computes total for a single item', () => {
    const items = [{ description: 'Labor', quantity: '3', unitPrice: '150' }];
    expect(computeLineItemsTotal(items)).toBe(450);
  });

  it('sums multiple items correctly', () => {
    const items = [
      { description: 'Labor', quantity: '4', unitPrice: '90' },
      { description: 'Parts', quantity: '2', unitPrice: '50' },
      { description: 'Disposal', quantity: '1', unitPrice: '75' },
    ];
    // 360 + 100 + 75 = 535
    expect(computeLineItemsTotal(items)).toBe(535);
  });

  it('treats empty quantity string as 0', () => {
    const items = [{ description: 'Labor', quantity: '', unitPrice: '100' }];
    expect(computeLineItemsTotal(items)).toBe(0);
  });

  it('treats empty unitPrice string as 0', () => {
    const items = [{ description: 'Labor', quantity: '5', unitPrice: '' }];
    expect(computeLineItemsTotal(items)).toBe(0);
  });

  it('treats non-numeric strings as 0', () => {
    const items = [{ description: 'Labor', quantity: 'abc', unitPrice: 'xyz' }];
    expect(computeLineItemsTotal(items)).toBe(0);
  });

  it('handles fractional quantities', () => {
    const items = [{ description: 'Material', quantity: '2.5', unitPrice: '80' }];
    expect(computeLineItemsTotal(items)).toBe(200);
  });

  it('handles decimal unit prices', () => {
    const items = [{ description: 'Part', quantity: '4', unitPrice: '125.50' }];
    expect(computeLineItemsTotal(items)).toBe(502);
  });
});
