import { useState, useRef, useEffect } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { serviceRequestsApi } from '../../api/serviceRequests';
import { quotesApi } from '../../api/quotes';
import { proposalsApi } from '../../api/proposals';
import { invoicesApi } from '../../api/invoices';
import { workOrdersApi } from '../../api/workOrders';
import FindVendorsModal from '../../components/vendors/FindVendorsModal';
import ManualQuoteModal from '../../components/quotes/ManualQuoteModal';
import ProposalBuilder from '../../components/proposals/ProposalBuilder';
import ProposalDetail from '../../components/proposals/ProposalDetail';
import type { ServiceRequestStatus, Quote } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import Button from '../../components/ui/Button';
import Modal from '../../components/ui/Modal';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatCurrency, formatRelativeTime } from '../../utils/formatters';
import ActivityTimeline from '../../components/ActivityTimeline';
import EmailList from '../../components/EmailList';
import { useAuthStore } from '../../stores/authStore';
import {
  PaperClipIcon,
  XMarkIcon,
  ExclamationTriangleIcon,
  CalendarDaysIcon,
  DocumentTextIcon,
  ArrowUpTrayIcon,
  CheckCircleIcon,
} from '@heroicons/react/24/solid';
import {
  ClockIcon,
  MapPinIcon,
  TagIcon,
  UserGroupIcon,
  CurrencyDollarIcon,
  InformationCircleIcon,
  ClipboardDocumentIcon,
  PaperAirplaneIcon,
  EyeIcon,
  EnvelopeIcon,
  LinkIcon,
} from '@heroicons/react/24/outline';

const API_BASE = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

const STATUS_LABELS: Record<string, string> = {
  New: 'New',
  Qualifying: 'Qualifying',
  Sourcing: 'Sourcing',
  SchedulingSiteVisit: 'Scheduling Site Visit',
  ScheduleConfirmed: 'Schedule Confirmed',
  PendingQuotes: 'Pending Quotes',
  ProposalReady: 'Proposal Ready',
  PendingApproval: 'Pending Approval',
  AwaitingPO: 'Awaiting PO',
  POReceived: 'PO Received',
  JobInProgress: 'Job In Progress',
  JobCompleted: 'Job Completed',
  Verification: 'Verification',
  InvoiceSent: 'Invoice Sent',
  InvoicePaid: 'Invoice Paid',
  Closed: 'Closed',
  Cancelled: 'Cancelled',
};

const TABS = ['timeline', 'details', 'emails', 'vendors', 'proposal', 'po-scheduling', 'invoice'] as const;
type Tab = (typeof TABS)[number];
const TAB_LABELS: Record<Tab, string> = {
  timeline: 'Timeline',
  details: 'Details',
  emails: 'Emails',
  vendors: 'Vendors & Quotes',
  proposal: 'Proposal',
  'po-scheduling': 'PO & Scheduling',
  invoice: 'Invoice',
};

const detailsSchema = z.object({
  title: z.string().min(1),
  description: z.string().min(1),
  location: z.string().min(1),
  category: z.string().min(1),
  priority: z.string().min(1),
});
type DetailsForm = z.infer<typeof detailsSchema>;

const poSchema = z.object({
  poNumber: z.string().min(1, 'PO number is required'),
  poAmount: z.string().optional(),
});
type PoForm = z.infer<typeof poSchema>;

// ─── QuoteDetailModal (reused from previous) ─────────────────────────────────
function QuoteDetailModal({ quote, open, onClose }: { quote: Quote; open: boolean; onClose: () => void }) {
  const hasLineItems = quote.lineItems && quote.lineItems.length > 0;
  const hasAttachments = quote.attachments && quote.attachments.length > 0;
  return (
    <Modal open={open} onClose={onClose} title={`Quote — ${quote.vendor?.companyName}`} size="lg">
      <div className="space-y-5 max-h-[70vh] overflow-y-auto pr-1">
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
        {quote.scopeOfWork && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Scope of Work</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{quote.scopeOfWork}</p>
          </div>
        )}
        {(quote.proposedStartDate || quote.estimatedDurationValue != null || quote.vendorAvailability || quote.validUntil || quote.submittedAt) && (
          <div className="grid grid-cols-2 gap-3">
            {quote.submittedAt && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Submitted</p><p className="text-sm text-gray-700">{formatDate(quote.submittedAt)}</p></div>}
            {quote.proposedStartDate && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Proposed Start</p><p className="text-sm text-gray-700">{formatDate(quote.proposedStartDate)}</p></div>}
            {quote.estimatedDurationValue != null && quote.estimatedDurationUnit && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Duration</p><p className="text-sm text-gray-700">{quote.estimatedDurationValue} {quote.estimatedDurationUnit}</p></div>}
            {quote.vendorAvailability && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Availability</p><p className="text-sm text-gray-700">{quote.vendorAvailability}</p></div>}
            {quote.validUntil && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Valid Until</p><p className="text-sm text-gray-700">{formatDate(quote.validUntil)}</p></div>}
          </div>
        )}
        {hasLineItems && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Line Items</p>
            <table className="min-w-full text-sm">
              <thead><tr className="text-xs text-gray-500 border-b border-gray-200"><th className="text-left font-medium pb-1 pr-4">Description</th><th className="text-left font-medium pb-1 pr-4">Qty</th><th className="text-left font-medium pb-1 pr-4">Unit Price</th><th className="text-right font-medium pb-1">Total</th></tr></thead>
              <tbody>{quote.lineItems.map(li => (<tr key={li.id} className="border-t border-gray-100"><td className="py-1.5 pr-4 text-gray-700">{li.description}</td><td className="py-1.5 pr-4 text-gray-700">{li.quantity}</td><td className="py-1.5 pr-4 text-gray-700">{formatCurrency(li.unitPrice)}</td><td className="py-1.5 text-right text-gray-900 font-medium">{formatCurrency(li.total)}</td></tr>))}</tbody>
            </table>
          </div>
        )}
        {(quote.assumptions || quote.exclusions) && (
          <div className="grid grid-cols-2 gap-4">
            {quote.assumptions && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Assumptions</p><p className="text-sm text-gray-700 whitespace-pre-wrap">{quote.assumptions}</p></div>}
            {quote.exclusions && <div><p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Exclusions</p><p className="text-sm text-gray-700 whitespace-pre-wrap">{quote.exclusions}</p></div>}
          </div>
        )}
        {hasAttachments && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Attachments</p>
            <div className="flex flex-wrap gap-2">
              {quote.attachments.map(a => {
                const url = `${API_BASE}${a.url}`;
                if (a.mimeType.startsWith('image/')) return <a key={a.id} href={url} target="_blank" rel="noopener noreferrer"><img src={url} alt={a.filename} className="w-24 h-24 object-cover rounded-lg border border-gray-200 hover:opacity-80" /></a>;
                return <a key={a.id} href={url} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 bg-gray-50 hover:bg-gray-100 text-xs text-gray-700"><PaperClipIcon className="w-3.5 h-3.5 text-gray-400" /><span className="truncate max-w-[120px]">{a.filename}</span></a>;
              })}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
}

// ─── QuoteCard ────────────────────────────────────────────────────────────────
function QuoteCard({ quote, isOperator, selectQuote, isSelected }: { quote: Quote; isOperator: boolean; selectQuote: { mutate: (id: string) => void; isPending: boolean }; isSelected?: boolean }) {
  const [detailOpen, setDetailOpen] = useState(false);
  return (
    <div className="border border-gray-200 rounded-xl bg-white shadow-sm p-4">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3 min-w-0">
          <span className="text-sm font-medium text-gray-900">{quote.vendor?.companyName}</span>
          <StatusBadge status={quote.status} />
          {quote.attachments?.length > 0 && <span className="text-xs text-gray-400">{quote.attachments.length} attachment{quote.attachments.length !== 1 ? 's' : ''}</span>}
        </div>
        <div className="flex items-center gap-3 flex-shrink-0">
          <div className="text-right">
            <p className="text-sm font-bold text-gray-900">{formatCurrency(quote.price)}</p>
            {quote.notToExceedPrice != null && <p className="text-xs text-gray-400">NTE {formatCurrency(quote.notToExceedPrice)}</p>}
          </div>
          {quote.submittedAt && <p className="text-xs text-gray-400 hidden sm:block">{formatDate(quote.submittedAt)}</p>}
        </div>
      </div>
      {isOperator && (
        <div className="mt-3 flex items-center gap-2 justify-end">
          <Button size="sm" variant="secondary" onClick={() => setDetailOpen(true)}>View</Button>
          {quote.publicToken && quote.status === 'Requested' && (
            <Button size="sm" variant="secondary" onClick={() => { navigator.clipboard.writeText(`${window.location.origin}/quotes/submit/${quote.publicToken}`); toast.success('Link copied'); }}>Copy Link</Button>
          )}
          {quote.status === 'Submitted' && !isSelected && <Button size="sm" onClick={() => selectQuote.mutate(quote.id)} loading={selectQuote.isPending}>Select</Button>}
          {isSelected && <span className="inline-flex items-center gap-1 text-xs font-medium text-green-700 bg-green-50 border border-green-200 rounded-full px-2.5 py-1"><CheckCircleIcon className="w-3.5 h-3.5" />Selected</span>}
        </div>
      )}
      <QuoteDetailModal quote={quote} open={detailOpen} onClose={() => setDetailOpen(false)} />
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────
export default function RequestDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const isOperator = user?.role === 'Operator' || user?.role === 'Admin';

  const tabParam = searchParams.get('tab') as Tab | null;
  const [activeTab, setActiveTab] = useState<Tab>(tabParam && TABS.includes(tabParam) ? tabParam : 'timeline');
  useEffect(() => {
    if (tabParam && TABS.includes(tabParam) && tabParam !== activeTab) setActiveTab(tabParam);
  }, [tabParam]); // eslint-disable-line react-hooks/exhaustive-deps
  const switchTab = (tab: Tab) => { setActiveTab(tab); setSearchParams({ tab }); };

  // ── PO file state ──
  const [poFile, setPoFile] = useState<File | null>(null);
  const poFileRef = useRef<HTMLInputElement>(null);

  // ── Find vendors modal state ──
  const [findVendorsOpen, setFindVendorsOpen] = useState(false);

  // ── Proposal state ──
  const [proposalEditMode, setProposalEditMode] = useState(false);
  const [selectedQuoteIdForProposal, setSelectedQuoteIdForProposal] = useState<string | null>(null);

  // ── Schedule state ──
  const [scheduleDate, setScheduleDate] = useState('');
  const [confirmSendWoVendorId, setConfirmSendWoVendorId] = useState<string | null>(null);

  // ── Manual quote entry state ──
  const [manualQuoteVendor, setManualQuoteVendor] = useState<{ id: string; name: string } | null>(null);
  const [confirmAssignVendor, setConfirmAssignVendor] = useState<{ id: string; name: string; hasQuote?: boolean; quoteId?: string } | null>(null);
  const [confirmUnassignVendor, setConfirmUnassignVendor] = useState<string | null>(null);

  // ══════════════════════════ QUERIES ══════════════════════════
  const { data: sr, isLoading } = useQuery({
    queryKey: ['service-requests', id],
    queryFn: () => serviceRequestsApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: allowedTransitions } = useQuery({
    queryKey: ['service-requests', id, 'transitions'],
    queryFn: () => serviceRequestsApi.getAllowedTransitions(id!).then(r => r.data),
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
    enabled: !!id && (activeTab === 'vendors' || activeTab === 'proposal'),
  });

  const { data: proposal } = useQuery({
    queryKey: ['service-requests', id, 'proposal'],
    queryFn: () => proposalsApi.getByServiceRequest(id!).then(r => r.status === 204 ? null : r.data),
    enabled: !!id && activeTab === 'proposal',
  });


  const invoiceStatuses = ['JobCompleted', 'Verification', 'InvoiceSent', 'InvoicePaid', 'Closed'];
  const { data: invoiceList } = useQuery({
    queryKey: ['invoices', { clientId: sr?.clientId }],
    queryFn: () => invoicesApi.list({ clientId: sr?.clientId, page: 1, pageSize: 50 }).then(r => r.data),
    enabled: !!sr && invoiceStatuses.includes(sr.status) && activeTab === 'invoice',
  });

  // ══════════════════════════ MUTATIONS ══════════════════════════
  const updateStatus = useMutation({
    mutationFn: (status: ServiceRequestStatus) => serviceRequestsApi.updateStatus(id!, status),
    onSuccess: () => {
      toast.success('Status updated');
      queryClient.invalidateQueries({ queryKey: ['service-requests', id] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'transitions'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
    },
    onError: () => toast.error('Failed to update status'),
  });

  const selectQuote = useMutation({
    mutationFn: (quoteId: string) => quotesApi.updateStatus(quoteId, 'Selected'),
    onSuccess: () => { toast.success('Vendor assigned'); setConfirmAssignVendor(null); queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'quotes'] }); queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'invites'] }); queryClient.invalidateQueries({ queryKey: ['activity-logs'] }); },
    onError: () => toast.error('Failed to assign vendor'),
  });

  const unselectQuote = useMutation({
    mutationFn: () => quotesApi.unselect(id!),
    onSuccess: () => { toast.success('Vendor unassigned'); queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'quotes'] }); queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'invites'] }); queryClient.invalidateQueries({ queryKey: ['activity-logs'] }); },
    onError: () => toast.error('Failed to unassign vendor'),
  });

  const updateDetails = useMutation({
    mutationFn: (data: DetailsForm) => serviceRequestsApi.update(id!, data),
    onSuccess: () => { toast.success('Details saved'); queryClient.invalidateQueries({ queryKey: ['service-requests', id] }); queryClient.invalidateQueries({ queryKey: ['activity-logs'] }); },
    onError: () => toast.error('Failed to save details'),
  });

  const uploadPo = useMutation({
    mutationFn: (data: { poNumber: string; poAmount?: number; file: File }) => serviceRequestsApi.uploadPo(id!, data),
    onSuccess: () => { toast.success('PO uploaded'); setPoFile(null); resetPo(); queryClient.invalidateQueries({ queryKey: ['service-requests', id] }); queryClient.invalidateQueries({ queryKey: ['activity-logs'] }); },
    onError: () => toast.error('Failed to upload PO'),
  });

  const saveSchedule = useMutation({
    mutationFn: (date: string) => serviceRequestsApi.updateSchedule(id!, date),
    onSuccess: () => { toast.success('Schedule updated'); queryClient.invalidateQueries({ queryKey: ['service-requests', id] }); queryClient.invalidateQueries({ queryKey: ['activity-logs'] }); },
    onError: () => toast.error('Failed to update schedule'),
  });

  const sendWorkOrder = useMutation({
    mutationFn: (vendorInviteId: string) => workOrdersApi.sendWorkOrder(id!, vendorInviteId),
    onSuccess: (_data, vendorInviteId) => {
      const invite = invites?.find(i => i.id === vendorInviteId);
      toast.success(`Work order sent to ${invite?.vendor?.companyName ?? 'vendor'}`);
      queryClient.invalidateQueries({ queryKey: ['service-requests', id, 'invites'] });
      queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
    },
    onError: () => toast.error('Failed to send work order'),
  });

  // ── Forms ──
  const { register: registerDetails, handleSubmit: handleDetailsSubmit, formState: { isDirty: detailsDirty } } = useForm<DetailsForm>({
    resolver: zodResolver(detailsSchema),
    values: sr ? { title: sr.title, description: sr.description, location: sr.location, category: sr.category, priority: sr.priority } : undefined,
  });
  const { register: registerPo, handleSubmit: handlePoSubmit, reset: resetPo, formState: { errors: poErrors } } = useForm<PoForm>({ resolver: zodResolver(poSchema) });

  // ══════════════════════════ LOADING / NOT FOUND ══════════════════════════
  if (isLoading) return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  if (!sr) return <EmptyState title="Work order not found" action={<Button onClick={() => navigate('/work-orders')}>Back to Work Orders</Button>} />;

  // ══════════════════════════ DERIVED STATE ══════════════════════════
  const alerts: { text: string; color: string }[] = [];
  if (sr.status === 'AwaitingPO') alerts.push({ text: 'Awaiting PO', color: 'bg-red-100 text-red-700' });
  if (sr.quoteCount === 0 && ['Sourcing', 'PendingQuotes'].includes(sr.status)) alerts.push({ text: 'No Quotes', color: 'bg-yellow-100 text-yellow-700' });
  if (!sr.hasProposal && ['PendingApproval', 'AwaitingPO'].includes(sr.status)) alerts.push({ text: 'No Proposal', color: 'bg-orange-100 text-orange-700' });

  return (
    <div>
      {/* ════════════════════ HEADER ════════════════════ */}
      <button onClick={() => navigate('/work-orders')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">&larr; Back to Work Orders</button>

      <div className={`flex items-start justify-between mb-4 ${sr.priority === 'Urgent' ? 'border-l-4 border-red-500 pl-4' : ''}`}>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900 truncate">
              {sr.title}
            </h1>
            {sr.workOrderNumber && (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 border border-gray-300 rounded-md text-sm font-mono text-gray-600 whitespace-nowrap flex-shrink-0">
                <span className="text-gray-400">#</span>
                {sr.workOrderNumber}
                <button
                  type="button"
                  onClick={() => {
                    navigator.clipboard.writeText(sr.workOrderNumber!);
                    toast.success('Copied to clipboard');
                  }}
                  className="text-gray-400 hover:text-gray-600 transition-colors"
                  title="Copy work order number"
                >
                  <ClipboardDocumentIcon className="w-3.5 h-3.5" />
                </button>
              </span>
            )}
          </div>
          {alerts.length > 0 && (
            <div className="flex flex-wrap items-center gap-2 mt-2">
              {alerts.map(a => <span key={a.text} className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${a.color}`}><ExclamationTriangleIcon className="w-3 h-3" />{a.text}</span>)}
            </div>
          )}
        </div>
      </div>

      {/* ════════════════════ TABS ════════════════════ */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex gap-6 overflow-x-auto">
          {TABS.map(tab => {
            const poSchedulingStatuses = ['AwaitingPO', 'POReceived', 'JobInProgress', 'JobCompleted', 'Verification', 'InvoiceSent', 'InvoicePaid', 'Closed'];
            const invoiceStatuses = ['JobCompleted', 'Verification', 'InvoiceSent', 'InvoicePaid', 'Closed'];
            const disabled =
              (tab === 'po-scheduling' && !poSchedulingStatuses.includes(sr.status)) ||
              (tab === 'invoice' && !invoiceStatuses.includes(sr.status));
            return (
              <button
                key={tab}
                onClick={() => !disabled && switchTab(tab)}
                disabled={disabled}
                className={`whitespace-nowrap py-3 text-sm font-medium border-b-2 transition-colors ${
                  disabled
                    ? 'border-transparent text-gray-300 cursor-not-allowed'
                    : activeTab === tab
                      ? 'border-brand-600 text-brand-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {TAB_LABELS[tab]}
              </button>
            );
          })}
        </nav>
      </div>

      {/* ════════════════════ 3-COLUMN LAYOUT ════════════════════ */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* ──────── MAIN PANEL (col-span-2) ──────── */}
        <div className="lg:col-span-2 space-y-6">

          {/* ─── TIMELINE TAB ─── */}
          {activeTab === 'timeline' && (
            <ActivityTimeline serviceRequestId={id!} />
          )}

          {/* ─── EMAILS TAB ─── */}
          {activeTab === 'emails' && (
            <EmailList serviceRequestId={id!} />
          )}

          {/* ─── DETAILS TAB ─── */}
          {activeTab === 'details' && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Job Details</h2>
              {isOperator ? (
                <form onSubmit={handleDetailsSubmit(data => updateDetails.mutate(data))} className="space-y-4">
                  <div>
                    <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Title</label>
                    <input {...registerDetails('title')} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Description</label>
                    <textarea {...registerDetails('description')} rows={4} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Location</label>
                      <input {...registerDetails('location')} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Service</label>
                      <input {...registerDetails('category')} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" />
                    </div>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Priority</label>
                    <select {...registerDetails('priority')} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2">
                      {['Low', 'Medium', 'High', 'Urgent'].map(p => <option key={p} value={p}>{p}</option>)}
                    </select>
                  </div>
                  <div className="flex justify-end">
                    <Button type="submit" loading={updateDetails.isPending} disabled={!detailsDirty}>Save Changes</Button>
                  </div>
                </form>
              ) : (
                <dl className="space-y-4">
                  <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Description</dt><dd className="mt-1 text-sm text-gray-900">{sr.description}</dd></div>
                  <div className="grid grid-cols-2 gap-4">
                    <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Location</dt><dd className="mt-1 text-sm text-gray-900">{sr.location}</dd></div>
                    <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Service</dt><dd className="mt-1 text-sm text-gray-900">{sr.category}</dd></div>
                  </div>
                  <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Priority</dt><dd className="mt-1"><PriorityBadge priority={sr.priority} /></dd></div>
                </dl>
              )}
            </div>
          )}

          {/* ─── VENDORS & QUOTES TAB ─── */}
          {activeTab === 'vendors' && (
            <div className="space-y-6">
              {/* Vendor Invites */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-base font-semibold text-gray-900">Vendors</h2>
                  {isOperator && (
                    <Button size="sm" onClick={() => setFindVendorsOpen(true)}>Add Vendors</Button>
                  )}
                </div>
                {(invites ?? []).length === 0 ? (
                  <p className="text-sm text-gray-500">No vendors added yet.</p>
                ) : (
                  <div className="overflow-hidden rounded-lg border border-gray-200">
                    <table className="min-w-full divide-y divide-gray-200">
                      <thead><tr className="bg-gray-100 border-b border-gray-300">
                        <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Vendor</th>
                        <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Status</th>
                        <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Quote</th>
                        {isOperator && <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Work Order Actions</th>}
                      </tr></thead>
                      <tbody className="divide-y divide-gray-100 bg-white">
                        {invites?.map((inv, idx) => (
                          <tr key={inv.id} className={`hover:bg-blue-50/50 transition-colors ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                            <td className="px-4 py-2.5 text-sm font-medium text-gray-900">{inv.vendor?.companyName}</td>
                            <td className="px-4 py-2.5"><StatusBadge status={inv.status} /></td>
                            <td className="px-4 py-2.5 text-sm text-gray-600">{inv.quote?.price != null ? formatCurrency(inv.quote.price) : '---'}</td>
                            {isOperator && (
                              <td className="px-4 py-2.5">
                                {(() => {
                                  const canEmail = inv.status !== 'Rejected';
                                  const canQuoteLink = !!inv.quote?.id;
                                  const canAssign = inv.status !== 'Selected' && inv.status !== 'Rejected';
                                  return (
                                    <div className="flex items-center gap-1 flex-nowrap">
                                      {/* Email */}
                                      <button
                                        className={`p-1.5 rounded-md transition-colors ${canEmail ? 'text-blue-500 hover:bg-blue-50 hover:text-blue-700 cursor-pointer' : 'text-gray-200 cursor-not-allowed'}`}
                                        title={!canEmail ? 'Not available' : inv.status === 'Candidate' ? 'Send work order' : 'Resend work order'}
                                        disabled={!canEmail}
                                        onClick={() => canEmail && setConfirmSendWoVendorId(inv.id)}
                                      >
                                        <EnvelopeIcon className="w-5 h-5" />
                                      </button>
                                      {/* Preview PDF */}
                                      <button
                                        className="p-1.5 rounded-md text-gray-400 hover:bg-gray-100 hover:text-gray-700 transition-colors cursor-pointer"
                                        title="Preview work order PDF"
                                        onClick={async () => {
                                          try {
                                            const res = await workOrdersApi.previewWorkOrderPdf(id!, inv.id);
                                            const blob = new Blob([res.data], { type: 'application/pdf' });
                                            const url = URL.createObjectURL(blob);
                                            window.open(url, '_blank');
                                          } catch {
                                            toast.error('Failed to load PDF preview');
                                          }
                                        }}
                                      >
                                        <EyeIcon className="w-5 h-5" />
                                      </button>
                                      {/* Quote link */}
                                      <button
                                        className={`p-1.5 rounded-md transition-colors ${canQuoteLink ? 'text-amber-500 hover:bg-amber-50 hover:text-amber-700 cursor-pointer' : 'text-gray-200 cursor-not-allowed'}`}
                                        title={canQuoteLink ? 'Copy quote submission link' : 'Quote link not available yet'}
                                        disabled={!canQuoteLink}
                                        onClick={() => {
                                          if (!canQuoteLink) return;
                                          const quoteLink = `${window.location.origin}/quotes/submit/${inv.publicToken}`;
                                          navigator.clipboard.writeText(quoteLink);
                                          toast.success('Quote link copied');
                                        }}
                                      >
                                        <LinkIcon className="w-5 h-5" />
                                      </button>
                                      {/* Assign / Unassign */}
                                      {inv.status === 'Selected' ? (
                                        <button
                                          className="p-1.5 rounded-md transition-colors text-red-400 hover:bg-red-50 hover:text-red-600 cursor-pointer"
                                          title="Unassign vendor"
                                          onClick={() => setConfirmUnassignVendor(inv.vendor?.companyName ?? 'this vendor')}
                                        >
                                          <XMarkIcon className="w-5 h-5" />
                                        </button>
                                      ) : (
                                        <button
                                          className={`p-1.5 rounded-md transition-colors ${canAssign ? 'text-orange-500 hover:bg-orange-50 hover:text-orange-600 cursor-pointer' : 'text-gray-200 cursor-not-allowed'}`}
                                          title={!canAssign ? 'Not available' : inv.quote?.status === 'Submitted' ? 'Assign vendor' : 'Enter quote & assign vendor'}
                                          disabled={!canAssign}
                                          onClick={() => canAssign && setConfirmAssignVendor({
                                            id: inv.id,
                                            name: inv.vendor?.companyName ?? 'Vendor',
                                            hasQuote: inv.quote?.status === 'Submitted',
                                            quoteId: inv.quote?.id,
                                          })}
                                        >
                                          <CheckCircleIcon className="w-5 h-5" />
                                        </button>
                                      )}
                                    </div>
                                  );
                                })()}
                              </td>
                            )}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Quotes */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4">Quotes Received</h2>
                {(() => {
                  const receivedQuotes = (quotes ?? []).filter(q => q.status !== 'Requested');
                  return receivedQuotes.length === 0 ? (
                    <p className="text-sm text-gray-500">No quotes yet. Quotes will appear here as vendors submit them.</p>
                  ) : (
                    <div className="space-y-4">
                      {receivedQuotes.map(q => {
                        const vendorInvite = invites?.find(i => i.vendorId === q.vendorId);
                        const isSelected = q.status === 'Selected' || vendorInvite?.status === 'Selected';
                        return <QuoteCard key={q.id} quote={q} isOperator={isOperator} selectQuote={selectQuote} isSelected={isSelected} />;
                      })}
                    </div>
                  );
                })()}
              </div>

              {/* Find Vendors Modal */}
              <FindVendorsModal
                isOpen={findVendorsOpen}
                onClose={() => setFindVendorsOpen(false)}
                serviceRequestZip={sr.location ?? ''}
                requiredTrade={sr.category ?? ''}
                serviceRequestId={id}
              />

              {/* Manual Quote Entry Modal */}
              <ManualQuoteModal
                isOpen={!!manualQuoteVendor}
                onClose={() => setManualQuoteVendor(null)}
                serviceRequestId={id!}
                vendorInviteId={manualQuoteVendor?.id ?? ''}
                vendorName={manualQuoteVendor?.name ?? ''}
                onQuoteCreated={(quoteId) => selectQuote.mutate(quoteId)}
              />

              {/* Confirm Send Work Order Dialog */}
              {confirmSendWoVendorId && (() => {
                const inv = invites?.find(i => i.id === confirmSendWoVendorId);
                const isResend = inv?.status !== 'Candidate';
                return (
                  <div className="fixed inset-0 z-50 flex items-center justify-center">
                    <div className="fixed inset-0 bg-black/30" onClick={() => setConfirmSendWoVendorId(null)} />
                    <div className="relative bg-white rounded-xl shadow-xl p-6 max-w-md w-full mx-4">
                      <h3 className="text-lg font-semibold text-gray-900 mb-2">
                        {isResend ? 'Resend Work Order?' : 'Send Work Order?'}
                      </h3>
                      <p className="text-sm text-gray-600 mb-1">
                        {isResend
                          ? `This will resend the work order and email to ${inv?.vendor?.companyName ?? 'this vendor'}.`
                          : `This will generate a work order PDF and send it via email to ${inv?.vendor?.companyName ?? 'this vendor'}.`}
                      </p>
                      {inv?.vendor?.email && (
                        <p className="text-sm text-gray-500 mb-4">
                          Email will be sent to <strong>{inv.vendor.email}</strong>
                        </p>
                      )}
                      <div className="flex justify-end gap-3">
                        <Button variant="secondary" onClick={() => setConfirmSendWoVendorId(null)}>
                          Cancel
                        </Button>
                        <Button
                          loading={sendWorkOrder.isPending}
                          onClick={() => {
                            sendWorkOrder.mutate(confirmSendWoVendorId, {
                              onSuccess: () => setConfirmSendWoVendorId(null),
                            });
                          }}
                        >
                          <PaperAirplaneIcon className="w-3.5 h-3.5 mr-1" />
                          {isResend ? 'Resend' : 'Send Work Order'}
                        </Button>
                      </div>
                    </div>
                  </div>
                );
              })()}

              {/* Confirm Assign Vendor Dialog */}
              {confirmAssignVendor && (
                <div className="fixed inset-0 z-50 flex items-center justify-center">
                  <div className="fixed inset-0 bg-black/30" onClick={() => setConfirmAssignVendor(null)} />
                  <div className="relative bg-white rounded-xl shadow-xl p-6 max-w-md w-full mx-4">
                    <h3 className="text-lg font-semibold text-gray-900 mb-2">Assign {confirmAssignVendor.name}?</h3>
                    <p className="text-sm text-gray-600 mb-4">
                      {confirmAssignVendor.hasQuote
                        ? `This will select ${confirmAssignVendor.name}'s quote and assign them to this work order.`
                        : `No quote on file. You'll enter quote details on behalf of this vendor.`}
                    </p>
                    <div className="flex justify-end gap-3">
                      <Button variant="secondary" onClick={() => setConfirmAssignVendor(null)}>
                        Cancel
                      </Button>
                      <Button
                        loading={selectQuote.isPending}
                        onClick={() => {
                          if (confirmAssignVendor.hasQuote && confirmAssignVendor.quoteId) {
                            selectQuote.mutate(confirmAssignVendor.quoteId);
                          } else {
                            setManualQuoteVendor({ id: confirmAssignVendor.id, name: confirmAssignVendor.name });
                            setConfirmAssignVendor(null);
                          }
                        }}
                      >
                        {confirmAssignVendor.hasQuote ? 'Assign' : 'Enter Quote'}
                      </Button>
                    </div>
                  </div>
                </div>
              )}

              {/* Confirm Unassign Vendor Dialog */}
              {confirmUnassignVendor && (
                <div className="fixed inset-0 z-50 flex items-center justify-center">
                  <div className="fixed inset-0 bg-black/30" onClick={() => setConfirmUnassignVendor(null)} />
                  <div className="relative bg-white rounded-xl shadow-xl p-6 max-w-md w-full mx-4">
                    <h3 className="text-lg font-semibold text-gray-900 mb-2">Unassign {confirmUnassignVendor}?</h3>
                    <p className="text-sm text-gray-600 mb-4">
                      This will unassign the vendor and revert all quotes back to submitted status. You can then select a different vendor.
                    </p>
                    <div className="flex justify-end gap-3">
                      <Button variant="secondary" onClick={() => setConfirmUnassignVendor(null)}>
                        Cancel
                      </Button>
                      <Button
                        variant="danger"
                        loading={unselectQuote.isPending}
                        onClick={() => {
                          unselectQuote.mutate(undefined, { onSuccess: () => setConfirmUnassignVendor(null) });
                        }}
                      >
                        Unassign
                      </Button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* ─── PROPOSAL TAB ─── */}
          {activeTab === 'proposal' && (
            <div>
              {(() => {
                const selectedQuote = selectedQuoteIdForProposal
                  ? quotes?.find(q => q.id === selectedQuoteIdForProposal)
                  : proposal?.quoteId ? quotes?.find(q => q.id === proposal.quoteId) : null;

                if (proposalEditMode || (!proposal && selectedQuote)) {
                  if (!selectedQuote) {
                    return (
                      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                        <p className="text-sm text-gray-600">No quote selected. Please go back and select a quote first.</p>
                        <Button variant="secondary" className="mt-3" onClick={() => { setProposalEditMode(false); setSelectedQuoteIdForProposal(null); }}>Back</Button>
                      </div>
                    );
                  }
                  return (
                    <div>
                      <button onClick={() => { setProposalEditMode(false); setSelectedQuoteIdForProposal(null); }} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">&larr; Back to proposal</button>
                      <ProposalBuilder serviceRequestId={id!} quote={selectedQuote} existingProposal={proposal} allQuotes={quotes ?? []} onSuccess={() => { setProposalEditMode(false); setSelectedQuoteIdForProposal(null); }} />
                    </div>
                  );
                }

                if (proposal) {
                  return <ProposalDetail proposal={proposal} serviceRequestId={id!} onEdit={() => setProposalEditMode(true)} />;
                }

                if (isOperator) {
                  const eligible = quotes?.filter(q => q.status === 'Selected' || q.status === 'Submitted') ?? [];
                  if (eligible.length === 0) return <EmptyState title="No quotes available" description="Wait for vendors to submit quotes, then select one to create a proposal." />;
                  return (
                    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                      <p className="text-sm text-gray-600 mb-4">Select a quote to base your proposal on.</p>
                      <div className="space-y-3">
                        {eligible.map(q => (
                          <button key={q.id} onClick={() => setSelectedQuoteIdForProposal(q.id)} className="w-full flex items-center justify-between p-4 rounded-lg border border-gray-200 hover:bg-gray-50 hover:border-brand-300 transition-colors text-left">
                            <div><p className="text-sm font-medium text-gray-900">{q.vendor?.companyName}</p><p className="text-xs text-gray-500">{q.scopeOfWork?.substring(0, 80)}...</p></div>
                            <div className="text-right"><p className="text-sm font-bold text-gray-900">{formatCurrency(q.price)}</p><StatusBadge status={q.status} /></div>
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

          {/* ─── PO & SCHEDULING TAB ─── */}
          {activeTab === 'po-scheduling' && (
            <div className="space-y-6">
              {/* PO Section */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4 flex items-center gap-2">
                  <DocumentTextIcon className="w-5 h-5 text-gray-400" /> Purchase Order
                </h2>
                {sr.poNumber ? (
                  <dl className="space-y-3">
                    <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">PO Number</dt><dd className="mt-1 text-sm font-medium text-gray-900">{sr.poNumber}</dd></div>
                    {sr.poAmount != null && <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">PO Amount</dt><dd className="mt-1 text-sm font-medium text-gray-900">{formatCurrency(sr.poAmount)}</dd></div>}
                    {sr.poFileUrl && (
                      <div>
                        <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">PO File</dt>
                        <dd className="mt-1"><a href={`${API_BASE}${sr.poFileUrl}`} target="_blank" rel="noopener noreferrer" className="text-sm text-brand-600 hover:text-brand-700 underline">View Document</a></dd>
                      </div>
                    )}
                    {sr.poReceivedAt && <div><dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Received</dt><dd className="mt-1 text-sm text-gray-900">{formatDate(sr.poReceivedAt)}</dd></div>}
                  </dl>
                ) : isOperator ? (
                  <form onSubmit={handlePoSubmit(data => {
                    if (!poFile) { toast.error('Please attach a PO file'); return; }
                    uploadPo.mutate({ poNumber: data.poNumber, poAmount: data.poAmount ? parseFloat(data.poAmount) : undefined, file: poFile });
                  })} className="space-y-4">
                    <div>
                      <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">PO Number</label>
                      <input {...registerPo('poNumber')} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" placeholder="e.g. PO-2026-0042" />
                      {poErrors.poNumber && <p className="mt-1 text-xs text-red-600">{poErrors.poNumber.message}</p>}
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">PO Amount (optional)</label>
                      <input {...registerPo('poAmount')} type="number" step="0.01" className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" placeholder="0.00" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">PO Document</label>
                      <input ref={poFileRef} type="file" accept=".pdf,.doc,.docx,.png,.jpg,.jpeg" onChange={e => setPoFile(e.target.files?.[0] ?? null)} className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-brand-50 file:text-brand-700 hover:file:bg-brand-100" />
                      {poFile && <p className="mt-1 text-xs text-gray-500">Selected: {poFile.name}</p>}
                    </div>
                    <Button type="submit" loading={uploadPo.isPending}><ArrowUpTrayIcon className="w-4 h-4 mr-1.5" />Upload PO</Button>
                  </form>
                ) : (
                  <p className="text-sm text-gray-500">No purchase order has been uploaded yet.</p>
                )}
              </div>

              {/* Scheduling Section */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4 flex items-center gap-2">
                  <CalendarDaysIcon className="w-5 h-5 text-gray-400" /> Scheduling
                </h2>
                {sr.scheduleConfirmedAt && (
                  <div className="mb-4 p-3 rounded-lg bg-green-50 border border-green-200">
                    <p className="text-sm text-green-800 font-medium">Schedule confirmed on {formatDate(sr.scheduleConfirmedAt)}</p>
                  </div>
                )}
                {sr.scheduledDate && (
                  <div className="mb-4">
                    <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Scheduled Date</p>
                    <p className="text-sm font-medium text-gray-900">{formatDate(sr.scheduledDate)}</p>
                  </div>
                )}
                {isOperator && (
                  <div className="flex items-end gap-3">
                    <div className="flex-1">
                      <label className="block text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">{sr.scheduledDate ? 'Reschedule' : 'Set Scheduled Date'}</label>
                      <input type="date" value={scheduleDate} onChange={e => setScheduleDate(e.target.value)} className="block w-full rounded-lg border border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2" />
                    </div>
                    <Button onClick={() => { if (!scheduleDate) { toast.error('Pick a date'); return; } saveSchedule.mutate(scheduleDate); }} loading={saveSchedule.isPending} disabled={!scheduleDate}>Save</Button>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* ─── INVOICE TAB ─── */}
          {activeTab === 'invoice' && (() => {
            const matchingInvoices = (invoiceList?.items ?? []).filter(inv => inv.location === sr.location);
            return (
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4 flex items-center gap-2">
                  <CurrencyDollarIcon className="w-5 h-5 text-gray-400" /> Invoice
                </h2>
                {matchingInvoices.length > 0 ? (
                  <div className="space-y-3">
                    {matchingInvoices.map(inv => (
                      <div key={inv.id} className="flex items-center justify-between p-4 rounded-lg border border-gray-200">
                        <div>
                          <p className="text-sm font-medium text-gray-900">{formatCurrency(inv.amount)}</p>
                          <p className="text-xs text-gray-500">{inv.clientName} &middot; {inv.location}</p>
                        </div>
                        <div className="flex items-center gap-3">
                          <StatusBadge status={inv.status} />
                          <Button size="sm" variant="secondary" onClick={() => navigate(`/invoices/${inv.id}`)}>View</Button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div>
                    <p className="text-sm text-gray-500 mb-3">No invoice has been created for this work order yet.</p>
                    <Button variant="secondary" onClick={() => navigate('/invoices')}>Go to Invoices</Button>
                  </div>
                )}
              </div>
            );
          })()}
        </div>

        {/* ──────── RIGHT PANEL (sidebar) ──────── */}
        <div className="space-y-4">
          {/* Status Card */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Status</h3>
            <div className="flex items-center gap-2 flex-wrap">
              <StatusBadge status={sr.status} />
              {isOperator && allowedTransitions && allowedTransitions.length > 0 && (
                <select
                  value=""
                  onChange={e => { if (e.target.value) updateStatus.mutate(e.target.value as ServiceRequestStatus); }}
                  className="border border-gray-300 rounded-lg text-xs px-2 py-1 focus:ring-brand-500 focus:border-brand-500 bg-white"
                >
                  <option value="">Move to...</option>
                  {allowedTransitions.map(s => <option key={s} value={s}>{STATUS_LABELS[s] ?? s}</option>)}
                </select>
              )}
            </div>
            <div className="mt-3">
              <p className="text-xs text-gray-500 mb-1">Priority</p>
              <div className="flex items-center gap-2">
                {sr.priority === 'Urgent' && (
                  <span className="relative flex h-3 w-3">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-3 w-3 bg-red-500"></span>
                  </span>
                )}
                <PriorityBadge priority={sr.priority} />
              </div>
            </div>
          </div>

          {/* Key Info Card */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Key Info</h3>
            <dl className="space-y-3">
              <div className="flex items-start gap-2">
                <UserGroupIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                <div><dt className="text-xs text-gray-500">Client</dt><dd className="text-sm font-medium text-gray-900">{sr.client?.companyName}</dd></div>
              </div>
              <div className="flex items-start gap-2">
                <MapPinIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                <div><dt className="text-xs text-gray-500">Location</dt><dd className="text-sm font-medium text-gray-900"><a href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(sr.location)}`} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:text-brand-700 hover:underline">{sr.location}</a></dd></div>
              </div>
              {sr.category && (
                <div className="flex items-start gap-2">
                  <TagIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                  <div><dt className="text-xs text-gray-500">Service</dt><dd className="text-sm font-medium text-gray-900">{sr.category}</dd></div>
                </div>
              )}
              <div className="flex items-start gap-2">
                <InformationCircleIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                <div>
                  <dt className="text-xs text-gray-500">Quotes</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.quoteCount}</dd>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <DocumentTextIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                <div>
                  <dt className="text-xs text-gray-500">Proposal</dt>
                  <dd className="text-sm font-medium text-gray-900">{sr.hasProposal ? 'Yes' : 'No'}</dd>
                </div>
              </div>
              {sr.poNumber && (
                <div className="flex items-start gap-2">
                  <CurrencyDollarIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                  <div>
                    <dt className="text-xs text-gray-500">PO</dt>
                    <dd className="text-sm font-medium text-gray-900">{sr.poNumber}{sr.poAmount != null && ` (${formatCurrency(sr.poAmount)})`}</dd>
                  </div>
                </div>
              )}
            </dl>
          </div>

          {/* Dates Card */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Dates</h3>
            <dl className="space-y-2">
              <div className="flex items-center gap-2">
                <ClockIcon className="w-4 h-4 text-gray-400 flex-shrink-0" />
                <div><dt className="text-xs text-gray-500 inline">Created</dt><dd className="text-sm text-gray-900 inline ml-1">{formatDate(sr.createdAt)}</dd></div>
              </div>
              <div className="flex items-center gap-2">
                <ClockIcon className="w-4 h-4 text-gray-400 flex-shrink-0" />
                <div><dt className="text-xs text-gray-500 inline">Updated</dt><dd className="text-sm text-gray-900 inline ml-1">{formatRelativeTime(sr.updatedAt)}</dd></div>
              </div>
              {sr.scheduledDate && (
                <div className="flex items-center gap-2">
                  <CalendarDaysIcon className="w-4 h-4 text-gray-400 flex-shrink-0" />
                  <div><dt className="text-xs text-gray-500 inline">Scheduled</dt><dd className="text-sm text-gray-900 inline ml-1">{formatDate(sr.scheduledDate)}</dd></div>
                </div>
              )}
            </dl>
          </div>

          {/* Alerts */}
          {alerts.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Alerts</h3>
              <div className="space-y-2">
                {alerts.map(a => (
                  <div key={a.text} className={`flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium ${a.color}`}>
                    <ExclamationTriangleIcon className="w-4 h-4 flex-shrink-0" />
                    {a.text}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
