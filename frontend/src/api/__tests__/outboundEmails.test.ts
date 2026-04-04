import { describe, it, expect, vi, beforeEach } from 'vitest';

// Mock the axios client
vi.mock('../client', () => {
  const mockClient = {
    get: vi.fn().mockResolvedValue({ data: {} }),
    post: vi.fn().mockResolvedValue({ data: {} }),
  };
  return { default: mockClient };
});

import apiClient from '../client';
import { outboundEmailsApi } from '../outboundEmails';

describe('outboundEmailsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('list() calls correct URL with params', async () => {
    await outboundEmailsApi.list('sr-123', 2, 10);
    expect(apiClient.get).toHaveBeenCalledWith(
      '/service-requests/sr-123/outbound-emails',
      { params: { page: 2, pageSize: 10 } }
    );
  });

  it('list() works without optional params', async () => {
    await outboundEmailsApi.list('sr-456');
    expect(apiClient.get).toHaveBeenCalledWith(
      '/service-requests/sr-456/outbound-emails',
      { params: { page: undefined, pageSize: undefined } }
    );
  });

  it('get() calls correct URL', async () => {
    await outboundEmailsApi.get('email-1');
    expect(apiClient.get).toHaveBeenCalledWith('/outbound-emails/email-1');
  });

  it('getAttachmentDownloadUrl() returns correct URL', () => {
    const url = outboundEmailsApi.getAttachmentDownloadUrl('email-1', 'att-1');
    expect(url).toContain('/outbound-emails/email-1/attachments/att-1');
  });

  it('resend() calls correct endpoint', async () => {
    await outboundEmailsApi.resend('email-1');
    expect(apiClient.post).toHaveBeenCalledWith('/outbound-emails/email-1/actions/resend');
  });

  it('forward() calls correct endpoint with body', async () => {
    await outboundEmailsApi.forward('email-1', 'test@example.com', 'John');
    expect(apiClient.post).toHaveBeenCalledWith(
      '/outbound-emails/email-1/actions/forward',
      { recipientEmail: 'test@example.com', recipientName: 'John' }
    );
  });

  it('forward() works without optional recipientName', async () => {
    await outboundEmailsApi.forward('email-1', 'test@example.com');
    expect(apiClient.post).toHaveBeenCalledWith(
      '/outbound-emails/email-1/actions/forward',
      { recipientEmail: 'test@example.com', recipientName: undefined }
    );
  });
});
