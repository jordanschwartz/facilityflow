import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { workOrdersApi } from '../../api/workOrders';
import type { WorkOrderStatus } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate } from '../../utils/formatters';

const STATUS_TABS: Array<WorkOrderStatus | ''> = ['', 'Assigned', 'InProgress', 'Completed', 'Closed'];
const STATUS_LABELS: Record<string, string> = {
  '': 'All',
  Assigned: 'Assigned',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Closed: 'Closed',
};

export default function WorkOrderListPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState<WorkOrderStatus | ''>('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useQuery({
    queryKey: ['work-orders', { status, page }],
    queryFn: () => workOrdersApi.list({ status: status || undefined, page, pageSize }).then(r => r.data),
  });

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div>
      <PageHeader
        title="Work Orders"
        subtitle={`${totalCount} total work orders`}
      />

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
        {isLoading ? (
          <div className="flex items-center justify-center h-48"><LoadingSpinner /></div>
        ) : items.length === 0 ? (
          <EmptyState title="No work orders found" description="Work orders are created when proposals are approved" />
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead>
              <tr className="bg-gray-100 border-b border-gray-300">
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Service Request</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Vendor</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Priority</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Status</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Completed</th>
                <th className="px-4 py-2.5 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {items.map((wo, idx) => (
                <tr key={wo.id} onClick={() => navigate(`/work-orders/${wo.id}`)} className={`hover:bg-blue-50/50 transition-colors cursor-pointer ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                  <td className="px-4 py-2.5">
                    <p className="text-sm font-medium text-gray-900">{wo.serviceRequest?.title}</p>
                    <p className="text-xs text-gray-500">{wo.serviceRequest?.client?.companyName}</p>
                  </td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{wo.vendor?.companyName}</td>
                  <td className="px-4 py-2.5"><PriorityBadge priority={wo.serviceRequest?.priority} /></td>
                  <td className="px-4 py-2.5"><StatusBadge status={wo.status} /></td>
                  <td className="px-4 py-2.5 text-sm text-gray-500">{wo.completedAt ? formatDate(wo.completedAt) : '—'}</td>
                  <td className="px-4 py-2.5 text-right">
                    <span className="text-brand-600 text-sm font-medium">View</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-gray-600">
            Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount}
          </p>
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
            <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>Next</Button>
          </div>
        </div>
      )}
    </div>
  );
}
