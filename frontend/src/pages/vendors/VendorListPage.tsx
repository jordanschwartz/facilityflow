import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { vendorsApi } from '../../api/vendors';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { MagnifyingGlassIcon, StarIcon } from '@heroicons/react/24/outline';
import { StarIcon as StarSolidIcon } from '@heroicons/react/24/solid';

export default function VendorListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [trade, setTrade] = useState('');
  const [zip, setZip] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useQuery({
    queryKey: ['vendors', { search, trade, zip, page }],
    queryFn: () => vendorsApi.list({
      search: search || undefined,
      trade: trade || undefined,
      zip: zip || undefined,
      page,
      pageSize,
    }).then(r => r.data),
  });

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const renderStars = (rating?: number) => {
    if (!rating) return <span className="text-xs text-gray-400">No rating</span>;
    return (
      <div className="flex items-center gap-0.5">
        {[1, 2, 3, 4, 5].map(i => (
          i <= rating
            ? <StarSolidIcon key={i} className="w-4 h-4 text-yellow-400" />
            : <StarIcon key={i} className="w-4 h-4 text-gray-300" />
        ))}
        <span className="ml-1 text-xs text-gray-600">{rating.toFixed(1)}</span>
      </div>
    );
  };

  return (
    <div>
      <PageHeader
        title="Vendors"
        subtitle={`${totalCount} registered vendors`}
        actions={<Button onClick={() => navigate('/vendors/new')}>+ New Vendor</Button>}
      />

      {/* Filters */}
      <div className="flex gap-3 mb-6">
        <div className="relative flex-1 max-w-xs">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search vendors..."
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
            className="block w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-brand-500 focus:border-brand-500"
          />
        </div>
        <input
          type="text"
          placeholder="Filter by trade..."
          value={trade}
          onChange={e => { setTrade(e.target.value); setPage(1); }}
          className="border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
        />
        <input
          type="text"
          placeholder="Filter by zip..."
          value={zip}
          onChange={e => { setZip(e.target.value); setPage(1); }}
          className="border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500 w-32"
        />
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center h-48">
            <LoadingSpinner />
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            title="No vendors found"
            description="Add vendors to start sourcing service requests"
            action={<Button onClick={() => navigate('/vendors/new')}>New Vendor</Button>}
          />
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead>
              <tr className="bg-gray-50">
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Company</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Trades</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Zip Codes</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Rating</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {items.map(vendor => (
                <tr key={vendor.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <p className="text-sm font-medium text-gray-900">{vendor.companyName}</p>
                    <p className="text-xs text-gray-500">{vendor.user?.email}</p>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-wrap gap-1">
                      {vendor.trades.map(t => (
                        <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700">{t}</span>
                      ))}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-wrap gap-1">
                      {vendor.zipCodes.slice(0, 4).map(z => (
                        <span key={z} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">{z}</span>
                      ))}
                      {vendor.zipCodes.length > 4 && (
                        <span className="text-xs text-gray-500">+{vendor.zipCodes.length - 4} more</span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">{vendor.phone}</td>
                  <td className="px-6 py-4">{renderStars(vendor.rating)}</td>
                  <td className="px-6 py-4 text-right">
                    <Link to={`/vendors/${vendor.id}`} className="text-brand-600 hover:text-brand-700 text-sm font-medium">
                      View
                    </Link>
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
