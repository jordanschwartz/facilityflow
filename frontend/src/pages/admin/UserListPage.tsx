import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { usersApi } from '../../api/users';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import StatusBadge from '../../components/ui/StatusBadge';
import Badge from '../../components/ui/Badge';
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline';
import { formatDate, formatRelativeTime } from '../../utils/formatters';

const roleColors: Record<string, string> = {
  Admin: 'bg-purple-100 text-purple-700',
  Operator: 'bg-blue-100 text-blue-700',
  Client: 'bg-gray-100 text-gray-700',
  Vendor: 'bg-green-100 text-green-700',
};

export default function UserListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useQuery({
    queryKey: ['users', { search, page }],
    queryFn: () => usersApi.list({ search: search || undefined, page, pageSize }).then(r => r.data),
  });

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div>
      <PageHeader
        title="User Management"
        subtitle={`${totalCount} users`}
        actions={<Button onClick={() => navigate('/admin/users/new')}>+ Add User</Button>}
      />

      <div className="flex gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search by name or email..."
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
            title="No users found"
            description="Add users to manage access to the platform"
            action={<Button onClick={() => navigate('/admin/users/new')}>Add User</Button>}
          />
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead>
              <tr className="bg-gray-100 border-b border-gray-300">
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Name</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Email</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Role</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Status</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Created</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Last Login</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {items.map((user, idx) => (
                <tr
                  key={user.id}
                  onClick={() => navigate(`/admin/users/${user.id}`)}
                  className={`hover:bg-blue-50/50 transition-colors cursor-pointer ${idx % 2 === 1 ? 'bg-gray-50/50' : ''}`}
                >
                  <td className="px-4 py-2.5">
                    <p className="text-sm font-medium text-gray-900">{user.firstName} {user.lastName}</p>
                  </td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{user.email}</td>
                  <td className="px-4 py-2.5"><Badge label={user.role} className={roleColors[user.role] ?? 'bg-gray-100 text-gray-700'} /></td>
                  <td className="px-4 py-2.5"><StatusBadge status={user.status} /></td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{formatDate(user.createdAt)}</td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">{user.lastLoginAt ? formatRelativeTime(user.lastLoginAt) : 'Never'}</td>
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
