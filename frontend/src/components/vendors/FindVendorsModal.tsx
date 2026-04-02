import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { vendorsApi } from '../../api/vendors';
import type { VendorSourcingResult } from '../../types';
import Modal from '../ui/Modal';
import LoadingSpinner from '../ui/LoadingSpinner';
import Button from '../ui/Button';
import { ExclamationTriangleIcon } from '@heroicons/react/24/solid';
import { formatDate } from '../../utils/formatters';

interface FindVendorsModalProps {
  isOpen: boolean;
  onClose: () => void;
  serviceRequestZip: string;
  requiredTrade?: string;
  onSelectVendor: (vendor: VendorSourcingResult) => void;
}

export default function FindVendorsModal({
  isOpen,
  onClose,
  serviceRequestZip,
  requiredTrade,
  onSelectVendor,
}: FindVendorsModalProps) {
  const [tradeFilter, setTradeFilter] = useState(requiredTrade ?? '');
  const [radiusOverride, setRadiusOverride] = useState('');

  const { data: vendors, isLoading } = useQuery({
    queryKey: ['vendors', 'nearby', serviceRequestZip, tradeFilter, radiusOverride],
    queryFn: () =>
      vendorsApi.getNearbyVendors(
        serviceRequestZip,
        radiusOverride ? parseInt(radiusOverride, 10) : undefined,
        tradeFilter || undefined,
      ).then(r => r.data),
    enabled: isOpen && !!serviceRequestZip,
  });

  return (
    <Modal open={isOpen} onClose={onClose} title="Find Vendors" size="lg">
      <div className="space-y-4">
        {/* Filter controls */}
        <div className="flex gap-3">
          <input
            type="text"
            placeholder="Filter by trade..."
            value={tradeFilter}
            onChange={e => setTradeFilter(e.target.value)}
            className="flex-1 border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          />
          <div className="relative">
            <input
              type="number"
              placeholder="Radius (mi)"
              value={radiusOverride}
              onChange={e => setRadiusOverride(e.target.value)}
              min={1}
              max={500}
              className="w-32 border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
            />
          </div>
        </div>

        <p className="text-xs text-gray-500">
          Searching near ZIP <span className="font-medium text-gray-700">{serviceRequestZip}</span>
          {tradeFilter && <> for <span className="font-medium text-gray-700">{tradeFilter}</span></>}
        </p>

        {/* Results */}
        <div className="max-h-[480px] overflow-y-auto space-y-3 pr-1">
          {isLoading ? (
            <div className="flex items-center justify-center h-32">
              <LoadingSpinner />
            </div>
          ) : (vendors ?? []).length === 0 ? (
            <div className="flex items-center justify-center h-32 text-center">
              <p className="text-sm text-gray-500">No vendors found for this area and trade.</p>
            </div>
          ) : (
            vendors?.map(vendor => (
              <VendorCard
                key={vendor.vendorId}
                vendor={vendor}
                onSelect={onSelectVendor}
              />
            ))
          )}
        </div>
      </div>
    </Modal>
  );
}

function VendorCard({
  vendor,
  onSelect,
}: {
  vendor: VendorSourcingResult;
  onSelect: (v: VendorSourcingResult) => void;
}) {
  return (
    <div className={`rounded-lg border p-4 ${vendor.isDnu ? 'border-red-200 bg-red-50' : 'border-gray-200 bg-white hover:bg-gray-50'}`}>
      {vendor.isDnu && (
        <div className="flex items-center gap-2 mb-3 px-3 py-2 bg-red-100 rounded-md border border-red-200">
          <ExclamationTriangleIcon className="w-4 h-4 text-red-600 flex-shrink-0" />
          <p className="text-xs font-medium text-red-700">
            Do Not Use{vendor.dnuReason ? `: ${vendor.dnuReason}` : ''}
          </p>
        </div>
      )}

      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-gray-900">{vendor.companyName}</p>
          <p className="text-xs text-gray-600 mt-0.5">
            {vendor.primaryContactName}
            {' · '}
            <a href={`mailto:${vendor.email}`} className="text-brand-600 hover:text-brand-700" onClick={e => e.stopPropagation()}>
              {vendor.email}
            </a>
          </p>
          <p className="text-xs text-gray-500 mt-0.5">
            {vendor.serviceRadiusMiles} mi radius from {vendor.primaryZip}
          </p>

          {vendor.trades.length > 0 && (
            <div className="flex flex-wrap gap-1 mt-2">
              {vendor.trades.map(t => (
                <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700">{t}</span>
              ))}
            </div>
          )}

          <div className="flex items-center gap-3 mt-2 text-xs text-gray-500">
            <span>{vendor.completedJobCount} completed {vendor.completedJobCount === 1 ? 'job' : 'jobs'}</span>
            <span>·</span>
            <span>
              Last used: {vendor.lastUsedDate ? formatDate(vendor.lastUsedDate) : 'Never'}
            </span>
          </div>
        </div>

        <div className="flex-shrink-0">
          {vendor.isDnu ? (
            <span title="Cannot select a Do Not Use vendor" className="inline-block">
              <Button size="sm" disabled variant="secondary">
                Select
              </Button>
            </span>
          ) : (
            <Button size="sm" onClick={() => onSelect(vendor)}>
              Select
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
