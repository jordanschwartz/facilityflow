import { useState } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { serviceRequestsApi } from '../../api/serviceRequests';
import { vendorsApi } from '../../api/vendors';
import { quotesApi } from '../../api/quotes';
import FindVendorsModal from '../../components/vendors/FindVendorsModal';
import type { VendorSourcingResult, Quote } from '../../types';
import { proposalsApi } from '../../api/proposals';
import { commentsApi } from '../../api/comments';
import ProposalBuilder from '../../components/proposals/ProposalBuilder';
import ProposalDetail from '../../components/proposals/ProposalDetail';
import type { ServiceRequestStatus } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import Button from '../../components/ui/Button';
import Modal from '../../components/ui/Modal';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatCurrency, formatRelativeTime } from '../../utils/formatters';
import { useAuthStore } from '../../stores/authStore';

const API_BASE = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

function QuoteDetailModal({ quote, open, onClose }: { quote: Quote; open: boolean; onClose: () => void }) {
  const hasLineItems = quote.lineItems && quote.lineItems.length > 0;
  const hasAttachments = quote.attachments && quote.attachments.length > 0;

  return (
    <Modal open={open} onClose={onClose} title={`Quote — ${quote.vendor?.companyName}`} size="lg">
      <div className="space-y-5 max-h-[70vh] overflow-y-auto pr-1">

        {/* Price */}
        <div className="flex items-center gap-6">
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Price</p>
            <p className="text-xl font-bold text-gray-900">{formatCurrency(quote.price)}</p>
          </div>
          {quote.notToExceedPrice != null && (
            <div>
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">NTE</p>
              <p className="text-xl font-bold text-gray-900">{formatCurrency(quote.notToExceedPrice)}</p>
            </div>
          )}
        </div>

        {/* Scope */}
        {quote.scopeOfWork && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Scope of Work</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{quote.scopeOfWork}</p>
          </div>
        )}

        {/* Scheduling / Meta */}
        {(quote.proposedStartDate || quote.estimatedDurationValue != null || quote.vendorAvailability || quote.validUntil || quote.submittedAt) && (
          <div className="grid grid-cols-2 gap-3">
            {quote.submittedAt && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Submitted</p>
                <p className="text-sm text-gray-700">{formatDate(quote.submittedAt)}</p>
              </div>
            )}
            {quote.proposedStartDate && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Proposed Start</p>
                <p className="text-sm text-gray-700">{formatDate(quote.proposedStartDate)}</p>
              </div>
            )}
            {quote.estimatedDurationValue != null && quote.estimatedDurationUnit && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Duration</p>
                <p className="text-sm text-gray-700">{quote.estimatedDurationValue} {quote.estimatedDurationUnit}</p>
              </div>
            )}
            {quote.vendorAvailability && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Availability</p>
                <p className="text-sm text-gray-700">{quote.vendorAvailability}</p>
              </div>
            )}
            {quote.validUntil && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Valid Until</p>
                <p className="text-sm text-gray-700">{formatDate(quote.validUntil)}</p>
              </div>
            )}
          </div>
        )}

        {/* Line Items */}
        {hasLineItems && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Line Items</p>
            <table className="min-w-full text-sm">
              <thead>
                <tr className="text-xs text-gray-500 border-b border-gray-200">
                  <th className="text-left font-medium pb-1 pr-4">Description</th>
                  <th className="text-left font-medium pb-1 pr-4">Qty</th>
                  <th className="text-left font-medium pb-1 pr-4">Unit Price</th>
                  <th className="text-right font-medium pb-1">Total</th>
                </tr>
              </thead>
              <tbody>
                {quote.lineItems.map(li => (
                  <tr key={li.id} className="border-t border-gray-100">
                    <td className="py-1.5 pr-4 text-gray-700">{li.description}</td>
                    <td className="py-1.5 pr-4 text-gray-700">{li.quantity}</td>
                    <td className="py-1.5 pr-4 text-gray-700">{formatCurrency(li.unitPrice)}</td>
                    <td className="py-1.5 text-right text-gray-900 font-medium">{formatCurrency(li.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Assumptions / Exclusions */}
        {(quote.assumptions || quote.exclusions) && (
          <div className="grid grid-cols-2 gap-4">
            {quote.assumptions && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Assumptions</p>
                <p className="text-sm text-gray-700 whitespace-pre-wrap">{quote.assumptions}</p>
              </div>
            )}
            {quote.exclusions && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Exclusions</p>
                <p className="text-sm text-gray-700 whitespace-pre-wrap">{quote.exclusions}</p>
              </div>
            )}
          </div>
        )}

        {/* Attachments */}
        {hasAttachments && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Attachments</p>
            <div className="flex flex-wrap gap-2">
              {quote.attachments.map(a => {
                const url = `${API_BASE}${a.url}`;
                if (a.mimeType.startsWith('image/')) {
                  return (
                    <a key={a.id} href={url} target="_blank" rel="noopener noreferrer" title={a.filename}>
                      <img src={url} alt={a.filename} className="w-24 h-24 object-cover rounded-lg border border-gray-200 hover:opacity-80 transition-opacity" />
                    </a>
                  );
                }
                if (a.mimeType.startsWith('video/')) {
                  return (
                    <a key={a.id} href={url} target="_blank" rel="noopener noreferrer" title={a.filename}
                      className="flex flex-col items-center justify-center w-24 h-24 rounded-lg border border-gray-200 bg-gray-50 hover:bg-gray-100 transition-colors text-gray-500 gap-1">
                      <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                      <span className="text-xs truncate w-full text-center px-1">{a.filename}</span>
                    </a>
                  );
                }
                return (
                  <a key={a.id} href={url} target="_blank" rel="noopener noreferrer" title={a.filename}
                    className="flex flex-col items-center justify-center w-24 h-24 rounded-lg border border-gray-200 bg-red-50 hover:bg-red-100 transition-colors text-red-500 gap-1">
                    <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6zm-1 1.5L18.5 9H13V3.5zM6 20V4h5v7h7v9H6z"/></svg>
                    <span className="text-xs truncate w-full text-center px-1">{a.filename}</span>
                  </a>
                );
              })}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
}

function QuoteCard({ quote, isOperator, selectQuote }: {
  quote: Quote;
  isOperator: boolean;
  selectQuote: { mutate: (id: string) => void; isPending: boolean };
}) {
  const [detailOpen, setDetailOpen] = useState(false);

  return (
    <div className="border border-gray-200 rounded-xl bg-white shadow-sm p-4">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3 min-w-0">
          <span className="text-sm font-medium text-gray-900">{quote.vendor?.companyName}</span>
          <StatusBadge status={quote.status} />
          {quote.attachments?.length > 0 && (
            <span className="text-xs text-gray-400">{quote.attachments.length} attachment{quote.attachments.length !== 1 ? 's' : ''}</span>
          )}
        </div>
        <div className="flex items-center gap-3 flex-shrink-0">
          <div className="text-right">
            <p className="text-sm font-bold text-gray-900">{formatCurrency(quote.price)}</p>
            {quote.notToExceedPrice != null && (
              <p className="text-xs text-gray-400">NTE {formatCurrency(quote.notToExceedPrice)}</p>
            )}
          </div>
          {quote.submittedAt && (
            <p className="text-xs text-gray-400 hidden sm:block">{formatDate(quote.submittedAt)}</p>
          )}
        </div>
      </div>

      {isOperator && (
        <div className="mt-3 flex items-center gap-2 justify-end">
          <Button size="sm" variant="secondary" onClick={() => setDetailOpen(true)}>
            View
          </Button>
          {quote.publicToken && quote.status === 'Requested' && (
            <Button
              size="sm"
              variant="secondary"
              onClick={() => {
                navigator.clipboard.writeText(`${window.location.origin}/quotes/submit/${quote.publicToken}`);
                toast.success('Link copied to clipboard');
              }}
            >
              Copy Link
            </Button>
          )}
          {quote.status === 'Submitted' && (
            <Button size="sm" onClick={() => selectQuote.mutate(quote.id)} loading={selectQuote.isPending}>
              Select
            </Button>
          )}
        </div>
      )}

      <QuoteDetailModal quote={quote} open={detailOpen} onClose={() => setDetailOpen(false)} />
    </div>
  );
}

function QuotesTab({ quotes, isOperator, selectQuote }: {
  quotes: Quote[];
  isOperator: boolean;
  selectQuote: { mutate: (id: string) => void; isPending: boolean };
}) {
  return (
    <div>
      <h2 className="text-base font-semibold text-gray-900 mb-4">Quotes Received</h2>
      {quotes.length === 0 ? (
        <EmptyState title="No quotes yet" description="Quotes will appear here as vendors submit them" />
      ) : (
        <div className="space-y-4">
          {quotes.map(quote => (
            <QuoteCard key={quote.id} quote={quote} isOperator={isOperator} selectQuote={selectQuote} />
          ))}
        </div>
      )}
    </div>
  );
}

const STATUSES: ServiceRequestStatus[] = ['New', 'Sourcing', 'Quoting', 'PendingApproval', 'Approved', 'Rejected', 'Completed'];
const TABS = ['overview', 'vendors', 'quotes', 'proposal', 'workorder'] as const;
type Tab = typeof TABS[number];

const commentSchema = z.object({ text: z.string().min(1, 'Comment cannot be empty') });
type CommentForm = z.infer<typeof commentSchema>;

export default function RequestDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const isOperator = user?.role === 'Operator';

  const activeTab = (searchParams.get('tab') as Tab) ?? 'overview';
  const setTab = (tab: Tab) => setSearchParams({ tab });

  const [inviteModalOpen, setInviteModalOpen] = useState(false);
  const [findVendorsOpen, setFindVendorsOpen] = useState(false);
  const [vendorSearch, setVendorSearch] = useState('');
  const [selectedVendors, setSelectedVendors] = useState<string[]>([]);
  const [proposalEditMode, setProposalEditMode] = useState(false);
  const [selectedQuoteIdForProposal, setSelectedQuoteIdForProposal] = useState<string | null>(null);

  const { data: sr, isLoading } = useQuery({
    queryKey: ['service-requests', id],
    queryFn: () => serviceRequestsApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: invites } = useQuery({
    queryKey: ['service-requests', id, 'invites'],
    queryFn: () => serviceRequestsApi.getInvites(id!).then(r => r.data),
    enabled: !!id && activeTab === 'vendors',
  });

  const { data: quotes } = useQuery({
    queryKey: ['service-requests', id, 'quotes'],
    queryFn: () => serviceRequestsApi.getQuotes(id!).then(r => r.data),
    enabled: !!id && (activeTab === 'quotes' || activeTab === 'proposal'),
  });

  const { data: proposal } = useQuery({
    queryKey: ['service-requests', id, 'proposal'],
    queryFn: () => proposalsApi.getByServiceRequest(id!).then(r => r.status === 204 ? null : r.data),
    enabled: !!id && activeTab === 'proposal',
  });

  const { data: vendors } = useQuery({
    queryKey: ['vendors', { search: vendorSearch }],
    queryFn: () => vendorsApi.list({ search: vendorSearch || undefined, pageSize: 50 }).then(r => r.data),
    enabled: inviteModalOpen,
  });

  const { data: comments } = useQuery({
    queryKey: ['comments', { serviceRequestId: id }],
    queryFn: () => commentsApi.list({ serviceRequestId: id! }).then(r => r.data),
    enabled: !!id && activeTab === 'overview',
  });

  const updateStatus = useMutation({
    mutationFn: (status: ServiceRequestStatus) => serviceRequestsApi.updateStatus(id!, status),
    onSuccess: () => {
      toast.success('Status updated');
      queryClient.invalidateQueries({ queryKey: ['service-requests', id] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Failed to update status'),
  });

  const createInvites = useMutation({
    mutationFn: (vendorIds: string[]) => serviceRequestsApi.createInvites(id!, vendorIds),
    onSuccess: () => {
      toast.success('Vendors invited');
      setInviteModalOpen(false);
      setSelectedVendors([]);
      queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'invites'] });
    },
    onError: () => toast.error('Failed to invite vendors'),
  });

  const selectQuote = useMutation({
    mutationFn: (quoteId: string) => quotesApi.updateStatus(quoteId, 'Selected'),
    onSuccess: () => {
      toast.success('Quote selected');
      queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'quotes'] });
    },
    onError: () => toast.error('Failed to select quote'),
  });

  const { register: registerComment, handleSubmit: handleCommentSubmit, reset: resetComment, formState: { errors: commentErrors, isSubmitting: isSubmittingComment } } = useForm<CommentForm>({
    resolver: zodResolver(commentSchema),
  });

  const createComment = useMutation({
    mutationFn: (data: CommentForm) => commentsApi.create({ text: data.text, serviceRequestId: id! }),
    onSuccess: () => {
      toast.success('Comment added');
      resetComment();
      queryClient.invalidateQueries({ queryKey: ['comments', { serviceRequestId: id }] });
    },
    onError: () => toast.error('Failed to add comment'),
  });

  // Proposal mutations removed — now handled by ProposalBuilder/ProposalDetail components

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <LoadingSpinner />
      </div>
    );
  }

  if (!sr) {
    return (
      <EmptyState title="Request not found" action={<Button onClick={() => navigate('/requests')}>Back to Requests</Button>} />
    );
  }

  return (
    <div>
      {/* Header */}
      <div className="mb-6">
        <button onClick={() => navigate('/requests')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
          ← Back to Requests
        </button>
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{sr.title}</h1>
            <div className="flex items-center gap-3 mt-2">
              <StatusBadge status={sr.status} />
              <PriorityBadge priority={sr.priority} />
              <span className="text-sm text-gray-500">{sr.client?.companyName}</span>
              <span className="text-sm text-gray-400">{formatDate(sr.createdAt)}</span>
            </div>
          </div>
          {isOperator && (
            <select
              value={sr.status}
              onChange={e => updateStatus.mutate(e.target.value as ServiceRequestStatus)}
              className="border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
            >
              {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex gap-6">
          {TABS.map(tab => (
            <button
              key={tab}
              onClick={() => setTab(tab)}
              className={`py-3 text-sm font-medium border-b-2 capitalize transition-colors ${
                activeTab === tab
                  ? 'border-brand-600 text-brand-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {tab === 'workorder' ? 'Work Order' : tab}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && (
        <div className="grid grid-cols-3 gap-6">
          <div className="col-span-2">
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Details</h2>
              <dl className="space-y-4">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Description</dt>
                  <dd className="mt-1 text-sm text-gray-900">{sr.description}</dd>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Location</dt>
                    <dd className="mt-1 text-sm text-gray-900">{sr.location}</dd>
                  </div>
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Category</dt>
                    <dd className="mt-1 text-sm text-gray-900">{sr.category}</dd>
                  </div>
                </div>
              </dl>
            </div>

            {/* Comments */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Comments</h2>
              <div className="space-y-4 mb-4">
                {(comments ?? []).length === 0 ? (
                  <p className="text-sm text-gray-500">No comments yet</p>
                ) : (
                  comments?.map(c => (
                    <div key={c.id} className="flex gap-3">
                      <div className="w-8 h-8 rounded-full bg-brand-100 flex items-center justify-center text-brand-700 text-sm font-medium flex-shrink-0">
                        {c.author?.name?.[0]?.toUpperCase()}
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium text-gray-900">{c.author?.name}</span>
                          <span className="text-xs text-gray-400">{formatRelativeTime(c.createdAt)}</span>
                        </div>
                        <p className="text-sm text-gray-700 mt-0.5">{c.text}</p>
                      </div>
                    </div>
                  ))
                )}
              </div>
              <form onSubmit={handleCommentSubmit(data => createComment.mutate(data))} className="flex gap-2">
                <input
                  type="text"
                  {...registerComment('text')}
                  className="flex-1 border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
                  placeholder="Add a comment..."
                />
                <Button type="submit" size="sm" loading={isSubmittingComment}>
                  Post
                </Button>
              </form>
              {commentErrors.text && <p className="mt-1 text-xs text-red-600">{commentErrors.text.message}</p>}
            </div>
          </div>

          <div>
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Summary</h2>
              <dl className="space-y-3">
                <div>
                  <dt className="text-xs text-gray-500">Client</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.client?.companyName}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Quotes</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.quoteCount}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Proposal</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.hasProposal ? 'Yes' : 'No'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Work Order</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.hasWorkOrder ? 'Yes' : 'No'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Created By</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.createdBy?.name}</dd>
                </div>
              </dl>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'vendors' && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">Vendor Invites</h2>
            {isOperator && (
              <div className="flex gap-2">
                <Button variant="secondary" onClick={() => setFindVendorsOpen(true)}>Find Vendors</Button>
                <Button onClick={() => setInviteModalOpen(true)}>Invite Vendors</Button>
              </div>
            )}
          </div>

          {(invites ?? []).length === 0 ? (
            <EmptyState title="No vendors invited yet" description="Invite vendors to submit quotes for this request" />
          ) : (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <table className="min-w-full divide-y divide-gray-200">
                <thead>
                  <tr className="bg-gray-100 border-b border-gray-300">
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Vendor</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Trades</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Status</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Quote Price</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Sent</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {invites?.map((invite, idx) => (
                    <tr key={invite.id} className={`hover:bg-blue-50/50 transition-colors ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                      <td className="px-4 py-2.5 text-sm font-medium text-gray-900">{invite.vendor?.companyName}</td>
                      <td className="px-4 py-2.5">
                        <div className="flex flex-wrap gap-1">
                          {invite.vendor?.trades.map(t => (
                            <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">{t}</span>
                          ))}
                        </div>
                      </td>
                      <td className="px-4 py-2.5"><StatusBadge status={invite.status} /></td>
                      <td className="px-4 py-2.5 text-sm text-gray-600">
                        {invite.quote?.price != null ? formatCurrency(invite.quote.price) : '—'}
                      </td>
                      <td className="px-4 py-2.5 text-sm text-gray-500">{formatDate(invite.sentAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Find Vendors Modal */}
          <FindVendorsModal
            isOpen={findVendorsOpen}
            onClose={() => setFindVendorsOpen(false)}
            serviceRequestZip={sr.location ?? ''}
            requiredTrade={sr.category ?? ''}
            onSelectVendor={(vendor: VendorSourcingResult) => {
              setFindVendorsOpen(false);
              toast.success(`${vendor.companyName} selected — invite them via the Invites section below`);
            }}
          />

          {/* Invite Modal */}
          <Modal open={inviteModalOpen} onClose={() => setInviteModalOpen(false)} title="Invite Vendors" size="lg">
            <div>
              <input
                type="text"
                placeholder="Search vendors by name or trade..."
                value={vendorSearch}
                onChange={e => setVendorSearch(e.target.value)}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2 mb-4"
              />
              <div className="max-h-64 overflow-y-auto space-y-2 mb-4">
                {(vendors?.items ?? []).map(vendor => (
                  <label key={vendor.id} className="flex items-center gap-3 p-3 rounded-lg border border-gray-200 hover:bg-gray-50 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={selectedVendors.includes(vendor.id)}
                      onChange={e => {
                        if (e.target.checked) setSelectedVendors(prev => [...prev, vendor.id]);
                        else setSelectedVendors(prev => prev.filter(v => v !== vendor.id));
                      }}
                      className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                    />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">{vendor.companyName}</p>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {vendor.trades.map(t => (
                          <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">{t}</span>
                        ))}
                      </div>
                    </div>
                  </label>
                ))}
              </div>
              <div className="flex justify-end gap-3">
                <Button variant="secondary" onClick={() => setInviteModalOpen(false)}>Cancel</Button>
                <Button
                  loading={createInvites.isPending}
                  disabled={selectedVendors.length === 0}
                  onClick={() => createInvites.mutate(selectedVendors)}
                >
                  Invite {selectedVendors.length > 0 ? `(${selectedVendors.length})` : ''} Vendors
                </Button>
              </div>
            </div>
          </Modal>
        </div>
      )}

      {activeTab === 'quotes' && (
        <QuotesTab quotes={quotes ?? []} isOperator={isOperator} selectQuote={selectQuote} />
      )}

      {activeTab === 'proposal' && (
        <div className="max-w-3xl">
          <h2 className="text-base font-semibold text-gray-900 mb-4">Proposal</h2>
          {(() => {
            const selectedQuote = selectedQuoteIdForProposal
              ? quotes?.find(q => q.id === selectedQuoteIdForProposal)
              : proposal?.quoteId
                ? quotes?.find(q => q.id === proposal.quoteId)
                : null;

            // Editing / creating mode
            if (proposalEditMode || (!proposal && selectedQuote)) {
              if (!selectedQuote) {
                return (
                  <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                    <p className="text-sm text-gray-600">No quote selected. Please go back and select a quote first.</p>
                    <Button variant="secondary" className="mt-3" onClick={() => { setProposalEditMode(false); setSelectedQuoteIdForProposal(null); }}>
                      Back
                    </Button>
                  </div>
                );
              }
              return (
                <div>
                  <button
                    onClick={() => { setProposalEditMode(false); setSelectedQuoteIdForProposal(null); }}
                    className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1"
                  >
                    &larr; Back to proposal
                  </button>
                  <ProposalBuilder
                    serviceRequestId={id!}
                    quote={selectedQuote}
                    existingProposal={proposal}
                    allQuotes={quotes ?? []}
                    onSuccess={() => { setProposalEditMode(false); setSelectedQuoteIdForProposal(null); }}
                  />
                </div>
              );
            }

            // Existing proposal — detail view
            if (proposal) {
              return (
                <ProposalDetail
                  proposal={proposal}
                  serviceRequestId={id!}
                  onEdit={() => setProposalEditMode(true)}
                />
              );
            }

            // No proposal yet — show quote selector
            if (isOperator) {
              const eligibleQuotes = quotes?.filter(q => q.status === 'Selected' || q.status === 'Submitted') ?? [];
              if (eligibleQuotes.length === 0) {
                return (
                  <EmptyState
                    title="No quotes available"
                    description="Wait for vendors to submit quotes, then select one to create a proposal."
                  />
                );
              }
              return (
                <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                  <p className="text-sm text-gray-600 mb-4">Select a quote to base your proposal on.</p>
                  <div className="space-y-3">
                    {eligibleQuotes.map(q => (
                      <button
                        key={q.id}
                        onClick={() => setSelectedQuoteIdForProposal(q.id)}
                        className="w-full flex items-center justify-between p-4 rounded-lg border border-gray-200 hover:bg-gray-50 hover:border-brand-300 transition-colors text-left"
                      >
                        <div>
                          <p className="text-sm font-medium text-gray-900">{q.vendor?.companyName}</p>
                          <p className="text-xs text-gray-500">{q.scopeOfWork?.substring(0, 80)}...</p>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-bold text-gray-900">{formatCurrency(q.price)}</p>
                          <StatusBadge status={q.status} />
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              );
            }

            return <EmptyState title="No proposal created yet" description="The operator will create a proposal for this request" />;
          })()}
        </div>
      )}

      {activeTab === 'workorder' && (
        <div>
          <h2 className="text-base font-semibold text-gray-900 mb-4">Work Order</h2>
          {!sr.hasWorkOrder ? (
            <EmptyState
              title="No work order yet"
              description="A work order is created automatically when a proposal is approved by the client"
            />
          ) : (
            <p className="text-sm text-gray-600">Work order created. View in the Work Orders section for full details.</p>
          )}
        </div>
      )}
    </div>
  );
}
