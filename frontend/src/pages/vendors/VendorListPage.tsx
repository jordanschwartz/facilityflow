import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { vendorsApi } from '../../api/vendors';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { SourceVendorsModal } from '../../components/vendors/FindVendorsModal';
import { MagnifyingGlassIcon, StarIcon } from '@heroicons/react/24/outline';
import { StarIcon as StarSolidIcon } from '@heroicons/react/24/solid';

export default function VendorListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [trade, setTrade] = useState('');
  const [zip, setZip] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('Active');
  const [hideDnu, setHideDnu] = useState(true);
  const [page, setPage] = useState(1);
  const [discoverOpen, setDiscoverOpen] = useState(false);
  const pageSize = 20;

  const { data, isLoading } = useQuery({
    queryKey: ['vendors', { search, trade, zip, statusFilter, hideDnu, page }],
    queryFn: () => vendorsApi.list({
      search: search || undefined,
      trade: trade || undefined,
      zip: zip || undefined,
      activeOnly: statusFilter === 'Active' ? true : undefined,
      hideDnu,
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
        subtitle={`${totalCount} vendors`}
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => setDiscoverOpen(true)}>Source New Vendors</Button>
            <Button onClick={() => navigate('/vendors/new')}>+ New Vendor</Button>
          </div>
        }
      />

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-6">
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
          placeholder="Filter by service..."
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
        <select
          value={statusFilter}
          onChange={e => { setStatusFilter(e.target.value); setPage(1); }}
          className="border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
        >
          <option value="All">All Statuses</option>
          <option value="Active">Active</option>
          <option value="Prospect">Prospect</option>
          <option value="Inactive">Inactive</option>
          <option value="Dnu">Do Not Use</option>
        </select>
        <label className="flex items-center gap-2 px-3 py-2 bg-white border border-gray-300 rounded-lg text-sm cursor-pointer hover:bg-gray-50">
          <input
            type="checkbox"
            checked={hideDnu}
            onChange={e => { setHideDnu(e.target.checked); setPage(1); }}
            className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
          />
          Hide DNU
        </label>
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
              <tr className="bg-gray-100 border-b border-gray-300">
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Company</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Contact</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Services</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Service Area</th>
                <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Rating</th>
                <th className="px-4 py-2.5 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {items.map((vendor, idx) => (
                <tr key={vendor.id} onClick={() => navigate(`/vendors/${vendor.id}`)} className={`hover:bg-blue-50/50 transition-colors cursor-pointer ${idx % 2 === 1 ? 'bg-gray-50/50' : ''} ${!vendor.isActive ? 'opacity-70' : ''}`}>
                  <td className="px-4 py-2.5">
                    <div className="flex items-center gap-2 flex-wrap">
                      <p className="text-sm font-medium text-gray-900">{vendor.companyName}</p>
                      {vendor.status === 'Prospect' && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-violet-100 text-violet-700 border border-violet-200">
                          Prospect
                        </span>
                      )}
                      {vendor.isDnu && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-red-100 text-red-800 border border-red-200">
                          DNU
                        </span>
                      )}
                      {!vendor.isActive && vendor.status !== 'Prospect' && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">
                          Inactive
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-2.5">
                    <p className="text-sm text-gray-900">{vendor.primaryContactName}</p>
                    <p className="text-xs text-gray-500">{vendor.email}</p>
                  </td>
                  <td className="px-4 py-2.5">
                    <div className="flex flex-wrap gap-1">
                      {vendor.trades.slice(0, 3).map(t => (
                        <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700">{t}</span>
                      ))}
                      {vendor.trades.length > 3 && (
                        <span className="text-xs text-gray-500">+{vendor.trades.length - 3}</span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-2.5 text-sm text-gray-600">
                    {vendor.primaryZip} — {vendor.serviceRadiusMiles} mi
                  </td>
                  <td className="px-4 py-2.5">{renderStars(vendor.rating)}</td>
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
      <SourceVendorsModal isOpen={discoverOpen} onClose={() => setDiscoverOpen(false)} />
    </div>
  );
}
