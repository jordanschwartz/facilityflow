import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { vendorsApi } from '../../api/vendors';
import { serviceRequestsApi } from '../../api/serviceRequests';
import type { VendorNote, VendorPayment } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import Modal from '../../components/ui/Modal';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatCurrency } from '../../utils/formatters';
import { XMarkIcon, PencilIcon, TrashIcon, ArrowUpCircleIcon } from '@heroicons/react/24/outline';

const editSchema = z.object({
  companyName: z.string().min(2, 'Company name required'),
  primaryContactName: z.string().min(2, 'Primary contact name required'),
  email: z.string().email('Valid email required'),
  phone: z.string().optional(),
  primaryZip: z.string().trim().regex(/^\d{5}$/, 'Must be a 5-digit ZIP code'),
  serviceRadiusMiles: z.coerce.number().min(1).max(500),
  isActive: z.boolean(),
});
type EditFormData = z.infer<typeof editSchema>;

const paymentSchema = z.object({
  amount: z.coerce.number().min(0.01, 'Amount required'),
  status: z.enum(['Pending', 'Paid']),
  paidAt: z.string().optional(),
  notes: z.string().optional(),
  workOrderId: z.string().optional(),
});
type PaymentFormData = z.infer<typeof paymentSchema>;

function formatNoteDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: 'numeric', minute: '2-digit',
  }).format(new Date(date));
}

export default function VendorDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState(false);
  const [trades, setTrades] = useState<string[]>([]);
  const [tradeInput, setTradeInput] = useState('');

  // DNU modal state
  const [dnuModalOpen, setDnuModalOpen] = useState(false);
  const [dnuReason, setDnuReason] = useState('');

  // Notes state
  const [showNoteForm, setShowNoteForm] = useState(false);
  const [noteText, setNoteText] = useState('');
  const [noteAttachmentUrl, setNoteAttachmentUrl] = useState('');
  const [noteAttachmentFilename, setNoteAttachmentFilename] = useState('');
  const [deletingNoteId, setDeletingNoteId] = useState<string | null>(null);

  // Payments state
  const [showPaymentForm, setShowPaymentForm] = useState(false);
  const [editingPayment, setEditingPayment] = useState<VendorPayment | null>(null);

  const { data: vendor, isLoading } = useQuery({
    queryKey: ['vendors', id],
    queryFn: () => vendorsApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: notes } = useQuery({
    queryKey: ['vendors', id, 'notes'],
    queryFn: () => vendorsApi.getVendorNotes(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: payments } = useQuery({
    queryKey: ['vendors', id, 'payments'],
    queryFn: () => vendorsApi.getVendorPayments(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: requests } = useQuery({
    queryKey: ['service-requests', { vendorId: id }],
    queryFn: () => serviceRequestsApi.list({ pageSize: 50 }).then(r => r.data),
    enabled: !!id,
  });

  const { register, handleSubmit, formState: { errors, isSubmitting }, reset } = useForm<EditFormData>({
    resolver: zodResolver(editSchema),
    values: vendor
      ? {
          companyName: vendor.companyName,
          primaryContactName: vendor.primaryContactName,
          email: vendor.email,
          phone: vendor.phone ?? '',
          primaryZip: vendor.primaryZip,
          serviceRadiusMiles: vendor.serviceRadiusMiles,
          isActive: vendor.isActive,
        }
      : undefined,
  });

  const { register: registerPayment, handleSubmit: handlePaymentSubmit, reset: resetPayment, formState: { errors: paymentErrors } } = useForm<PaymentFormData>({
    resolver: zodResolver(paymentSchema),
    defaultValues: { status: 'Pending' },
  });

  const handleTagKeyDown = (
    e: KeyboardEvent<HTMLInputElement>,
    inputValue: string,
    setInput: (v: string) => void,
    tags: string[],
    setTags: (t: string[]) => void
  ) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      const val = inputValue.trim();
      if (val && !tags.includes(val)) setTags([...tags, val]);
      setInput('');
    }
  };

  const startEditing = () => {
    if (vendor) setTrades([...vendor.trades]);
    setEditing(true);
  };

  const updateMutation = useMutation({
    mutationFn: (data: EditFormData) =>
      vendorsApi.update(id!, { ...data, phone: data.phone || undefined, trades }),
    onSuccess: () => {
      toast.success('Vendor updated');
      setEditing(false);
      queryClient.invalidateQueries({ queryKey: ['vendors', id] });
    },
    onError: () => toast.error('Failed to update vendor'),
  });

  const toggleDnuMutation = useMutation({
    mutationFn: ({ isDnu, reason }: { isDnu: boolean; reason?: string }) =>
      vendorsApi.toggleDnu(id!, isDnu, reason),
    onSuccess: () => {
      toast.success(vendor?.isDnu ? 'DNU status removed' : 'Vendor marked as Do Not Use');
      setDnuModalOpen(false);
      setDnuReason('');
      queryClient.invalidateQueries({ queryKey: ['vendors', id] });
    },
    onError: () => toast.error('Failed to update DNU status'),
  });

  const promoteMutation = useMutation({
    mutationFn: () => vendorsApi.promote(id!),
    onSuccess: () => {
      toast.success('Vendor promoted to active');
      queryClient.invalidateQueries({ queryKey: ['vendors', id] });
      queryClient.invalidateQueries({ queryKey: ['vendors'] });
    },
    onError: () => toast.error('Failed to promote vendor'),
  });

  const createNoteMutation = useMutation({
    mutationFn: () =>
      vendorsApi.createVendorNote(
        id!,
        noteText,
        noteAttachmentUrl || undefined,
        noteAttachmentFilename || undefined,
      ),
    onSuccess: () => {
      toast.success('Note added');
      setNoteText('');
      setNoteAttachmentUrl('');
      setNoteAttachmentFilename('');
      setShowNoteForm(false);
      queryClient.invalidateQueries({ queryKey: ['vendors', id, 'notes'] });
    },
    onError: () => toast.error('Failed to add note'),
  });

  const deleteNoteMutation = useMutation({
    mutationFn: (noteId: string) => vendorsApi.deleteVendorNote(id!, noteId),
    onSuccess: () => {
      toast.success('Note deleted');
      setDeletingNoteId(null);
      queryClient.invalidateQueries({ queryKey: ['vendors', id, 'notes'] });
    },
    onError: () => toast.error('Failed to delete note'),
  });

  const createPaymentMutation = useMutation({
    mutationFn: (data: PaymentFormData) =>
      vendorsApi.createVendorPayment(id!, {
        amount: data.amount,
        status: data.status,
        paidAt: data.paidAt || undefined,
        notes: data.notes || undefined,
        workOrderId: data.workOrderId || undefined,
      }),
    onSuccess: () => {
      toast.success('Payment recorded');
      setShowPaymentForm(false);
      resetPayment();
      queryClient.invalidateQueries({ queryKey: ['vendors', id, 'payments'] });
    },
    onError: () => toast.error('Failed to record payment'),
  });

  const updatePaymentMutation = useMutation({
    mutationFn: (data: PaymentFormData) =>
      vendorsApi.updateVendorPayment(id!, editingPayment!.id, {
        status: data.status,
        paidAt: data.paidAt || undefined,
        notes: data.notes || undefined,
      }),
    onSuccess: () => {
      toast.success('Payment updated');
      setEditingPayment(null);
      resetPayment();
      queryClient.invalidateQueries({ queryKey: ['vendors', id, 'payments'] });
    },
    onError: () => toast.error('Failed to update payment'),
  });

  const totalPaid = (payments ?? [])
    .filter(p => p.status === 'Paid')
    .reduce((sum, p) => sum + p.amount, 0);

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  if (!vendor) {
    return <EmptyState title="Vendor not found" action={<Button onClick={() => navigate('/vendors')}>Back to Vendors</Button>} />;
  }

  return (
    <div>
      <button onClick={() => navigate('/vendors')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
        ← Back to Vendors
      </button>

      {/* Header */}
      <div className="mb-6">
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-3 flex-wrap">
              <h1 className="text-2xl font-bold text-gray-900">{vendor.companyName}</h1>
              {vendor.status === 'Prospect' && (
                <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold bg-violet-100 text-violet-700 border border-violet-200">
                  Prospect
                </span>
              )}
              {vendor.isDnu && (
                <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold bg-red-100 text-red-800 border border-red-200">
                  DO NOT USE
                </span>
              )}
              {vendor.status !== 'Prospect' && (vendor.isActive ? (
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">Active</span>
              ) : (
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">Inactive</span>
              ))}
            </div>
            <div className="mt-2 space-y-0.5">
              <p className="text-sm text-gray-700 font-medium">{vendor.primaryContactName}</p>
              <a href={`mailto:${vendor.email}`} className="text-sm text-brand-600 hover:text-brand-700">{vendor.email}</a>
              {vendor.phone && <p className="text-sm text-gray-500">{vendor.phone}</p>}
            </div>
            {vendor.isDnu && vendor.dnuReason && (
              <div className="mt-2 px-3 py-2 bg-red-50 border border-red-200 rounded-lg max-w-xl">
                <p className="text-xs text-red-700"><span className="font-semibold">DNU Reason:</span> {vendor.dnuReason}</p>
              </div>
            )}
          </div>
          <div className="flex items-center gap-2">
            {!editing ? (
              <>
                {vendor.status === 'Prospect' && (
                  <Button
                    size="sm"
                    loading={promoteMutation.isPending}
                    onClick={() => promoteMutation.mutate()}
                  >
                    <ArrowUpCircleIcon className="w-4 h-4 mr-1" /> Promote to Active
                  </Button>
                )}
                <Button
                  variant={vendor.isDnu ? 'secondary' : 'danger'}
                  size="sm"
                  onClick={() => setDnuModalOpen(true)}
                >
                  {vendor.isDnu ? 'Remove DNU' : 'Mark Do Not Use'}
                </Button>
                <Button variant="secondary" onClick={startEditing}>
                  <PencilIcon className="w-4 h-4 mr-1" /> Edit
                </Button>
              </>
            ) : (
              <Button variant="ghost" onClick={() => { setEditing(false); reset(); }}>Cancel</Button>
            )}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2 space-y-6">

          {/* Info / Edit Section */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            {!editing ? (
              <dl className="grid grid-cols-2 gap-4">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Primary ZIP</dt>
                  <dd className="mt-1 text-sm text-gray-900">{vendor.primaryZip}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Service Radius</dt>
                  <dd className="mt-1 text-sm text-gray-900">{vendor.serviceRadiusMiles} miles</dd>
                </div>
                <div className="col-span-2">
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Trades</dt>
                  <dd className="mt-1 flex flex-wrap gap-1">
                    {vendor.trades.map(t => (
                      <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700">{t}</span>
                    ))}
                  </dd>
                </div>
                {vendor.rating != null && (
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Rating</dt>
                    <dd className="mt-1 text-sm text-gray-900">{vendor.rating.toFixed(1)} / 5.0{vendor.reviewCount != null && <span className="text-gray-500"> ({vendor.reviewCount} reviews)</span>}</dd>
                  </div>
                )}
                {vendor.website && (
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Website</dt>
                    <dd className="mt-1 text-sm"><a href={vendor.website} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:text-brand-700">{vendor.website.replace(/^https?:\/\/(www\.)?/, '').replace(/\/$/, '')}</a></dd>
                  </div>
                )}
                {vendor.googleProfileUrl && (
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Google Profile</dt>
                    <dd className="mt-1 text-sm"><a href={vendor.googleProfileUrl} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:text-brand-700">View on Google →</a></dd>
                  </div>
                )}
              </dl>
            ) : (
              <form onSubmit={handleSubmit(data => updateMutation.mutate(data))} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Company Name</label>
                    <input type="text" {...register('companyName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.companyName && <p className="mt-1 text-xs text-red-600">{errors.companyName.message}</p>}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Primary Contact Name</label>
                    <input type="text" {...register('primaryContactName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.primaryContactName && <p className="mt-1 text-xs text-red-600">{errors.primaryContactName.message}</p>}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                    <input type="email" {...register('email')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                    <input type="tel" {...register('phone')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Primary ZIP Code</label>
                    <input type="text" {...register('primaryZip')} maxLength={5} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.primaryZip && <p className="mt-1 text-xs text-red-600">{errors.primaryZip.message}</p>}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Service Radius (miles)</label>
                    <input type="number" {...register('serviceRadiusMiles')} min={1} max={500} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.serviceRadiusMiles && <p className="mt-1 text-xs text-red-600">{errors.serviceRadiusMiles.message}</p>}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Trades</label>
                  <div className="flex flex-wrap gap-2 mb-2">
                    {trades.map(t => (
                      <span key={t} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                        {t}
                        <button type="button" onClick={() => setTrades(trades.filter(x => x !== t))}><XMarkIcon className="w-3 h-3" /></button>
                      </span>
                    ))}
                  </div>
                  <input type="text" value={tradeInput} onChange={e => setTradeInput(e.target.value)}
                    onKeyDown={e => handleTagKeyDown(e, tradeInput, setTradeInput, trades, setTrades)}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Add trade (Enter to add)" />
                </div>

                <div className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    {...register('isActive')}
                    className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                  />
                  <label htmlFor="isActive" className="text-sm font-medium text-gray-700">Active vendor</label>
                </div>

                <Button type="submit" loading={isSubmitting || updateMutation.isPending}>Save Changes</Button>
              </form>
            )}
          </div>

          {/* Internal Notes */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-base font-semibold text-gray-900">Internal Notes</h2>
              {!showNoteForm && (
                <Button size="sm" variant="secondary" onClick={() => setShowNoteForm(true)}>Add Note</Button>
              )}
            </div>

            {showNoteForm && (
              <div className="mb-4 p-4 bg-gray-50 rounded-lg border border-gray-200 space-y-3">
                <textarea
                  value={noteText}
                  onChange={e => setNoteText(e.target.value)}
                  rows={3}
                  className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                  placeholder="Add an internal note..."
                />
                <div className="grid grid-cols-2 gap-3">
                  <input
                    type="url"
                    value={noteAttachmentUrl}
                    onChange={e => setNoteAttachmentUrl(e.target.value)}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Attachment URL (optional)"
                  />
                  <input
                    type="text"
                    value={noteAttachmentFilename}
                    onChange={e => setNoteAttachmentFilename(e.target.value)}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Filename (optional)"
                  />
                </div>
                <div className="flex gap-2">
                  <Button size="sm" loading={createNoteMutation.isPending} disabled={!noteText.trim()} onClick={() => createNoteMutation.mutate()}>
                    Save Note
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => { setShowNoteForm(false); setNoteText(''); setNoteAttachmentUrl(''); setNoteAttachmentFilename(''); }}>
                    Cancel
                  </Button>
                </div>
              </div>
            )}

            {(notes ?? []).length === 0 ? (
              <p className="text-sm text-gray-500">No notes yet. Add one to track vendor history.</p>
            ) : (
              <div className="space-y-3">
                {notes?.map(note => (
                  <div key={note.id} className="flex gap-3 p-3 rounded-lg border border-gray-100 hover:bg-gray-50 group">
                    <div className="w-8 h-8 rounded-full bg-brand-100 flex items-center justify-center text-brand-700 text-sm font-medium flex-shrink-0">
                      {note.createdByName?.[0]?.toUpperCase() ?? '?'}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-xs font-medium text-gray-700">{note.createdByName}</span>
                        <span className="text-xs text-gray-400">{formatNoteDate(note.createdAt)}</span>
                      </div>
                      <p className="text-sm text-gray-800 whitespace-pre-wrap">{note.text}</p>
                      {note.attachmentUrl && (
                        <a href={note.attachmentUrl} target="_blank" rel="noopener noreferrer" className="mt-1 text-xs text-brand-600 hover:text-brand-700 inline-flex items-center gap-1">
                          {note.attachmentFilename ?? 'Attachment'}
                        </a>
                      )}
                    </div>
                    <div className="flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                      {deletingNoteId === note.id ? (
                        <div className="flex gap-1">
                          <button
                            onClick={() => deleteNoteMutation.mutate(note.id)}
                            className="text-xs text-red-600 hover:text-red-700 font-medium px-2 py-1 rounded border border-red-200 hover:bg-red-50"
                          >
                            Confirm
                          </button>
                          <button
                            onClick={() => setDeletingNoteId(null)}
                            className="text-xs text-gray-500 hover:text-gray-700 px-2 py-1 rounded border border-gray-200 hover:bg-gray-100"
                          >
                            Cancel
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => setDeletingNoteId(note.id)}
                          className="p-1 text-gray-400 hover:text-red-500 rounded"
                          title="Delete note"
                        >
                          <TrashIcon className="w-4 h-4" />
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Job History — hidden for prospects */}
          {vendor.status !== 'Prospect' && <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Job History</h2>
            {(requests?.items ?? []).length === 0 ? (
              <p className="text-sm text-gray-500">No service requests associated with this vendor</p>
            ) : (
              <div className="space-y-2">
                {requests?.items.slice(0, 10).map(sr => (
                  <div key={sr.id} className="flex items-center justify-between p-3 rounded-lg border border-gray-200 hover:bg-gray-50">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{sr.title}</p>
                      <p className="text-xs text-gray-500">{formatDate(sr.createdAt)}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <PriorityBadge priority={sr.priority} />
                      <StatusBadge status={sr.status} />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>}

          {/* Payment History — hidden for prospects */}
          {vendor.status !== 'Prospect' && <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-base font-semibold text-gray-900">Payment History</h2>
              {!showPaymentForm && !editingPayment && (
                <Button size="sm" variant="secondary" onClick={() => setShowPaymentForm(true)}>Add Payment</Button>
              )}
            </div>

            {(showPaymentForm || editingPayment) && (
              <div className="mb-4 p-4 bg-gray-50 rounded-lg border border-gray-200">
                <h3 className="text-sm font-medium text-gray-700 mb-3">
                  {editingPayment ? 'Edit Payment' : 'Record Payment'}
                </h3>
                <form
                  onSubmit={handlePaymentSubmit(data =>
                    editingPayment
                      ? updatePaymentMutation.mutate(data)
                      : createPaymentMutation.mutate(data)
                  )}
                  className="space-y-3"
                >
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Amount ($)</label>
                      <input
                        type="number"
                        step="0.01"
                        {...registerPayment('amount')}
                        defaultValue={editingPayment?.amount}
                        className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                        placeholder="0.00"
                      />
                      {paymentErrors.amount && <p className="mt-1 text-xs text-red-600">{paymentErrors.amount.message}</p>}
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Status</label>
                      <select
                        {...registerPayment('status')}
                        defaultValue={editingPayment?.status ?? 'Pending'}
                        className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      >
                        <option value="Pending">Pending</option>
                        <option value="Paid">Paid</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Payment Date</label>
                      <input
                        type="date"
                        {...registerPayment('paidAt')}
                        defaultValue={editingPayment?.paidAt ? editingPayment.paidAt.slice(0, 10) : ''}
                        className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      />
                    </div>
                    {!editingPayment && (
                      <div>
                        <label className="block text-xs font-medium text-gray-700 mb-1">Work Order ID</label>
                        <input
                          type="text"
                          {...registerPayment('workOrderId')}
                          className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                          placeholder="Optional"
                        />
                      </div>
                    )}
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
                    <textarea
                      {...registerPayment('notes')}
                      defaultValue={editingPayment?.notes ?? ''}
                      rows={2}
                      className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      placeholder="Optional notes..."
                    />
                  </div>
                  <div className="flex gap-2">
                    <Button
                      type="submit"
                      size="sm"
                      loading={createPaymentMutation.isPending || updatePaymentMutation.isPending}
                    >
                      {editingPayment ? 'Update Payment' : 'Save Payment'}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        setShowPaymentForm(false);
                        setEditingPayment(null);
                        resetPayment();
                      }}
                    >
                      Cancel
                    </Button>
                  </div>
                </form>
              </div>
            )}

            {(payments ?? []).length === 0 ? (
              <p className="text-sm text-gray-500">No payment records yet.</p>
            ) : (
              <>
                <div className="overflow-hidden rounded-lg border border-gray-200">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead>
                      <tr className="bg-gray-50">
                        <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
                        <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                        <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                        <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Notes</th>
                        <th className="px-4 py-2.5" />
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {payments?.map(payment => (
                        <tr key={payment.id} className="hover:bg-gray-50">
                          <td className="px-4 py-3 text-sm font-medium text-gray-900">{formatCurrency(payment.amount)}</td>
                          <td className="px-4 py-3">
                            <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                              payment.status === 'Paid'
                                ? 'bg-green-100 text-green-800'
                                : 'bg-yellow-100 text-yellow-800'
                            }`}>
                              {payment.status}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-sm text-gray-500">
                            {payment.paidAt ? formatDate(payment.paidAt) : '—'}
                          </td>
                          <td className="px-4 py-3 text-sm text-gray-500 max-w-xs truncate">{payment.notes ?? '—'}</td>
                          <td className="px-4 py-3 text-right">
                            <button
                              onClick={() => {
                                setEditingPayment(payment);
                                setShowPaymentForm(false);
                                resetPayment();
                              }}
                              className="text-xs text-brand-600 hover:text-brand-700 font-medium"
                            >
                              Edit
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                {totalPaid > 0 && (
                  <div className="mt-3 flex justify-end">
                    <p className="text-sm font-semibold text-gray-700">
                      Total Paid: <span className="text-green-700">{formatCurrency(totalPaid)}</span>
                    </p>
                  </div>
                )}
              </>
            )}
          </div>}
        </div>

        {/* Right sidebar */}
        <div className="space-y-6">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Quick Info</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Contact</dt>
                <dd className="text-sm text-gray-900">{vendor.primaryContactName}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Email</dt>
                <dd className="text-sm">
                  <a href={`mailto:${vendor.email}`} className="text-brand-600 hover:text-brand-700">{vendor.email}</a>
                </dd>
              </div>
              {vendor.phone && (
                <div>
                  <dt className="text-xs text-gray-500">Phone</dt>
                  <dd className="text-sm text-gray-900">{vendor.phone}</dd>
                </div>
              )}
              <div>
                <dt className="text-xs text-gray-500">Service Area</dt>
                <dd className="text-sm text-gray-900">{vendor.primaryZip} — {vendor.serviceRadiusMiles} mi radius</dd>
              </div>
              {vendor.rating != null && (
                <div>
                  <dt className="text-xs text-gray-500">Rating</dt>
                  <dd className="text-sm text-gray-900">{vendor.rating.toFixed(1)} / 5.0</dd>
                </div>
              )}
              {vendor.googleProfileUrl && (
                <div>
                  <dt className="text-xs text-gray-500">Google</dt>
                  <dd className="text-sm"><a href={vendor.googleProfileUrl} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:text-brand-700">View on Google →</a></dd>
                </div>
              )}
            </dl>
          </div>
        </div>
      </div>

      {/* DNU Modal */}
      <Modal
        open={dnuModalOpen}
        onClose={() => { setDnuModalOpen(false); setDnuReason(''); }}
        title={vendor.isDnu ? 'Remove Do Not Use Status' : 'Mark as Do Not Use'}
        size="sm"
      >
        <div className="space-y-4">
          {!vendor.isDnu && (
            <>
              <p className="text-sm text-gray-600">
                Marking this vendor as Do Not Use will flag them across the system. You can optionally provide a reason.
              </p>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reason (optional)</label>
                <textarea
                  value={dnuReason}
                  onChange={e => setDnuReason(e.target.value)}
                  rows={3}
                  className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                  placeholder="e.g. Poor quality work, missed deadlines..."
                />
              </div>
            </>
          )}
          {vendor.isDnu && (
            <p className="text-sm text-gray-600">
              This will remove the Do Not Use flag from <strong>{vendor.companyName}</strong> and allow them to be sourced again.
            </p>
          )}
          <div className="flex justify-end gap-3">
            <Button
              variant="secondary"
              onClick={() => { setDnuModalOpen(false); setDnuReason(''); }}
            >
              Cancel
            </Button>
            <Button
              variant={vendor.isDnu ? 'primary' : 'danger'}
              loading={toggleDnuMutation.isPending}
              onClick={() => toggleDnuMutation.mutate({
                isDnu: !vendor.isDnu,
                reason: dnuReason || undefined,
              })}
            >
              {vendor.isDnu ? 'Remove DNU Status' : 'Mark Do Not Use'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
