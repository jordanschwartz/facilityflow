import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { inboundEmailsApi } from '../api/inboundEmails';
import { outboundEmailsApi } from '../api/outboundEmails';
import { emailConversationsApi } from '../api/emailConversations';
import type { InboundEmail, OutboundEmail, EmailConversation, OutboundEmailDetail } from '../types';
import { formatDateTime, formatDate } from '../utils/formatters';
import {
  EnvelopeOpenIcon,
  ArrowDownTrayIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  PaperAirplaneIcon,
  MagnifyingGlassIcon,
  ChatBubbleLeftRightIcon,
} from '@heroicons/react/24/outline';
import { PaperClipIcon } from '@heroicons/react/24/solid';
import EmailActionMenu from './EmailActionMenu';
import ForwardEmailModal from './ForwardEmailModal';

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

type DirectionFilter = 'all' | 'inbound' | 'outbound';
type ViewMode = 'all' | 'conversations';

interface UnifiedEmail {
  id: string;
  direction: 'inbound' | 'outbound';
  fromAddress: string;
  fromName?: string;
  toAddress?: string;
  toName?: string;
  subject: string;
  bodyPreview?: string;
  timestamp: string;
  attachmentCount: number;
  sentByName?: string;
  recipientAddress?: string;
  recipientName?: string;
}

function InboundEmailDetailInline({ emailId }: { emailId: string }) {
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

function OutboundEmailDetailInline({ emailId }: { emailId: string }) {
  const { data: email, isLoading } = useQuery({
    queryKey: ['outbound-email', emailId],
    queryFn: () => outboundEmailsApi.get(emailId).then(r => r.data),
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
                href={outboundEmailsApi.getAttachmentDownloadUrl(email.id, att.id)}
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

function ConversationCard({ conversation }: { conversation: EmailConversation }) {
  const [expanded, setExpanded] = useState(false);
  const [expandedEmailId, setExpandedEmailId] = useState<string | null>(null);
  const [forwardEmail, setForwardEmail] = useState<{ id: string; subject: string } | null>(null);

  const emails = conversation.emails;
  const firstDate = emails.length > 0 ? formatDate(emails[0].timestamp) : '';
  const lastDate = emails.length > 0 ? formatDate(emails[emails.length - 1].timestamp) : '';
  const dateRange = firstDate === lastDate ? firstDate : `${firstDate} – ${lastDate}`;

  return (
    <>
      <div className="border border-gray-200 rounded-lg overflow-hidden">
        <button
          onClick={() => setExpanded(!expanded)}
          className="w-full px-4 py-3 text-left hover:bg-gray-50 transition-colors"
        >
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-purple-100 text-purple-700 flex items-center justify-center flex-shrink-0">
              <ChatBubbleLeftRightIcon className="w-4 h-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-gray-900 truncate">{conversation.subject}</p>
              <p className="text-xs text-gray-500">
                {conversation.emailCount} email{conversation.emailCount !== 1 ? 's' : ''} &middot; {dateRange}
              </p>
            </div>
            <div className="flex-shrink-0 text-gray-400">
              {expanded ? <ChevronUpIcon className="w-4 h-4" /> : <ChevronDownIcon className="w-4 h-4" />}
            </div>
          </div>
        </button>

        {expanded && (
          <div className="border-t border-gray-100">
            {emails.map((email, idx) => {
              const isInbound = email.type === 'inbound';
              const isExpanded = expandedEmailId === email.id;

              return (
                <div key={email.id} className={`px-4 py-3 ${idx > 0 ? 'border-t border-gray-50' : ''}`}>
                  <div className={`flex items-start gap-3 ${idx > 0 ? 'ml-4' : ''}`}>
                    {/* Left indicator line for threading */}
                    {idx > 0 && (
                      <div className="w-0.5 h-full bg-gray-200 -ml-4 mr-3 flex-shrink-0" />
                    )}
                    <div className={`w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 ${
                      isInbound ? 'bg-sky-100 text-sky-700' : 'bg-blue-100 text-blue-700'
                    }`}>
                      {isInbound ? <EnvelopeOpenIcon className="w-3.5 h-3.5" /> : <PaperAirplaneIcon className="w-3.5 h-3.5" />}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-sm font-medium text-gray-900">
                          {isInbound
                            ? `From: ${email.fromName || email.fromAddress}`
                            : `To: ${email.toName || email.toAddress}`}
                        </span>
                        <span className={`inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium ${
                          isInbound ? 'bg-sky-50 text-sky-700' : 'bg-blue-50 text-blue-700'
                        }`}>
                          {isInbound ? 'Received' : 'Sent'}
                        </span>
                        {email.attachmentCount > 0 && (
                          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-gray-100 text-gray-600">
                            <PaperClipIcon className="w-3 h-3" />
                            {email.attachmentCount}
                          </span>
                        )}
                        <span className="text-xs text-gray-400 ml-auto">{formatDateTime(email.timestamp)}</span>
                        <EmailActionMenu
                          type={email.type}
                          emailId={email.id}
                          attachmentCount={email.attachmentCount}
                          recipientAddress={email.toAddress}
                          onForward={() => setForwardEmail({ id: email.id, subject: email.subject })}
                        />
                      </div>
                      {email.bodyPreview && (
                        <p className="text-sm text-gray-500 mt-0.5 line-clamp-2">{email.bodyPreview}</p>
                      )}
                      <button
                        onClick={() => setExpandedEmailId(isExpanded ? null : email.id)}
                        className="text-xs text-brand-600 hover:text-brand-700 mt-1 font-medium"
                      >
                        {isExpanded ? 'Hide' : 'View full email'}
                      </button>
                      {isExpanded && (
                        isInbound
                          ? <InboundEmailDetailInline emailId={email.id} />
                          : <OutboundEmailDetailInline emailId={email.id} />
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {forwardEmail && (
        <ForwardEmailModal
          open={!!forwardEmail}
          onClose={() => setForwardEmail(null)}
          emailId={forwardEmail.id}
          emailSubject={forwardEmail.subject}
          type="outbound"
        />
      )}
    </>
  );
}

export default function EmailList({ serviceRequestId }: { serviceRequestId: string }) {
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode>('all');
  const [directionFilter, setDirectionFilter] = useState<DirectionFilter>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [forwardEmail, setForwardEmail] = useState<{ id: string; subject: string; recipientAddress: string } | null>(null);

  const { data: inboundData, isLoading: inboundLoading } = useQuery({
    queryKey: ['inbound-emails', serviceRequestId, page],
    queryFn: () => inboundEmailsApi.list(serviceRequestId, page, 50).then(r => r.data),
  });

  const { data: outboundData, isLoading: outboundLoading } = useQuery({
    queryKey: ['outbound-emails', serviceRequestId, page],
    queryFn: () => outboundEmailsApi.list(serviceRequestId, page, 50).then(r => r.data),
  });

  const { data: conversations, isLoading: convoLoading } = useQuery({
    queryKey: ['email-conversations', serviceRequestId],
    queryFn: () => emailConversationsApi.getConversations(serviceRequestId).then(r => r.data),
    enabled: viewMode === 'conversations',
  });

  const isLoading = inboundLoading || outboundLoading;

  // Merge inbound + outbound into unified list
  const allEmails = useMemo((): UnifiedEmail[] => {
    const emails: UnifiedEmail[] = [];

    (inboundData?.items ?? []).forEach(e => {
      emails.push({
        id: e.id,
        direction: 'inbound',
        fromAddress: e.fromAddress,
        fromName: e.fromName,
        subject: e.subject,
        bodyPreview: e.bodyPreview,
        timestamp: e.receivedAt,
        attachmentCount: e.attachmentCount,
      });
    });

    (outboundData?.items ?? []).forEach(e => {
      emails.push({
        id: e.id,
        direction: 'outbound',
        fromAddress: '',
        toAddress: e.recipientAddress,
        toName: e.recipientName,
        subject: e.subject,
        bodyPreview: e.bodyPreview,
        timestamp: e.sentAt,
        attachmentCount: e.attachmentCount,
        sentByName: e.sentByName,
        recipientAddress: e.recipientAddress,
        recipientName: e.recipientName,
      });
    });

    emails.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
    return emails;
  }, [inboundData, outboundData]);

  // Apply filters
  const filteredEmails = useMemo(() => {
    let result = allEmails;

    if (directionFilter !== 'all') {
      result = result.filter(e => e.direction === directionFilter);
    }

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter(e =>
        e.subject.toLowerCase().includes(q) ||
        (e.fromName || '').toLowerCase().includes(q) ||
        e.fromAddress.toLowerCase().includes(q) ||
        (e.toAddress || '').toLowerCase().includes(q) ||
        (e.toName || '').toLowerCase().includes(q) ||
        (e.recipientAddress || '').toLowerCase().includes(q)
      );
    }

    return result;
  }, [allEmails, directionFilter, searchQuery]);

  const totalCount = (inboundData?.totalCount ?? 0) + (outboundData?.totalCount ?? 0);

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
      <div className="px-6 py-4 border-b border-gray-200">
        <div className="flex items-center justify-between mb-3">
          <div>
            <h2 className="text-base font-semibold text-gray-900">Emails</h2>
            {!isLoading && <p className="text-xs text-gray-500 mt-0.5">{totalCount} email{totalCount !== 1 ? 's' : ''}</p>}
          </div>

          {/* View toggle */}
          <div className="flex bg-gray-100 rounded-lg p-0.5">
            <button
              onClick={() => setViewMode('all')}
              className={`px-3 py-1 text-xs font-medium rounded-md transition-colors ${
                viewMode === 'all' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              All Emails
            </button>
            <button
              onClick={() => setViewMode('conversations')}
              className={`px-3 py-1 text-xs font-medium rounded-md transition-colors ${
                viewMode === 'conversations' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              Conversations
            </button>
          </div>
        </div>

        {/* Filters (only in All Emails view) */}
        {viewMode === 'all' && (
          <div className="flex items-center gap-3">
            {/* Direction filter */}
            <div className="flex gap-1">
              {(['all', 'inbound', 'outbound'] as DirectionFilter[]).map(dir => (
                <button
                  key={dir}
                  onClick={() => setDirectionFilter(dir)}
                  className={`px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
                    directionFilter === dir
                      ? 'bg-brand-100 text-brand-700'
                      : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                  }`}
                >
                  {dir === 'all' ? 'All' : dir === 'inbound' ? 'Inbound' : 'Outbound'}
                </button>
              ))}
            </div>

            {/* Search */}
            <div className="relative flex-1 max-w-xs">
              <MagnifyingGlassIcon className="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search emails..."
                className="w-full pl-8 pr-3 py-1.5 border border-gray-300 rounded-lg text-xs focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
          </div>
        )}
      </div>

      {/* All Emails View */}
      {viewMode === 'all' && (
        <>
          {isLoading ? (
            <div className="px-6 py-8 text-center text-sm text-gray-500">Loading emails...</div>
          ) : filteredEmails.length === 0 ? (
            <div className="px-6 py-8 text-center">
              <EnvelopeOpenIcon className="w-10 h-10 text-gray-300 mx-auto mb-2" />
              <p className="text-sm text-gray-500">
                {searchQuery || directionFilter !== 'all' ? 'No emails match your filters' : 'No emails yet'}
              </p>
            </div>
          ) : (
            <div className="divide-y divide-gray-100">
              {filteredEmails.map(email => {
                const isExpanded = expandedId === email.id;
                const isInbound = email.direction === 'inbound';

                return (
                  <div key={`${email.direction}-${email.id}`}>
                    <div className="flex items-start px-4 py-3 hover:bg-gray-50 transition-colors">
                      <button
                        onClick={() => setExpandedId(isExpanded ? null : email.id)}
                        className="flex-1 text-left flex items-start gap-3"
                      >
                        <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5 ${
                          isInbound ? 'bg-sky-100 text-sky-700' : 'bg-blue-100 text-blue-700'
                        }`}>
                          {isInbound ? <EnvelopeOpenIcon className="w-4 h-4" /> : <PaperAirplaneIcon className="w-4 h-4" />}
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-sm font-medium text-gray-900">
                              {isInbound
                                ? (email.fromName || email.fromAddress)
                                : `To: ${email.recipientName || email.recipientAddress}`}
                            </span>
                            <span className={`inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium ${
                              isInbound ? 'bg-sky-50 text-sky-700' : 'bg-blue-50 text-blue-700'
                            }`}>
                              {isInbound ? 'Received' : 'Sent'}
                            </span>
                            {email.attachmentCount > 0 && (
                              <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-gray-100 text-gray-600">
                                <PaperClipIcon className="w-3 h-3" />
                                {email.attachmentCount}
                              </span>
                            )}
                            <span className="text-xs text-gray-400 ml-auto flex-shrink-0">{formatDateTime(email.timestamp)}</span>
                          </div>
                          <p className="text-sm font-medium text-gray-800 mt-0.5 truncate">{email.subject}</p>
                          {email.bodyPreview && (
                            <p className="text-sm text-gray-500 mt-0.5 line-clamp-1">{email.bodyPreview}</p>
                          )}
                          {!isInbound && email.sentByName && (
                            <p className="text-xs text-gray-400 mt-0.5">Sent by {email.sentByName}</p>
                          )}
                        </div>
                      </button>
                      <div className="flex items-center gap-1 flex-shrink-0 mt-1">
                        <EmailActionMenu
                          type={email.direction}
                          emailId={email.id}
                          attachmentCount={email.attachmentCount}
                          recipientAddress={email.recipientAddress}
                          onForward={() => !isInbound && setForwardEmail({
                            id: email.id,
                            subject: email.subject,
                            recipientAddress: email.recipientAddress!,
                          })}
                        />
                        <button
                          onClick={() => setExpandedId(isExpanded ? null : email.id)}
                          className="p-1 text-gray-400"
                        >
                          {isExpanded ? <ChevronUpIcon className="w-4 h-4" /> : <ChevronDownIcon className="w-4 h-4" />}
                        </button>
                      </div>
                    </div>
                    {isExpanded && (
                      isInbound
                        ? <InboundEmailDetailInline emailId={email.id} />
                        : <OutboundEmailDetailInline emailId={email.id} />
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}

      {/* Conversations View */}
      {viewMode === 'conversations' && (
        <>
          {convoLoading ? (
            <div className="px-6 py-8 text-center text-sm text-gray-500">Loading conversations...</div>
          ) : !conversations || conversations.length === 0 ? (
            <div className="px-6 py-8 text-center">
              <ChatBubbleLeftRightIcon className="w-10 h-10 text-gray-300 mx-auto mb-2" />
              <p className="text-sm text-gray-500">No email conversations yet</p>
            </div>
          ) : (
            <div className="p-4 space-y-3">
              {conversations.map(convo => (
                <ConversationCard key={convo.conversationId} conversation={convo} />
              ))}
            </div>
          )}
        </>
      )}

      {/* Forward email modal */}
      {forwardEmail && (
        <ForwardEmailModal
          open={!!forwardEmail}
          onClose={() => setForwardEmail(null)}
          emailId={forwardEmail.id}
          emailSubject={forwardEmail.subject}
          type="outbound"
        />
      )}
    </div>
  );
}
