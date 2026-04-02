/**
 * Tests for the payload construction logic in QuoteSubmitPage.
 * Mirrors the mutationFn body to ensure optional fields are stripped when empty.
 */

type FormData = {
  price: string;
  scopeOfWork: string;
  proposedStartDate?: string;
  estimatedDurationValue?: string;
  estimatedDurationUnit?: string;
  vendorAvailability?: string;
  notToExceedPrice?: string;
  assumptions?: string;
  exclusions?: string;
  validUntil?: string;
  lineItems?: { description: string; quantity: string; unitPrice: string }[];
};

type Payload = {
  price: number;
  scopeOfWork: string;
  proposedStartDate?: string;
  estimatedDurationValue?: number;
  estimatedDurationUnit?: string;
  vendorAvailability?: string;
  notToExceedPrice?: number;
  assumptions?: string;
  exclusions?: string;
  validUntil?: string;
  lineItems?: { description: string; quantity: number; unitPrice: number }[];
};

function buildPayload(formData: FormData): Payload {
  const payload: Payload = {
    price: parseFloat(formData.price),
    scopeOfWork: formData.scopeOfWork,
  };
  if (formData.proposedStartDate) payload.proposedStartDate = formData.proposedStartDate;
  if (formData.estimatedDurationValue) payload.estimatedDurationValue = parseFloat(formData.estimatedDurationValue);
  if (formData.estimatedDurationUnit) payload.estimatedDurationUnit = formData.estimatedDurationUnit;
  if (formData.vendorAvailability) payload.vendorAvailability = formData.vendorAvailability;
  if (formData.notToExceedPrice) payload.notToExceedPrice = parseFloat(formData.notToExceedPrice);
  if (formData.assumptions) payload.assumptions = formData.assumptions;
  if (formData.exclusions) payload.exclusions = formData.exclusions;
  if (formData.validUntil) payload.validUntil = formData.validUntil;
  if (formData.lineItems && formData.lineItems.length > 0) {
    payload.lineItems = formData.lineItems.map(li => ({
      description: li.description,
      quantity: parseFloat(li.quantity),
      unitPrice: parseFloat(li.unitPrice),
    }));
  }
  return payload;
}

describe('buildPayload (QuoteSubmitPage mutationFn)', () => {
  it('includes required fields price and scopeOfWork', () => {
    const payload = buildPayload({ price: '1500', scopeOfWork: 'Fix the roof' });
    expect(payload.price).toBe(1500);
    expect(payload.scopeOfWork).toBe('Fix the roof');
  });

  it('strips all optional fields when they are empty', () => {
    const payload = buildPayload({ price: '500', scopeOfWork: 'Paint walls' });
    expect(payload).not.toHaveProperty('proposedStartDate');
    expect(payload).not.toHaveProperty('estimatedDurationValue');
    expect(payload).not.toHaveProperty('estimatedDurationUnit');
    expect(payload).not.toHaveProperty('vendorAvailability');
    expect(payload).not.toHaveProperty('notToExceedPrice');
    expect(payload).not.toHaveProperty('assumptions');
    expect(payload).not.toHaveProperty('exclusions');
    expect(payload).not.toHaveProperty('validUntil');
    expect(payload).not.toHaveProperty('lineItems');
  });

  it('includes all optional fields when populated', () => {
    const payload = buildPayload({
      price: '2000',
      scopeOfWork: 'HVAC replacement',
      proposedStartDate: '2026-05-01',
      estimatedDurationValue: '3',
      estimatedDurationUnit: 'Days',
      vendorAvailability: 'Within 48 hours',
      notToExceedPrice: '2500',
      assumptions: 'Access provided',
      exclusions: 'No disposal',
      validUntil: '2026-05-31',
      lineItems: [
        { description: 'Labor', quantity: '8', unitPrice: '90' },
      ],
    });

    expect(payload.proposedStartDate).toBe('2026-05-01');
    expect(payload.estimatedDurationValue).toBe(3);
    expect(payload.estimatedDurationUnit).toBe('Days');
    expect(payload.vendorAvailability).toBe('Within 48 hours');
    expect(payload.notToExceedPrice).toBe(2500);
    expect(payload.assumptions).toBe('Access provided');
    expect(payload.exclusions).toBe('No disposal');
    expect(payload.validUntil).toBe('2026-05-31');
    expect(payload.lineItems).toHaveLength(1);
    expect(payload.lineItems![0]).toEqual({ description: 'Labor', quantity: 8, unitPrice: 90 });
  });

  it('strips lineItems when the array is empty', () => {
    const payload = buildPayload({ price: '800', scopeOfWork: 'Electrical', lineItems: [] });
    expect(payload).not.toHaveProperty('lineItems');
  });

  it('converts line item strings to numbers', () => {
    const payload = buildPayload({
      price: '1000',
      scopeOfWork: 'Plumbing',
      lineItems: [
        { description: 'Parts', quantity: '2.5', unitPrice: '40.50' },
      ],
    });
    expect(payload.lineItems![0].quantity).toBe(2.5);
    expect(payload.lineItems![0].unitPrice).toBe(40.5);
  });

  it('converts notToExceedPrice string to number', () => {
    const payload = buildPayload({ price: '1000', scopeOfWork: 'Roofing', notToExceedPrice: '1200.99' });
    expect(payload.notToExceedPrice).toBe(1200.99);
  });
});
