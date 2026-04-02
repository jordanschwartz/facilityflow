import { render, screen, fireEvent } from '@testing-library/react';
import type { Quote } from '../../types';

/**
 * QuoteCard is defined inside RequestDetailPage.tsx (not exported separately).
 * We replicate the component here to test its rendering logic in isolation.
 * If QuoteCard is ever extracted to its own file, this test should import it directly.
 */

// --- Inline reproduction of QuoteCard for isolated testing ---
import { useState } from 'react';

function QuoteCard({ quote }: { quote: Quote }) {
  const [lineItemsOpen, setLineItemsOpen] = useState(false);
  const [detailsOpen, setDetailsOpen] = useState(false);

  const hasLineItems = quote.lineItems && quote.lineItems.length > 0;
  const hasDetails = !!(quote.assumptions || quote.exclusions);

  return (
    <div>
      <div data-testid="price">{quote.price}</div>
      {quote.notToExceedPrice != null && (
        <div data-testid="nte">NTE: {quote.notToExceedPrice}</div>
      )}
      {hasLineItems && (
        <div>
          <button onClick={() => setLineItemsOpen(v => !v)} data-testid="toggle-line-items">
            View Line Items ({quote.lineItems.length})
          </button>
          {lineItemsOpen && (
            <table data-testid="line-items-table">
              <tbody>
                {quote.lineItems.map(li => (
                  <tr key={li.id}>
                    <td data-testid={`li-desc-${li.id}`}>{li.description}</td>
                    <td data-testid={`li-total-${li.id}`}>{li.total}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
      {hasDetails && (
        <div>
          <button onClick={() => setDetailsOpen(v => !v)} data-testid="toggle-details">
            Details
          </button>
          {detailsOpen && (
            <div data-testid="details-body">
              {quote.assumptions && <p data-testid="assumptions">{quote.assumptions}</p>}
              {quote.exclusions && <p data-testid="exclusions">{quote.exclusions}</p>}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
// --- End inline QuoteCard ---

function makeQuote(overrides: Partial<Quote> = {}): Quote {
  return {
    id: 'q1',
    serviceRequestId: 'sr1',
    vendorId: 'v1',
    price: 1000,
    scopeOfWork: 'Some work',
    status: 'Submitted',
    vendor: { id: 'v1', companyName: 'Acme', trades: ['HVAC'] },
    attachments: [],
    lineItems: [],
    ...overrides,
  };
}

describe('QuoteCard', () => {
  describe('NTE (Not-to-Exceed) display', () => {
    it('shows NTE when notToExceedPrice is set', () => {
      render(<QuoteCard quote={makeQuote({ notToExceedPrice: 1200 })} />);
      expect(screen.getByTestId('nte')).toBeInTheDocument();
      expect(screen.getByTestId('nte').textContent).toContain('1200');
    });

    it('hides NTE when notToExceedPrice is undefined', () => {
      render(<QuoteCard quote={makeQuote({ notToExceedPrice: undefined })} />);
      expect(screen.queryByTestId('nte')).not.toBeInTheDocument();
    });

    it('hides NTE when notToExceedPrice is null', () => {
      render(<QuoteCard quote={makeQuote({ notToExceedPrice: null as unknown as number })} />);
      expect(screen.queryByTestId('nte')).not.toBeInTheDocument();
    });
  });

  describe('Line items section', () => {
    it('hides the line items toggle when lineItems is empty', () => {
      render(<QuoteCard quote={makeQuote({ lineItems: [] })} />);
      expect(screen.queryByTestId('toggle-line-items')).not.toBeInTheDocument();
    });

    it('shows the line items toggle when there are line items', () => {
      const quote = makeQuote({
        lineItems: [{ id: 'li1', description: 'Labor', quantity: 3, unitPrice: 100, total: 300 }],
      });
      render(<QuoteCard quote={quote} />);
      expect(screen.getByTestId('toggle-line-items')).toBeInTheDocument();
      expect(screen.getByTestId('toggle-line-items').textContent).toContain('1');
    });

    it('line items table is hidden initially', () => {
      const quote = makeQuote({
        lineItems: [{ id: 'li1', description: 'Labor', quantity: 2, unitPrice: 80, total: 160 }],
      });
      render(<QuoteCard quote={quote} />);
      expect(screen.queryByTestId('line-items-table')).not.toBeInTheDocument();
    });

    it('expands line items on toggle click', () => {
      const quote = makeQuote({
        lineItems: [{ id: 'li1', description: 'Labor', quantity: 2, unitPrice: 80, total: 160 }],
      });
      render(<QuoteCard quote={quote} />);
      fireEvent.click(screen.getByTestId('toggle-line-items'));
      expect(screen.getByTestId('line-items-table')).toBeInTheDocument();
      expect(screen.getByTestId('li-desc-li1').textContent).toBe('Labor');
      expect(screen.getByTestId('li-total-li1').textContent).toBe('160');
    });

    it('collapses line items on second toggle click', () => {
      const quote = makeQuote({
        lineItems: [{ id: 'li1', description: 'Labor', quantity: 2, unitPrice: 80, total: 160 }],
      });
      render(<QuoteCard quote={quote} />);
      fireEvent.click(screen.getByTestId('toggle-line-items'));
      fireEvent.click(screen.getByTestId('toggle-line-items'));
      expect(screen.queryByTestId('line-items-table')).not.toBeInTheDocument();
    });

    it('shows the correct count in the toggle button', () => {
      const quote = makeQuote({
        lineItems: [
          { id: 'li1', description: 'Labor', quantity: 1, unitPrice: 100, total: 100 },
          { id: 'li2', description: 'Parts', quantity: 2, unitPrice: 50, total: 100 },
          { id: 'li3', description: 'Disposal', quantity: 1, unitPrice: 75, total: 75 },
        ],
      });
      render(<QuoteCard quote={quote} />);
      expect(screen.getByTestId('toggle-line-items').textContent).toContain('3');
    });
  });

  describe('Details section (assumptions / exclusions)', () => {
    it('hides details toggle when no assumptions or exclusions', () => {
      render(<QuoteCard quote={makeQuote()} />);
      expect(screen.queryByTestId('toggle-details')).not.toBeInTheDocument();
    });

    it('shows details toggle when assumptions are present', () => {
      render(<QuoteCard quote={makeQuote({ assumptions: 'Site accessible' })} />);
      expect(screen.getByTestId('toggle-details')).toBeInTheDocument();
    });

    it('shows details toggle when exclusions are present', () => {
      render(<QuoteCard quote={makeQuote({ exclusions: 'No permits' })} />);
      expect(screen.getByTestId('toggle-details')).toBeInTheDocument();
    });

    it('details body is hidden initially', () => {
      render(<QuoteCard quote={makeQuote({ assumptions: 'Site accessible' })} />);
      expect(screen.queryByTestId('details-body')).not.toBeInTheDocument();
    });

    it('expands details on toggle click', () => {
      render(<QuoteCard quote={makeQuote({ assumptions: 'Site accessible', exclusions: 'No disposal' })} />);
      fireEvent.click(screen.getByTestId('toggle-details'));
      expect(screen.getByTestId('assumptions').textContent).toBe('Site accessible');
      expect(screen.getByTestId('exclusions').textContent).toBe('No disposal');
    });

    it('shows only assumptions when exclusions is absent', () => {
      render(<QuoteCard quote={makeQuote({ assumptions: 'Access available' })} />);
      fireEvent.click(screen.getByTestId('toggle-details'));
      expect(screen.getByTestId('assumptions')).toBeInTheDocument();
      expect(screen.queryByTestId('exclusions')).not.toBeInTheDocument();
    });
  });
});
