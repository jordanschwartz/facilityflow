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
import { proposalsApi } from '../../api/proposals';
import { commentsApi } from '../../api/comments';
import type { ServiceRequestStatus } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import Button from '../../components/ui/Button';
import Modal from '../../components/ui/Modal';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatCurrency, formatRelativeTime } from '../../utils/formatters';
import { useAuthStore } from '../../stores/authStore';

const STATUSES: ServiceRequestStatus[] = ['New', 'Sourcing', 'Quoting', 'PendingApproval', 'Approved', 'Rejected', 'Completed'];
const TABS = ['overview', 'vendors', 'quotes', 'proposal', 'workorder'] as const;
type Tab = typeof TABS[number];

const commentSchema = z.object({ text: z.string().min(1, 'Comment cannot be empty') });
type CommentForm = z.infer<typeof commentSchema>;

const proposalSchema = z.object({
  quoteId: z.string().min(1, 'Select a quote'),
  price: z.string().min(1, 'Price required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) >= 0, 'Price must be positive'),
  scopeOfWork: z.string().min(10, 'Scope of work required'),
});
type ProposalForm = z.infer<typeof proposalSchema>;

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
  const [vendorSearch, setVendorSearch] = useState('');
  const [selectedVendors, setSelectedVendors] = useState<string[]>([]);

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

  const { register: registerProposal, handleSubmit: handleProposalSubmit, formState: { errors: proposalErrors, isSubmitting: isSubmittingProposal } } = useForm<ProposalForm>({
    resolver: zodResolver(proposalSchema),
  });

  const createProposal = useMutation({
    mutationFn: (data: ProposalForm) => proposalsApi.create(id!, { quoteId: data.quoteId, price: parseFloat(data.price), scopeOfWork: data.scopeOfWork }),
    onSuccess: () => {
      toast.success('Proposal created');
      queryClient.invalidateQueries({ queryKey: ['service-requests', id] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'proposal'] });
    },
    onError: () => toast.error('Failed to create proposal'),
  });

  const sendProposal = useMutation({
    mutationFn: (proposalId: string) => proposalsApi.send(proposalId),
    onSuccess: () => {
      toast.success('Proposal sent to client');
      queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'proposal'] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', id] });
    },
    onError: () => toast.error('Failed to send proposal'),
  });

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
              <Button onClick={() => setInviteModalOpen(true)}>Invite Vendors</Button>
            )}
          </div>

          {(invites ?? []).length === 0 ? (
            <EmptyState title="No vendors invited yet" description="Invite vendors to submit quotes for this request" />
          ) : (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <table className="min-w-full divide-y divide-gray-200">
                <thead>
                  <tr className="bg-gray-50">
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Vendor</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Trades</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Quote Price</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Sent</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {invites?.map(invite => (
                    <tr key={invite.id}>
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">{invite.vendor?.companyName}</td>
                      <td className="px-6 py-4">
                        <div className="flex flex-wrap gap-1">
                          {invite.vendor?.trades.map(t => (
                            <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">{t}</span>
                          ))}
                        </div>
                      </td>
                      <td className="px-6 py-4"><StatusBadge status={invite.status} /></td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {invite.quote?.price != null ? formatCurrency(invite.quote.price) : '—'}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">{formatDate(invite.sentAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

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
        <div>
          <h2 className="text-base font-semibold text-gray-900 mb-4">Quotes Received</h2>
          {(quotes ?? []).length === 0 ? (
            <EmptyState title="No quotes yet" description="Quotes will appear here as vendors submit them" />
          ) : (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <table className="min-w-full divide-y divide-gray-200">
                <thead>
                  <tr className="bg-gray-50">
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Vendor</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Price</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Scope (Preview)</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Submitted</th>
                    {isOperator && <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {quotes?.map(quote => (
                    <tr key={quote.id}>
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">{quote.vendor?.companyName}</td>
                      <td className="px-6 py-4 text-sm text-gray-900 font-medium">{formatCurrency(quote.price)}</td>
                      <td className="px-6 py-4 text-sm text-gray-500 max-w-xs truncate">{quote.scopeOfWork}</td>
                      <td className="px-6 py-4"><StatusBadge status={quote.status} /></td>
                      <td className="px-6 py-4 text-sm text-gray-500">{quote.submittedAt ? formatDate(quote.submittedAt) : '—'}</td>
                      {isOperator && (
                        <td className="px-6 py-4 text-right">
                          <div className="flex items-center justify-end gap-2">
                            {quote.publicToken && quote.status === 'Requested' && (
                              <Button
                                size="sm"
                                variant="secondary"
                                onClick={() => {
                                  const link = `${window.location.origin}/quotes/submit/${quote.publicToken}`;
                                  navigator.clipboard.writeText(link);
                                  toast.success('Link copied to clipboard');
                                }}
                              >
                                Copy Link
                              </Button>
                            )}
                            {quote.status === 'Submitted' && (
                              <Button
                                size="sm"
                                onClick={() => selectQuote.mutate(quote.id)}
                                loading={selectQuote.isPending}
                              >
                                Select
                              </Button>
                            )}
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'proposal' && (
        <div className="max-w-2xl">
          <h2 className="text-base font-semibold text-gray-900 mb-4">Proposal</h2>
          {proposal ? (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 space-y-4">
              <div className="flex items-center justify-between">
                <StatusBadge status={proposal.status} />
                {proposal.status === 'Draft' && isOperator && (
                  <Button size="sm" loading={sendProposal.isPending} onClick={() => sendProposal.mutate(proposal.id)}>
                    Send to Client
                  </Button>
                )}
              </div>
              <dl className="space-y-3">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Price to Client</dt>
                  <dd className="mt-1 text-2xl font-bold text-gray-900">{formatCurrency(proposal.price)}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Scope of Work</dt>
                  <dd className="mt-1 text-sm text-gray-700 whitespace-pre-wrap">{proposal.scopeOfWork}</dd>
                </div>
                {proposal.sentAt && (
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Sent</dt>
                    <dd className="mt-1 text-sm text-gray-700">{formatDate(proposal.sentAt)}</dd>
                  </div>
                )}
                {proposal.status === 'Sent' && proposal.publicToken && (
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Client Link</dt>
                    <dd className="mt-1 flex items-center gap-2">
                      <code className="text-xs bg-gray-100 rounded px-2 py-1 flex-1 truncate">
                        {`${window.location.origin}/proposals/view/${proposal.publicToken}`}
                      </code>
                      <Button size="sm" variant="secondary" onClick={() => {
                        navigator.clipboard.writeText(`${window.location.origin}/proposals/view/${proposal.publicToken}`);
                        toast.success('Link copied');
                      }}>Copy</Button>
                    </dd>
                  </div>
                )}
                {proposal.clientResponse && (
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Client Response</dt>
                    <dd className="mt-1 text-sm text-gray-700">{proposal.clientResponse}</dd>
                  </div>
                )}
              </dl>
            </div>
          ) : isOperator ? (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
              <p className="text-sm text-gray-600 mb-4">Create a proposal based on the selected quote to send to the client.</p>
              <form onSubmit={handleProposalSubmit(data => createProposal.mutate(data))} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Base Quote</label>
                  <select
                    {...registerProposal('quoteId')}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                  >
                    <option value="">Select a quote...</option>
                    {quotes?.filter(q => q.status === 'Selected' || q.status === 'Submitted').map(q => (
                      <option key={q.id} value={q.id}>
                        {q.vendor?.companyName} — {formatCurrency(q.price)}
                      </option>
                    ))}
                  </select>
                  {proposalErrors.quoteId && <p className="mt-1 text-xs text-red-600">{proposalErrors.quoteId.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Price to Client</label>
                  <input
                    type="number"
                    step="0.01"
                    {...registerProposal('price')}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="0.00"
                  />
                  {proposalErrors.price && <p className="mt-1 text-xs text-red-600">{proposalErrors.price.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Scope of Work</label>
                  <textarea
                    {...registerProposal('scopeOfWork')}
                    rows={5}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Describe the work to be performed..."
                  />
                  {proposalErrors.scopeOfWork && <p className="mt-1 text-xs text-red-600">{proposalErrors.scopeOfWork.message}</p>}
                </div>
                <Button type="submit" loading={isSubmittingProposal}>
                  Create Proposal
                </Button>
              </form>
            </div>
          ) : (
            <EmptyState title="No proposal created yet" description="The operator will create a proposal for this request" />
          )}
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
