import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
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

const STATUSES: ServiceRequestStatus[] = ['New', 'Sourcing', 'Quoting', 'PendingApproval', 'Approved', 'Rejected', 'Completed'];
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
        title="Service Requests"
        subtitle={`${totalCount} total requests`}
        actions={
          <Button onClick={() => navigate('/requests/new')}>
            + New Request
          </Button>
        }
      />

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
          value={status}
          onChange={e => { setStatus(e.target.value); setPage(1); }}
          className="border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
        >
          <option value="">All Statuses</option>
          {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
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
            title="No requests found"
            description="Try adjusting your filters or create a new request"
            action={<Button onClick={() => navigate('/requests/new')}>New Request</Button>}
          />
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead>
              <tr className="bg-gray-50">
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Title</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Client</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Category</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Priority</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {filteredItems.map(item => (
                <tr key={item.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <p className="text-sm font-medium text-gray-900">{item.title}</p>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">{item.client?.companyName}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">—</td>
                  <td className="px-6 py-4"><PriorityBadge priority={item.priority} /></td>
                  <td className="px-6 py-4"><StatusBadge status={item.status} /></td>
                  <td className="px-6 py-4 text-sm text-gray-500">{formatDate(item.createdAt)}</td>
                  <td className="px-6 py-4 text-right">
                    <Link to={`/requests/${item.id}`} className="text-brand-600 hover:text-brand-700 text-sm font-medium">
                      View
                    </Link>
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
