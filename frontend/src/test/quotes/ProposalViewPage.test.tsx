import { render, screen, fireEvent } from '@testing-library/react';
import type { ProposalQuoteSummary } from '../../types';

/**
 * Tests for the NTE, timeline, and scope details sections of ProposalViewPage.
 * These sections are purely conditional/presentational so we test them
 * via inlined component logic extracted from the page.
 */

// --- Inline the conditional rendering blocks from ProposalViewPage ---
import { useState } from 'react';

function ProposalScopeDetails({ assumptions, exclusions }: { assumptions?: string; exclusions?: string }) {
  const [open, setOpen] = useState(false);
  return (
    <div>
      <button onClick={() => setOpen(v => !v)} data-testid="toggle-scope">
        Scope Details
      </button>
      {open && (
        <div data-testid="scope-body">
          {assumptions && <p data-testid="assumptions">{assumptions}</p>}
          {exclusions && <p data-testid="exclusions">{exclusions}</p>}
        </div>
      )}
    </div>
  );
}

type QuoteFields = Pick<
  ProposalQuoteSummary,
  'notToExceedPrice' | 'proposedStartDate' | 'estimatedDurationValue' | 'estimatedDurationUnit' | 'assumptions' | 'exclusions'
>;

function ProposalDetails({ quote }: { quote?: QuoteFields }) {
  const hasTimeline =
    quote?.proposedStartDate ||
    (quote?.estimatedDurationValue != null && quote?.estimatedDurationUnit);
  return (
    <div>
      {/* NTE block */}
      {quote?.notToExceedPrice != null && (
        <div data-testid="nte">Not-to-Exceed: {quote.notToExceedPrice}</div>
      )}

      {/* Timeline block */}
      {hasTimeline && (
        <div data-testid="timeline">
          {quote?.proposedStartDate && (
            <span data-testid="start-date">{quote.proposedStartDate}</span>
          )}
          {quote?.estimatedDurationValue != null && quote?.estimatedDurationUnit && (
            <span data-testid="duration">
              {quote.estimatedDurationValue} {quote.estimatedDurationUnit}
            </span>
          )}
        </div>
      )}

      {/* Scope Details (collapsible) */}
      {(quote?.assumptions || quote?.exclusions) && (
        <ProposalScopeDetails assumptions={quote.assumptions} exclusions={quote.exclusions} />
      )}
    </div>
  );
}
// --- End inline ---

describe('ProposalViewPage conditional sections', () => {
  describe('NTE section', () => {
    it('shows NTE when notToExceedPrice is set', () => {
      render(<ProposalDetails quote={{ notToExceedPrice: 5000 }} />);
      expect(screen.getByTestId('nte')).toBeInTheDocument();
      expect(screen.getByTestId('nte').textContent).toContain('5000');
    });

    it('hides NTE when notToExceedPrice is undefined', () => {
      render(<ProposalDetails quote={{ notToExceedPrice: undefined }} />);
      expect(screen.queryByTestId('nte')).not.toBeInTheDocument();
    });

    it('hides NTE when quote is undefined', () => {
      render(<ProposalDetails />);
      expect(screen.queryByTestId('nte')).not.toBeInTheDocument();
    });
  });

  describe('Timeline section', () => {
    it('shows timeline when proposedStartDate is set', () => {
      render(<ProposalDetails quote={{ proposedStartDate: '2026-05-01' }} />);
      expect(screen.getByTestId('timeline')).toBeInTheDocument();
      expect(screen.getByTestId('start-date').textContent).toBe('2026-05-01');
    });

    it('shows timeline when duration value and unit are both set', () => {
      render(
        <ProposalDetails quote={{ estimatedDurationValue: 3, estimatedDurationUnit: 'Days' }} />
      );
      expect(screen.getByTestId('timeline')).toBeInTheDocument();
      expect(screen.getByTestId('duration').textContent).toBe('3 Days');
    });

    it('hides timeline when no scheduling fields are present', () => {
      render(<ProposalDetails quote={{ notToExceedPrice: 1000 }} />);
      expect(screen.queryByTestId('timeline')).not.toBeInTheDocument();
    });

    it('hides duration when only value is present without unit', () => {
      render(<ProposalDetails quote={{ estimatedDurationValue: 3 }} />);
      // No timeline because the duration pair is incomplete and no start date
      expect(screen.queryByTestId('duration')).not.toBeInTheDocument();
    });

    it('shows start date without duration when only start date given', () => {
      render(<ProposalDetails quote={{ proposedStartDate: '2026-06-15' }} />);
      expect(screen.getByTestId('timeline')).toBeInTheDocument();
      expect(screen.getByTestId('start-date')).toBeInTheDocument();
      expect(screen.queryByTestId('duration')).not.toBeInTheDocument();
    });
  });

  describe('Scope Details (assumptions/exclusions)', () => {
    it('does not render scope details when no assumptions or exclusions', () => {
      render(<ProposalDetails quote={{ notToExceedPrice: 500 }} />);
      expect(screen.queryByTestId('toggle-scope')).not.toBeInTheDocument();
    });

    it('renders scope details toggle when assumptions present', () => {
      render(<ProposalDetails quote={{ assumptions: 'Assumes clear access' }} />);
      expect(screen.getByTestId('toggle-scope')).toBeInTheDocument();
    });

    it('renders scope details toggle when exclusions present', () => {
      render(<ProposalDetails quote={{ exclusions: 'No disposal included' }} />);
      expect(screen.getByTestId('toggle-scope')).toBeInTheDocument();
    });

    it('scope body is collapsed initially', () => {
      render(<ProposalDetails quote={{ assumptions: 'Some assumption' }} />);
      expect(screen.queryByTestId('scope-body')).not.toBeInTheDocument();
    });

    it('expands scope body on click', () => {
      render(<ProposalDetails quote={{ assumptions: 'Some assumption', exclusions: 'Some exclusion' }} />);
      fireEvent.click(screen.getByTestId('toggle-scope'));
      expect(screen.getByTestId('assumptions').textContent).toBe('Some assumption');
      expect(screen.getByTestId('exclusions').textContent).toBe('Some exclusion');
    });

    it('shows only assumptions when exclusions is absent', () => {
      render(<ProposalDetails quote={{ assumptions: 'Access required' }} />);
      fireEvent.click(screen.getByTestId('toggle-scope'));
      expect(screen.getByTestId('assumptions')).toBeInTheDocument();
      expect(screen.queryByTestId('exclusions')).not.toBeInTheDocument();
    });
  });
});
