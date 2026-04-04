import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import EmailList from '../EmailList';

// Mock the API modules
vi.mock('../../api/inboundEmails', () => ({
  inboundEmailsApi: {
    list: vi.fn().mockResolvedValue({
      data: {
        items: [
          {
            id: 'inbound-1',
            serviceRequestId: 'sr-1',
            fromAddress: 'vendor@example.com',
            fromName: 'Vendor Bob',
            subject: 'Re: Work Order #123',
            bodyPreview: 'Here is my quote...',
            receivedAt: '2026-04-03T10:00:00Z',
            attachmentCount: 2,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 50,
      },
    }),
    get: vi.fn().mockResolvedValue({ data: {} }),
    getAttachmentDownloadUrl: vi.fn().mockReturnValue('/download'),
  },
}));

vi.mock('../../api/outboundEmails', () => ({
  outboundEmailsApi: {
    list: vi.fn().mockResolvedValue({
      data: {
        items: [
          {
            id: 'outbound-1',
            serviceRequestId: 'sr-1',
            recipientAddress: 'vendor@example.com',
            recipientName: 'Vendor Bob',
            subject: 'Work Order #123',
            bodyPreview: 'Please see attached work order...',
            sentAt: '2026-04-03T09:00:00Z',
            sentByName: 'Admin User',
            emailType: 'WorkOrder',
            attachmentCount: 1,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 50,
      },
    }),
    get: vi.fn().mockResolvedValue({ data: {} }),
    getAttachmentDownloadUrl: vi.fn().mockReturnValue('/download'),
    resend: vi.fn().mockResolvedValue({}),
    forward: vi.fn().mockResolvedValue({}),
  },
}));

vi.mock('../../api/emailConversations', () => ({
  emailConversationsApi: {
    getConversations: vi.fn().mockResolvedValue({
      data: [
        {
          conversationId: 'conv-1',
          subject: 'Work Order #123',
          latestEmailAt: '2026-04-03T10:00:00Z',
          emailCount: 2,
          emails: [
            {
              id: 'outbound-1',
              type: 'outbound',
              fromAddress: 'admin@facilityflow.com',
              toAddress: 'vendor@example.com',
              toName: 'Vendor Bob',
              subject: 'Work Order #123',
              bodyPreview: 'Please see attached...',
              timestamp: '2026-04-03T09:00:00Z',
              attachmentCount: 1,
            },
            {
              id: 'inbound-1',
              type: 'inbound',
              fromAddress: 'vendor@example.com',
              fromName: 'Vendor Bob',
              subject: 'Re: Work Order #123',
              bodyPreview: 'Here is my quote...',
              timestamp: '2026-04-03T10:00:00Z',
              attachmentCount: 2,
            },
          ],
        },
      ],
    }),
  },
}));

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('EmailList', () => {
  it('renders both inbound and outbound emails', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByText('Vendor Bob')).toBeInTheDocument();
    });

    expect(screen.getByText('Re: Work Order #123')).toBeInTheDocument();
    expect(screen.getByText('To: Vendor Bob')).toBeInTheDocument();
    expect(screen.getByText('Work Order #123')).toBeInTheDocument();
  });

  it('shows total email count', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByText('2 emails')).toBeInTheDocument();
    });
  });

  it('shows direction filter toggles', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByText('All')).toBeInTheDocument();
    });
    expect(screen.getByText('Inbound')).toBeInTheDocument();
    expect(screen.getByText('Outbound')).toBeInTheDocument();
  });

  it('filters by direction when toggle clicked', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByText('Vendor Bob')).toBeInTheDocument();
    });

    // Click "Inbound" filter
    fireEvent.click(screen.getByText('Inbound'));

    // Outbound email should no longer be visible
    await waitFor(() => {
      expect(screen.queryByText('To: Vendor Bob')).not.toBeInTheDocument();
    });

    // Inbound email should still be visible
    expect(screen.getByText('Vendor Bob')).toBeInTheDocument();
  });

  it('switches to conversations view', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByText('Conversations')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Conversations'));

    await waitFor(() => {
      // Conversation subject should appear
      expect(screen.getByText('Work Order #123')).toBeInTheDocument();
      // Conversation card shows email count with date range
      expect(screen.getByText(/2 emails · Apr 3, 2026/)).toBeInTheDocument();
    });
  });

  it('shows search input in all emails view', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Search emails...')).toBeInTheDocument();
    });
  });

  it('filters emails by search query', async () => {
    renderWithProviders(<EmailList serviceRequestId="sr-1" />);

    await waitFor(() => {
      expect(screen.getByText('Re: Work Order #123')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Search emails...');
    fireEvent.change(searchInput, { target: { value: 'Re: Work' } });

    await waitFor(() => {
      expect(screen.getByText('Re: Work Order #123')).toBeInTheDocument();
      // The outbound email "Work Order #123" also matches "Re: Work" partially...
      // Actually "Work Order #123" does NOT contain "Re: Work" so it should be filtered out
      expect(screen.queryByText('To: Vendor Bob')).not.toBeInTheDocument();
    });
  });
});
