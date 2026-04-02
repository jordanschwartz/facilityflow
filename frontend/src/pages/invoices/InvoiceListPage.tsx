import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { invoicesApi } from '../../api/invoices';
import type { InvoiceStatus, BillableWorkOrder } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import StatusBadge from '../../components/ui/StatusBadge';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatCurrency } from '../../utils/formatters';
import CreateInvoiceModal from './CreateInvoiceModal';

const STATUS_TABS: Array<InvoiceStatus | ''> = ['', 'Draft', 'Sent', 'Paid', 'Cancelled'];
const STATUS_LABELS: Record<string, string> = {
  '': 'All',
  Draft: 'Draft',
  Sent: 'Sent',
  Paid: 'Paid',
  Cancelled: 'Cancelled',
};

type TopTab = 'invoices' | 'ready';

export default function InvoiceListPage() {
  const [topTab, setTopTab] = useState<TopTab>('invoices');
  const [status, setStatus] = useState<InvoiceStatus | ''>('');
  const [page, setPage] = useState(1);
  const [billablePage, setBillablePage] = useState(1);
  const [selectedWo, setSelectedWo] = useState<BillableWorkOrder | null>(null);
  const pageSize = 20;

  const { data: invoiceData, isLoading: invoicesLoading } = useQuery({
    queryKey: ['invoices', { status, page }],
    queryFn: () => invoicesApi.list({ status: status || undefined, page, pageSize }).then(r => r.data),
    enabled: topTab === 'invoices',
  });

  const { data: billableData, isLoading: billableLoading } = useQuery({
    queryKey: ['billable-work-orders', { page: billablePage }],
    queryFn: () => invoicesApi.billableWorkOrders({ page: billablePage, pageSize }).then(r => r.data),
    enabled: topTab === 'ready',
  });

  const invoiceItems = invoiceData?.items ?? [];
  const invoiceTotalCount = invoiceData?.totalCount ?? 0;
  const invoiceTotalPages = Math.ceil(invoiceTotalCount / pageSize);

  const billableItems = billableData?.items ?? [];
  const billableTotalCount = billableData?.totalCount ?? 0;
  const billableTotalPages = Math.ceil(billableTotalCount / pageSize);

  return (
    <div>
      <PageHeader
        title="Invoices"
        subtitle={topTab === 'invoices' ? `${invoiceTotalCount} total invoices` : `${billableTotalCount} ready to invoice`}
      />

      {/* Top-level tab switcher */}
      <div className="flex gap-2 mb-6">
        <button
          onClick={() => setTopTab('invoices')}
          className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
            topTab === 'invoices' ? 'bg-brand-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          }`}
        >
          Invoices
        </button>
        <button
          onClick={() => setTopTab('ready')}
          className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
            topTab === 'ready' ? 'bg-brand-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          }`}
        >
          Ready to Invoice
        </button>
      </div>

      {topTab === 'invoices' && (
        <>
          {/* Status Tabs */}
          <div className="border-b border-gray-200 mb-6">
            <nav className="-mb-px flex gap-6">
              {STATUS_TABS.map(s => (
                <button
                  key={s}
                  onClick={() => { setStatus(s); setPage(1); }}
                  className={`py-3 text-sm font-medium border-b-2 transition-colors ${
                    status === s
                      ? 'border-brand-600 text-brand-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  {STATUS_LABELS[s]}
                </button>
              ))}
            </nav>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            {invoicesLoading ? (
              <div className="flex items-center justify-center h-48"><LoadingSpinner /></div>
            ) : invoiceItems.length === 0 ? (
              <EmptyState title="No invoices found" description="Create invoices from completed work orders in the Ready to Invoice tab" />
            ) : (
              <table className="min-w-full divide-y divide-gray-200">
                <thead>
                  <tr className="bg-gray-100 border-b border-gray-300">
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Client Name</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Location</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Completed</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Amount</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Status</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Sent</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Paid</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Bill-To Email</th>
                    <th className="px-4 py-2.5 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {invoiceItems.map((inv, idx) => (
                    <tr key={inv.id} className={`hover:bg-blue-50/50 transition-colors ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                      <td className="px-4 py-2.5 text-sm font-medium text-gray-900">{inv.clientName}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-700">{inv.location}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-600">{inv.completedAt ? formatDate(inv.completedAt) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm font-semibold text-gray-900">{formatCurrency(inv.amount)}</td>
                      <td className="px-4 py-2.5"><StatusBadge status={inv.status} /></td>
                      <td className="px-4 py-2.5 text-sm text-gray-600">{inv.sentAt ? formatDate(inv.sentAt) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-600">{inv.paidAt ? formatDate(inv.paidAt) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-600">{inv.billToEmail}</td>
                      <td className="px-4 py-2.5 text-right">
                        <Link to={`/invoices/${inv.id}`} className="text-brand-600 hover:text-brand-700 text-sm font-medium">View</Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {invoiceTotalPages > 1 && (
            <div className="mt-4 flex items-center justify-between">
              <p className="text-sm text-gray-600">
                Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, invoiceTotalCount)} of {invoiceTotalCount}
              </p>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
                <Button variant="secondary" size="sm" disabled={page >= invoiceTotalPages} onClick={() => setPage(p => p + 1)}>Next</Button>
              </div>
            </div>
          )}
        </>
      )}

      {topTab === 'ready' && (
        <>
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            {billableLoading ? (
              <div className="flex items-center justify-center h-48"><LoadingSpinner /></div>
            ) : billableItems.length === 0 ? (
              <EmptyState title="No billable work orders" description="Completed work orders without invoices will appear here" />
            ) : (
              <table className="min-w-full divide-y divide-gray-200">
                <thead>
                  <tr className="bg-gray-100 border-b border-gray-300">
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Title</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Client</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Location</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Completed</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Proposal Amount</th>
                    <th className="px-4 py-2.5 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {billableItems.map((wo, idx) => (
                    <tr key={wo.id} className={`hover:bg-blue-50/50 transition-colors ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                      <td className="px-4 py-2.5 text-sm font-medium text-gray-900">{wo.title}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-700">{wo.clientName}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-700">{wo.location}</td>
                      <td className="px-4 py-2.5 text-sm text-gray-600">{wo.completedAt ? formatDate(wo.completedAt) : '—'}</td>
                      <td className="px-4 py-2.5 text-sm font-semibold text-gray-900">{wo.proposalAmount != null ? formatCurrency(wo.proposalAmount) : '—'}</td>
                      <td className="px-4 py-2.5 text-right">
                        <Button size="sm" onClick={() => setSelectedWo(wo)}>Create Invoice</Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {billableTotalPages > 1 && (
            <div className="mt-4 flex items-center justify-between">
              <p className="text-sm text-gray-600">
                Showing {(billablePage - 1) * pageSize + 1}–{Math.min(billablePage * pageSize, billableTotalCount)} of {billableTotalCount}
              </p>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" disabled={billablePage <= 1} onClick={() => setBillablePage(p => p - 1)}>Previous</Button>
                <Button variant="secondary" size="sm" disabled={billablePage >= billableTotalPages} onClick={() => setBillablePage(p => p + 1)}>Next</Button>
              </div>
            </div>
          )}
        </>
      )}

      {selectedWo && (
        <CreateInvoiceModal workOrder={selectedWo} onClose={() => setSelectedWo(null)} />
      )}
    </div>
  );
}
