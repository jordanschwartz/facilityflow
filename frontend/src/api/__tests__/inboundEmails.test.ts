import { describe, it, expect, vi, beforeEach } from 'vitest';

// Mock the axios client
vi.mock('../client', () => {
  const mockClient = {
    get: vi.fn().mockResolvedValue({ data: {} }),
    post: vi.fn().mockResolvedValue({ data: { quoteId: 'q-1' } }),
  };
  return { default: mockClient };
});

import apiClient from '../client';
import { inboundEmailsApi } from '../inboundEmails';

describe('inboundEmailsApi actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('createQuoteFromEmail() calls correct endpoint', async () => {
    await inboundEmailsApi.createQuoteFromEmail('email-1');
    expect(apiClient.post).toHaveBeenCalledWith('/inbound-emails/email-1/actions/create-quote');
  });

  it('attachAsPo() calls correct endpoint with attachmentId', async () => {
    await inboundEmailsApi.attachAsPo('email-1', 'att-1');
    expect(apiClient.post).toHaveBeenCalledWith(
      '/inbound-emails/email-1/actions/attach-as-po',
      { attachmentId: 'att-1' }
    );
  });

  it('addToNotes() calls correct endpoint', async () => {
    await inboundEmailsApi.addToNotes('email-1');
    expect(apiClient.post).toHaveBeenCalledWith('/inbound-emails/email-1/actions/add-to-notes');
  });

  it('list() calls correct URL with params', async () => {
    await inboundEmailsApi.list('sr-1', 1, 20);
    expect(apiClient.get).toHaveBeenCalledWith(
      '/service-requests/sr-1/emails',
      { params: { page: 1, pageSize: 20 } }
    );
  });

  it('get() calls correct URL', async () => {
    await inboundEmailsApi.get('email-1');
    expect(apiClient.get).toHaveBeenCalledWith('/inbound-emails/email-1');
  });

  it('getAttachmentDownloadUrl() returns correct URL', () => {
    const url = inboundEmailsApi.getAttachmentDownloadUrl('email-1', 'att-1');
    expect(url).toContain('/inbound-emails/email-1/attachments/att-1');
  });
});
