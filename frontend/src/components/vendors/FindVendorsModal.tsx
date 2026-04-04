import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { vendorsApi } from '../../api/vendors';
import { serviceRequestsApi } from '../../api/serviceRequests';
import type { VendorSourcingResult, DiscoveredVendor } from '../../types';
import Modal from '../ui/Modal';
import LoadingSpinner from '../ui/LoadingSpinner';
import Button from '../ui/Button';
import toast from 'react-hot-toast';
import { ExclamationTriangleIcon, StarIcon as StarOutlineIcon, MapPinIcon, PhoneIcon, GlobeAltIcon } from '@heroicons/react/24/outline';
import { StarIcon as StarSolidIcon, CheckCircleIcon } from '@heroicons/react/24/solid';
import { formatDate } from '../../utils/formatters';
import { Link } from 'react-router-dom';

interface FindVendorsModalProps {
  isOpen: boolean;
  onClose: () => void;
  serviceRequestZip: string;
  requiredTrade?: string;
  serviceRequestId?: string;
}

const TRADE_OPTIONS = [
  'HVAC', 'Electrical', 'Plumbing', 'Roofing', 'General Maintenance',
  'Painting', 'Landscaping', 'Fire Protection', 'Elevator', 'Security',
];

const RADIUS_OPTIONS = [
  { label: '10 miles', value: 10 },
  { label: '25 miles', value: 25 },
  { label: '50 miles', value: 50 },
  { label: '100 miles', value: 100 },
];

// Extract just the zip code from a location string, or leave blank
const extractZip = (location: string) => {
  const match = location.match(/\b(\d{5})\b/);
  return match ? match[1] : '';
};

type Tab = 'local' | 'discover';

export default function FindVendorsModal({
  isOpen,
  onClose,
  serviceRequestZip,
  requiredTrade,
  serviceRequestId,
}: FindVendorsModalProps) {
  const [activeTab, setActiveTab] = useState<Tab>('local');

  return (
    <Modal open={isOpen} onClose={onClose} title="Find Vendors" size="lg">
      {/* Tab bar */}
      <div className="flex border-b border-gray-200 mb-4 -mt-1">
        <button
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'local'
              ? 'border-brand-600 text-brand-600'
              : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
          }`}
          onClick={() => setActiveTab('local')}
        >
          Your Vendors
        </button>
        <button
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'discover'
              ? 'border-brand-600 text-brand-600'
              : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
          }`}
          onClick={() => setActiveTab('discover')}
        >
          Discover New
        </button>
      </div>

      {activeTab === 'local' ? (
        <LocalVendorsTab
          serviceRequestZip={serviceRequestZip}
          requiredTrade={requiredTrade}
          serviceRequestId={serviceRequestId}
          isOpen={isOpen}
        />
      ) : (
        <DiscoverVendorsTab
          serviceRequestZip={serviceRequestZip}
          requiredTrade={requiredTrade}
          serviceRequestId={serviceRequestId}
        />
      )}
    </Modal>
  );
}

/* ─── Local Vendors Tab ─── */

function LocalVendorsTab({
  serviceRequestZip,
  requiredTrade,
  serviceRequestId,
  isOpen,
}: {
  serviceRequestZip: string;
  requiredTrade?: string;
  serviceRequestId?: string;
  isOpen: boolean;
}) {
  const queryClient = useQueryClient();

  const [tradeFilter, setTradeFilter] = useState('');
  const [nameSearch, setNameSearch] = useState('');
  const [zip, setZip] = useState('');
  const [radius, setRadius] = useState(25);

  const { data: vendors, isLoading } = useQuery({
    queryKey: ['vendors', 'nearby', zip, tradeFilter, radius, nameSearch],
    queryFn: () =>
      vendorsApi.getNearbyVendors(
        zip || undefined,
        zip ? radius : undefined,
        tradeFilter || undefined,
        nameSearch || undefined,
      ).then(r => r.data),
    enabled: isOpen,
  });

  const filtered = vendors;

  const inviteMutation = useMutation({
    mutationFn: (vendorId: string) => serviceRequestsApi.createInvites(serviceRequestId!, [vendorId]),
    onSuccess: () => {
      toast.success('Vendor added');
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'invites'] });
      queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
    },
    onError: () => toast.error('Failed to add vendor'),
  });

  return (
    <div className="space-y-4">
      {/* Filter controls */}
      <div className="flex flex-wrap gap-3">
        <div className="flex-1 min-w-[140px]">
          <label className="block text-xs font-medium text-gray-700 mb-1">Service / Specialty</label>
          <input
            type="text"
            list="local-trade-options"
            value={tradeFilter}
            onChange={e => setTradeFilter(e.target.value)}
            placeholder="e.g. HVAC, Electrical..."
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          />
          <datalist id="local-trade-options">
            {TRADE_OPTIONS.map(t => (
              <option key={t} value={t} />
            ))}
          </datalist>
        </div>
        <div className="flex-1 min-w-[140px]">
          <label className="block text-xs font-medium text-gray-700 mb-1">Vendor Name</label>
          <input
            type="text"
            value={nameSearch}
            onChange={e => setNameSearch(e.target.value)}
            placeholder="Search by name..."
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          />
        </div>
        <div className="w-28">
          <label className="block text-xs font-medium text-gray-700 mb-1">ZIP Code</label>
          <input
            type="text"
            value={zip}
            onChange={e => setZip(e.target.value)}
            maxLength={5}
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          />
        </div>
        <div className="w-32">
          <label className="block text-xs font-medium text-gray-700 mb-1">Radius</label>
          <select
            value={radius}
            onChange={e => setRadius(Number(e.target.value))}
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          >
            {RADIUS_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
      </div>

      <p className="text-xs text-gray-500">
        {zip
          ? <>Searching near ZIP <span className="font-medium text-gray-700">{zip}</span> within {radius} miles</>
          : <>Showing all vendors</>
        }
        {tradeFilter && <> for <span className="font-medium text-gray-700">{tradeFilter}</span></>}
        {nameSearch && <> matching "<span className="font-medium text-gray-700">{nameSearch}</span>"</>}
      </p>

      {/* Results */}
      <div className="max-h-[480px] overflow-y-auto space-y-3 pr-1">
        {isLoading ? (
          <div className="flex items-center justify-center h-32">
            <LoadingSpinner />
          </div>
        ) : (filtered ?? []).length === 0 ? (
          <div className="flex items-center justify-center h-32 text-center">
            <p className="text-sm text-gray-500">No vendors found for this area and trade.</p>
          </div>
        ) : (
          filtered?.map(vendor => (
            <LocalVendorCard
              key={vendor.vendorId}
              vendor={vendor}
              canInvite={!!serviceRequestId}
              isInviting={inviteMutation.isPending && inviteMutation.variables === vendor.vendorId}
              onInvite={() => inviteMutation.mutate(vendor.vendorId)}
            />
          ))
        )}
      </div>
    </div>
  );
}

function LocalVendorCard({
  vendor,
  canInvite,
  isInviting,
  onInvite,
}: {
  vendor: VendorSourcingResult;
  canInvite: boolean;
  isInviting: boolean;
  onInvite: () => void;
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
            {vendor.distanceMiles != null && (
              <span className="ml-2 text-brand-600 font-medium">{vendor.distanceMiles.toFixed(1)} mi away</span>
            )}
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
            <span title="Cannot add a Do Not Use vendor" className="inline-block">
              <Button size="sm" disabled variant="secondary">
                Add
              </Button>
            </span>
          ) : canInvite ? (
            <Button size="sm" onClick={onInvite} loading={isInviting}>
              Add
            </Button>
          ) : (
            <Link
              to={`/vendors/${vendor.vendorId}`}
              className="inline-flex items-center px-3 py-1.5 rounded-lg text-sm font-medium text-brand-600 hover:text-brand-700 border border-brand-200 hover:bg-brand-50"
            >
              View
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}

/* ─── Standalone Source Vendors Modal ─── */

export function SourceVendorsModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  return (
    <Modal open={isOpen} onClose={onClose} title="Source New Vendors" size="lg">
      <DiscoverVendorsTab serviceRequestZip="" />
    </Modal>
  );
}

/* ─── Discover New Vendors Tab ─── */

function DiscoverVendorsTab({
  serviceRequestZip,
  requiredTrade,
  serviceRequestId,
}: {
  serviceRequestZip: string;
  requiredTrade?: string;
  serviceRequestId?: string;
}) {
  const queryClient = useQueryClient();
  const [trade, setTrade] = useState(requiredTrade ?? '');
  const [zip, setZip] = useState(extractZip(serviceRequestZip));
  const [radius, setRadius] = useState(25);
  const [searchTriggered, setSearchTriggered] = useState(false);
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set());
  const [addedVendorMap, setAddedVendorMap] = useState<Map<string, string>>(new Map());

  const { data: results, isFetching, isError, refetch } = useQuery({
    queryKey: ['vendors', 'discover', trade, zip, radius],
    queryFn: () => vendorsApi.discover({ trade, zip, radiusMiles: radius }).then(r => r.data),
    enabled: false,
  });

  const handleSearch = () => {
    setSearchTriggered(true);
    refetch();
  };

  const addProspectMutation = useMutation({
    mutationFn: (vendor: DiscoveredVendor) => {
      // Extract ZIP from address or fall back to search ZIP
      const zipMatch = vendor.address.match(/\b\d{5}\b/);
      const vendorZip = zipMatch ? zipMatch[0] : zip;

      return vendorsApi.addProspect({
        companyName: vendor.businessName,
        phone: vendor.phone,
        primaryZip: vendorZip,
        website: vendor.website,
        rating: vendor.rating,
        reviewCount: vendor.reviewCount,
        googleProfileUrl: vendor.googleProfileUrl,
        trades: trade ? [trade] : undefined,
      });
    },
    onSuccess: (response, vendor) => {
      toast.success('Vendor added as prospect');
      setAddedIds(prev => new Set(prev).add(vendor.businessName));
      if (response.data?.id) {
        setAddedVendorMap(prev => new Map(prev).set(vendor.businessName, response.data.id));
      }
      queryClient.invalidateQueries({ queryKey: ['vendors'] });
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.response?.data || 'Failed to add prospect';
      toast.error(typeof message === 'string' ? message : 'Failed to add prospect');
    },
  });

  const inviteMutation = useMutation({
    mutationFn: (vendorId: string) => serviceRequestsApi.createInvites(serviceRequestId!, [vendorId]),
    onSuccess: () => {
      toast.success('Vendor added');
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'invites'] });
      queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
    },
    onError: () => toast.error('Failed to add vendor'),
  });

  return (
    <div className="space-y-4">
      {/* Search form */}
      <div className="flex flex-wrap gap-3">
        <div className="flex-1 min-w-[160px]">
          <label className="block text-xs font-medium text-gray-700 mb-1">Service / Specialty</label>
          <input
            type="text"
            list="trade-options"
            value={trade}
            onChange={e => setTrade(e.target.value)}
            placeholder="e.g. HVAC, Electrical, Locksmith..."
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          />
          <datalist id="trade-options">
            {TRADE_OPTIONS.map(t => (
              <option key={t} value={t} />
            ))}
          </datalist>
        </div>
        <div className="w-28">
          <label className="block text-xs font-medium text-gray-700 mb-1">ZIP Code</label>
          <input
            type="text"
            value={zip}
            onChange={e => setZip(e.target.value)}
            maxLength={5}
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          />
        </div>
        <div className="w-32">
          <label className="block text-xs font-medium text-gray-700 mb-1">Radius</label>
          <select
            value={radius}
            onChange={e => setRadius(Number(e.target.value))}
            className="block w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
          >
            {RADIUS_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
        <div className="flex items-end">
          <Button onClick={handleSearch} disabled={!trade || !zip} loading={isFetching}>
            Search
          </Button>
        </div>
      </div>

      {/* Results */}
      <div className="max-h-[420px] overflow-y-auto space-y-3 pr-1">
        {isFetching ? (
          /* Skeleton loading cards */
          <div className="space-y-3">
            {[1, 2, 3].map(i => (
              <div key={i} className="rounded-lg border border-gray-200 p-4 animate-pulse">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1 space-y-2">
                    <div className="h-4 bg-gray-200 rounded w-2/3" />
                    <div className="h-3 bg-gray-200 rounded w-1/3" />
                    <div className="h-3 bg-gray-200 rounded w-full" />
                    <div className="h-3 bg-gray-200 rounded w-1/2" />
                  </div>
                  <div className="h-8 w-28 bg-gray-200 rounded-lg" />
                </div>
              </div>
            ))}
          </div>
        ) : isError ? (
          <div className="flex items-center justify-center h-32 text-center">
            <p className="text-sm text-red-600">Search failed. Please try again.</p>
          </div>
        ) : searchTriggered && (results ?? []).length === 0 ? (
          <div className="flex items-center justify-center h-32 text-center">
            <p className="text-sm text-gray-500">No vendors found. Try adjusting your search criteria.</p>
          </div>
        ) : !searchTriggered ? (
          <div className="flex items-center justify-center h-32 text-center">
            <p className="text-sm text-gray-500">Select a trade and ZIP code, then click Search to discover vendors.</p>
          </div>
        ) : (
          results?.map((vendor, idx) => (
            <DiscoveredVendorCard
              key={`${vendor.businessName}-${idx}`}
              vendor={vendor}
              isAdded={addedIds.has(vendor.businessName) || !!vendor.existingVendorId}
              isAdding={addProspectMutation.isPending && addProspectMutation.variables?.businessName === vendor.businessName}
              onAdd={() => addProspectMutation.mutate(vendor)}
              canInvite={!!serviceRequestId}
              addedVendorId={addedVendorMap.get(vendor.businessName) || vendor.existingVendorId}
              isInviting={inviteMutation.isPending}
              onInvite={(vendorId: string) => inviteMutation.mutate(vendorId)}
            />
          ))
        )}
      </div>
    </div>
  );
}

function DiscoveredVendorCard({
  vendor,
  isAdded,
  isAdding,
  onAdd,
  canInvite,
  addedVendorId,
  isInviting,
  onInvite,
}: {
  vendor: DiscoveredVendor;
  isAdded: boolean;
  isAdding: boolean;
  onAdd: () => void;
  canInvite: boolean;
  addedVendorId?: string;
  isInviting: boolean;
  onInvite: (vendorId: string) => void;
}) {
  const truncateUrl = (url: string) => {
    try {
      return new URL(url).hostname.replace('www.', '');
    } catch {
      return url;
    }
  };

  const renderStars = (rating: number) => {
    const full = Math.floor(rating);
    const hasHalf = rating - full >= 0.25;
    return (
      <div className="flex items-center gap-0.5">
        {[1, 2, 3, 4, 5].map(i => (
          i <= full
            ? <StarSolidIcon key={i} className="w-3.5 h-3.5 text-yellow-400" />
            : i === full + 1 && hasHalf
              ? <StarSolidIcon key={i} className="w-3.5 h-3.5 text-yellow-300" />
              : <StarOutlineIcon key={i} className="w-3.5 h-3.5 text-gray-300" />
        ))}
      </div>
    );
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white hover:bg-gray-50 p-4 transition-colors">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-gray-900">{vendor.businessName}</p>

          {/* Rating */}
          {vendor.rating != null && (
            <div className="flex items-center gap-1.5 mt-1">
              {renderStars(vendor.rating)}
              <span className="text-xs font-medium text-gray-700">{vendor.rating.toFixed(1)}</span>
              {vendor.reviewCount != null && (
                <span className="text-xs text-gray-500">({vendor.reviewCount} {vendor.reviewCount === 1 ? 'review' : 'reviews'})</span>
              )}
            </div>
          )}

          {/* Address */}
          <div className="flex items-center gap-1.5 mt-1.5">
            <MapPinIcon className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
            <p className="text-xs text-gray-600">{vendor.address}</p>
          </div>

          {/* Phone */}
          {vendor.phone && (
            <div className="flex items-center gap-1.5 mt-1">
              <PhoneIcon className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
              <a
                href={`tel:${vendor.phone}`}
                className="text-xs text-brand-600 hover:text-brand-700"
                onClick={e => e.stopPropagation()}
              >
                {vendor.phone}
              </a>
            </div>
          )}

          {/* Website */}
          {vendor.website && (
            <div className="flex items-center gap-1.5 mt-1">
              <GlobeAltIcon className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
              <a
                href={vendor.website}
                target="_blank"
                rel="noopener noreferrer"
                className="text-xs text-brand-600 hover:text-brand-700"
                onClick={e => e.stopPropagation()}
              >
                {truncateUrl(vendor.website)}
              </a>
            </div>
          )}

          {/* Google profile link */}
          {vendor.googleProfileUrl && (
            <a
              href={vendor.googleProfileUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-block mt-1.5 text-xs text-gray-500 hover:text-gray-700 underline"
              onClick={e => e.stopPropagation()}
            >
              View on Google
            </a>
          )}
        </div>

        <div className="flex-shrink-0 flex flex-col items-end gap-2">
          {vendor.existingVendorId ? (
            <>
              <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700">
                Already in system
              </span>
              <div className="flex items-center gap-2">
                <Link
                  to={`/vendors/${vendor.existingVendorId}`}
                  className="text-xs text-brand-600 hover:text-brand-700 font-medium"
                  onClick={e => e.stopPropagation()}
                >
                  View vendor
                </Link>
                {canInvite && (
                  <Button size="sm" onClick={() => onInvite(vendor.existingVendorId!)} loading={isInviting}>
                    Add
                  </Button>
                )}
              </div>
            </>
          ) : isAdded ? (
            <div className="flex flex-col items-end gap-2">
              <span className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-sm font-medium text-green-700 bg-green-50 border border-green-200">
                <CheckCircleIcon className="w-4 h-4" />
                Added
              </span>
              {canInvite && addedVendorId && (
                <Button size="sm" onClick={() => onInvite(addedVendorId)} loading={isInviting}>
                  Add
                </Button>
              )}
            </div>
          ) : (
            <Button size="sm" onClick={onAdd} loading={isAdding}>
              Add as Prospect
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
