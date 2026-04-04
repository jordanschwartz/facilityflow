import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { inboundEmailsApi } from '../api/inboundEmails';
import type { InboundEmailDetail } from '../types';
import { formatDateTime } from '../utils/formatters';
import {
  EnvelopeOpenIcon,
  ArrowDownTrayIcon,
  ChevronDownIcon,
  ChevronUpIcon,
} from '@heroicons/react/24/outline';
import { PaperClipIcon } from '@heroicons/react/24/solid';

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function EmailDetailInline({ emailId }: { emailId: string }) {
  const { data: email, isLoading } = useQuery({
    queryKey: ['inbound-email', emailId],
    queryFn: () => inboundEmailsApi.get(emailId).then(r => r.data),
  });

  if (isLoading) return <div className="px-4 py-3 text-sm text-gray-500">Loading...</div>;
  if (!email) return <div className="px-4 py-3 text-sm text-gray-500">Email not found</div>;

  return (
    <div className="border-t border-gray-100">
      <div className="px-4 py-3">
        {email.bodyHtml ? (
          <iframe
            srcDoc={email.bodyHtml}
            title="Email body"
            className="w-full border-0 min-h-[200px]"
            sandbox="allow-same-origin"
            onLoad={(e) => {
              const iframe = e.target as HTMLIFrameElement;
              if (iframe.contentDocument) {
                iframe.style.height = iframe.contentDocument.body.scrollHeight + 'px';
              }
            }}
          />
        ) : email.bodyText ? (
          <pre className="text-sm text-gray-700 whitespace-pre-wrap font-sans">{email.bodyText}</pre>
        ) : (
          <p className="text-sm text-gray-400 italic">No email body</p>
        )}
      </div>

      {email.attachments.length > 0 && (
        <div className="px-4 py-3 border-t border-gray-100 bg-gray-50">
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
            Attachments ({email.attachments.length})
          </p>
          <div className="flex flex-wrap gap-2">
            {email.attachments.map(att => (
              <a
                key={att.id}
                href={inboundEmailsApi.getAttachmentDownloadUrl(email.id, att.id)}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 bg-white hover:bg-gray-50 text-xs text-gray-700"
              >
                <ArrowDownTrayIcon className="w-3.5 h-3.5 text-gray-400" />
                <span className="truncate max-w-[140px]">{att.fileName}</span>
                <span className="text-gray-400">({formatFileSize(att.fileSize)})</span>
              </a>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default function EmailList({ serviceRequestId }: { serviceRequestId: string }) {
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['inbound-emails', serviceRequestId, page],
    queryFn: () => inboundEmailsApi.list(serviceRequestId, page, 20).then(r => r.data),
  });

  const emails = data?.items ?? [];
  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 1;

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
      <div className="px-6 py-4 border-b border-gray-200">
        <h2 className="text-base font-semibold text-gray-900">Emails</h2>
        {data && <p className="text-xs text-gray-500 mt-0.5">{data.totalCount} email{data.totalCount !== 1 ? 's' : ''}</p>}
      </div>

      {isLoading ? (
        <div className="px-6 py-8 text-center text-sm text-gray-500">Loading emails...</div>
      ) : emails.length === 0 ? (
        <div className="px-6 py-8 text-center">
          <EnvelopeOpenIcon className="w-10 h-10 text-gray-300 mx-auto mb-2" />
          <p className="text-sm text-gray-500">No emails received yet</p>
        </div>
      ) : (
        <div className="divide-y divide-gray-100">
          {emails.map(email => {
            const isExpanded = expandedId === email.id;
            return (
              <div key={email.id}>
                <button
                  onClick={() => setExpandedId(isExpanded ? null : email.id)}
                  className="w-full px-4 py-3 text-left hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-start gap-3">
                    <div className="w-8 h-8 rounded-full bg-sky-100 text-sky-700 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <EnvelopeOpenIcon className="w-4 h-4" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-sm font-medium text-gray-900">
                          {email.fromName || email.fromAddress}
                        </span>
                        {email.attachmentCount > 0 && (
                          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-gray-100 text-gray-600">
                            <PaperClipIcon className="w-3 h-3" />
                            {email.attachmentCount}
                          </span>
                        )}
                        <span className="text-xs text-gray-400 ml-auto flex-shrink-0">{formatDateTime(email.receivedAt)}</span>
                      </div>
                      <p className="text-sm font-medium text-gray-800 mt-0.5 truncate">{email.subject}</p>
                      {email.bodyPreview && (
                        <p className="text-sm text-gray-500 mt-0.5 line-clamp-1">{email.bodyPreview}</p>
                      )}
                    </div>
                    <div className="flex-shrink-0 mt-1 text-gray-400">
                      {isExpanded ? <ChevronUpIcon className="w-4 h-4" /> : <ChevronDownIcon className="w-4 h-4" />}
                    </div>
                  </div>
                </button>
                {isExpanded && <EmailDetailInline emailId={email.id} />}
              </div>
            );
          })}
        </div>
      )}

      {totalPages > 1 && (
        <div className="px-6 py-3 border-t border-gray-200 flex items-center justify-between">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="text-xs font-medium text-gray-600 hover:text-gray-900 disabled:text-gray-300 disabled:cursor-not-allowed"
          >
            Previous
          </button>
          <span className="text-xs text-gray-500">Page {page} of {totalPages}</span>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="text-xs font-medium text-gray-600 hover:text-gray-900 disabled:text-gray-300 disabled:cursor-not-allowed"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
