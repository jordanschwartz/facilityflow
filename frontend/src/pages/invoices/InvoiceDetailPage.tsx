import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { invoicesApi } from '../../api/invoices';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatCurrency } from '../../utils/formatters';

export default function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: invoice, isLoading } = useQuery({
    queryKey: ['invoices', id],
    queryFn: () => invoicesApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const sendMutation = useMutation({
    mutationFn: () => invoicesApi.send(id!),
    onSuccess: () => {
      toast.success('Invoice sent');
      queryClient.invalidateQueries({ queryKey: ['invoices', id] });
      queryClient.invalidateQueries({ queryKey: ['invoices'] });
    },
    onError: () => toast.error('Failed to send invoice'),
  });

  const cancelMutation = useMutation({
    mutationFn: () => invoicesApi.cancel(id!),
    onSuccess: () => {
      toast.success('Invoice cancelled');
      queryClient.invalidateQueries({ queryKey: ['invoices', id] });
      queryClient.invalidateQueries({ queryKey: ['invoices'] });
    },
    onError: () => toast.error('Failed to cancel invoice'),
  });

  const handleCancel = () => {
    if (window.confirm('Are you sure you want to cancel this invoice?')) {
      cancelMutation.mutate();
    }
  };

  const handleCopyPaymentLink = () => {
    if (invoice?.stripeInvoiceUrl) {
      navigator.clipboard.writeText(invoice.stripeInvoiceUrl);
      toast.success('Payment link copied to clipboard');
    }
  };

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  if (!invoice) {
    return <EmptyState title="Invoice not found" action={<Button onClick={() => navigate('/invoices')}>Back to Invoices</Button>} />;
  }

  return (
    <div>
      <button onClick={() => navigate('/invoices')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
        ← Back to Invoices
      </button>

      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{formatCurrency(invoice.amount)}</h1>
          <div className="flex items-center gap-3 mt-2">
            <StatusBadge status={invoice.status} />
            <span className="text-sm text-gray-600">{invoice.description}</span>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Main content */}
        <div className="col-span-2 space-y-6">
          {/* Invoice Details */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Invoice Details</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Description</dt>
                <dd className="text-sm text-gray-900">{invoice.description}</dd>
              </div>
              {invoice.notes && (
                <div>
                  <dt className="text-xs text-gray-500">Notes</dt>
                  <dd className="text-sm text-gray-900">{invoice.notes}</dd>
                </div>
              )}
              <div>
                <dt className="text-xs text-gray-500">Location</dt>
                <dd className="text-sm text-gray-900">{invoice.location}</dd>
              </div>
            </dl>
          </div>

          {/* Bill-To */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Bill-To</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Name</dt>
                <dd className="text-sm text-gray-900">{invoice.billToName}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Email</dt>
                <dd className="text-sm text-gray-900">{invoice.billToEmail}</dd>
              </div>
            </dl>
          </div>

          {/* Timeline */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Timeline</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Created</dt>
                <dd className="text-sm text-gray-900">{formatDate(invoice.createdAt)}</dd>
              </div>
              {invoice.sentAt && (
                <div>
                  <dt className="text-xs text-gray-500">Sent</dt>
                  <dd className="text-sm text-gray-900">{formatDate(invoice.sentAt)}</dd>
                </div>
              )}
              {invoice.paidAt && (
                <div>
                  <dt className="text-xs text-gray-500">Paid</dt>
                  <dd className="text-sm text-gray-900">{formatDate(invoice.paidAt)}</dd>
                </div>
              )}
            </dl>
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Invoice Info</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Status</dt>
                <dd className="mt-1"><StatusBadge status={invoice.status} /></dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Amount</dt>
                <dd className="text-sm font-medium text-gray-900">{formatCurrency(invoice.amount)}</dd>
              </div>
              {invoice.client && (
                <div>
                  <dt className="text-xs text-gray-500">Client</dt>
                  <dd className="text-sm text-gray-900">{invoice.client.companyName}</dd>
                </div>
              )}
              {invoice.vendorName && (
                <div>
                  <dt className="text-xs text-gray-500">Vendor</dt>
                  <dd className="text-sm text-gray-900">{invoice.vendorName}</dd>
                </div>
              )}
              <div>
                <dt className="text-xs text-gray-500">Work Order</dt>
                <dd className="text-sm">
                  <Link to={`/work-orders/${invoice.workOrderId}`} className="text-brand-600 hover:text-brand-700 font-medium">
                    View Work Order
                  </Link>
                </dd>
              </div>
            </dl>
          </div>

          {/* Stripe Invoice Link */}
          {invoice.stripeInvoiceUrl && invoice.status === 'Sent' && (
            <a
              href={invoice.stripeInvoiceUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="block w-full text-center px-4 py-2 text-sm font-medium text-brand-600 bg-brand-50 rounded-lg hover:bg-brand-100 transition-colors"
            >
              View Stripe Invoice
            </a>
          )}

          {/* Action Buttons */}
          <div className="space-y-2">
            {invoice.status === 'Draft' && (
              <>
                <Button className="w-full" onClick={() => sendMutation.mutate()} loading={sendMutation.isPending}>
                  Send Invoice
                </Button>
                <Button className="w-full" variant="danger" onClick={handleCancel} loading={cancelMutation.isPending}>
                  Cancel
                </Button>
              </>
            )}

            {invoice.status === 'Sent' && (
              <>
                {invoice.stripeInvoiceUrl && (
                  <Button className="w-full" variant="secondary" onClick={handleCopyPaymentLink}>
                    Copy Payment Link
                  </Button>
                )}
                <Button className="w-full" variant="danger" onClick={handleCancel} loading={cancelMutation.isPending}>
                  Cancel
                </Button>
              </>
            )}

            {invoice.status === 'Paid' && (
              <div className="flex items-center justify-center gap-2 py-3 px-4 bg-green-50 rounded-lg">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">Paid</span>
                {invoice.paidAt && <span className="text-sm text-green-700">{formatDate(invoice.paidAt)}</span>}
              </div>
            )}

            {invoice.status === 'Cancelled' && (
              <div className="flex items-center justify-center py-3 px-4 bg-red-50 rounded-lg">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">Cancelled</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
