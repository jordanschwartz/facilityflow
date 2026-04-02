import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { serviceRequestsApi } from '../../api/serviceRequests';
import type { ServiceRequestStatus, Priority } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate } from '../../utils/formatters';
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline';

const STATUS_TABS: Array<ServiceRequestStatus | ''> = ['', 'New', 'Qualifying', 'Sourcing', 'PendingQuotes', 'ProposalReady', 'PendingApproval', 'AwaitingPO', 'JobInProgress', 'JobCompleted', 'Closed'];
const STATUS_LABELS: Record<string, string> = {
  '': 'All',
  New: 'New',
  Qualifying: 'Qualifying',
  Sourcing: 'Sourcing',
  PendingQuotes: 'Pending Quotes',
  ProposalReady: 'Proposal Ready',
  PendingApproval: 'Pending Approval',
  AwaitingPO: 'Awaiting PO',
  JobInProgress: 'In Progress',
  JobCompleted: 'Completed',
  Closed: 'Closed',
};
const PRIORITIES: Priority[] = ['Low', 'Medium', 'High', 'Urgent'];

export default function RequestListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [priority, setPriority] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useQuery({
    queryKey: ['service-requests', { search, status, page }],
    queryFn: () => serviceRequestsApi.list({ search: search || undefined, status: status || undefined, page, pageSize }).then(r => r.data),
  });

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const filteredItems = priority ? items.filter(i => i.priority === priority) : items;

  return (
    <div>
      <PageHeader
        title="Work Orders"
        subtitle={`${totalCount} total work orders`}
        actions={
          <Button onClick={() => navigate('/work-orders/new')}>
            + New Work Order
          </Button>
        }
      />

      {/* Status Tabs */}
      <div className="flex gap-1 mb-4 overflow-x-auto pb-1">
        {STATUS_TABS.map(tab => (
          <button
            key={tab}
            onClick={() => { setStatus(tab); setPage(1); }}
            className={`px-3 py-1.5 text-sm font-medium rounded-lg whitespace-nowrap transition-colors ${
              status === tab
                ? 'bg-brand-600 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            {STATUS_LABELS[tab] ?? tab}
          </button>
        ))}
      </div>

      {/* Filters */}
      <div className="flex gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search requests..."
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
            className="block w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-brand-500 focus:border-brand-500"
          />
        </div>
        <select
          value={priority}
          onChange={e => setPriority(e.target.value)}
          className="border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
        >
          <option value="">All Priorities</option>
          {PRIORITIES.map(p => <option key={p} value={p}>{p}</option>)}
        </select>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center h-48">
            <LoadingSpinner />
          </div>
        ) : filteredItems.length === 0 ? (
          <EmptyState
            title="No work orders found"
            description="Try adjusting your filters or create a new request"
            action={<Button onClick={() => navigate('/work-orders/new')}>New Work Order</Button>}
          />
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead>
              <tr className="bg-gray-100 border-b border-gray-300">
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Title</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Client</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Category</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Priority</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Status</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Created</th>
                <th className="px-4 py-2.5 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {filteredItems.map((item, idx) => (
                <tr key={item.id} onClick={() => navigate(`/work-orders/${item.id}`)} className={`hover:bg-blue-50/50 transition-colors cursor-pointer ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                  <td className="px-4 py-2.5">
                    <p className="text-sm font-medium text-gray-900">{item.title}</p>
                  </td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{item.client?.companyName}</td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">—</td>
                  <td className="px-4 py-2.5"><PriorityBadge priority={item.priority} /></td>
                  <td className="px-4 py-2.5"><StatusBadge status={item.status} /></td>
                  <td className="px-4 py-2.5 text-sm text-gray-500">{formatDate(item.createdAt)}</td>
                  <td className="px-4 py-2.5 text-right">
                    <span className="text-brand-600 text-sm font-medium">View</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-gray-600">
            Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount}
          </p>
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>
              Previous
            </Button>
            <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>
              Next
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
