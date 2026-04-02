import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { clientsApi } from '../../api/clients';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline';

export default function ClientListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useQuery({
    queryKey: ['clients', { search, page }],
    queryFn: () => clientsApi.list({ search: search || undefined, page, pageSize }).then(r => r.data),
  });

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div>
      <PageHeader
        title="Clients"
        subtitle={`${totalCount} registered clients`}
        actions={<Button onClick={() => navigate('/clients/new')}>+ New Client</Button>}
      />

      <div className="flex gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search clients..."
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
            className="block w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-brand-500 focus:border-brand-500"
          />
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center h-48"><LoadingSpinner /></div>
        ) : items.length === 0 ? (
          <EmptyState
            title="No clients found"
            description="Add clients to start managing service requests"
            action={<Button onClick={() => navigate('/clients/new')}>New Client</Button>}
          />
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead>
              <tr className="bg-gray-100 border-b border-gray-300">
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Company</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Phone</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Address</th>
                <th className="px-4 py-2.5 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {items.map((client, idx) => (
                <tr key={client.id} onClick={() => navigate(`/clients/${client.id}`)} className={`hover:bg-blue-50/50 transition-colors cursor-pointer ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}>
                  <td className="px-4 py-2.5">
                    <p className="text-sm font-medium text-gray-900">{client.companyName}</p>
                    <p className="text-xs text-gray-500">{client.user?.email}</p>
                  </td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{client.phone}</td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{client.address}</td>
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
