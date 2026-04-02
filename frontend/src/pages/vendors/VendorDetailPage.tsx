import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { vendorsApi } from '../../api/vendors';
import { serviceRequestsApi } from '../../api/serviceRequests';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate } from '../../utils/formatters';
import { XMarkIcon, PencilIcon } from '@heroicons/react/24/outline';

const schema = z.object({
  companyName: z.string().min(2, 'Company name required'),
  phone: z.string().min(7, 'Phone required'),
});
type FormData = z.infer<typeof schema>;

export default function VendorDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [trades, setTrades] = useState<string[]>([]);
  const [tradeInput, setTradeInput] = useState('');
  const [zipCodes, setZipCodes] = useState<string[]>([]);
  const [zipInput, setZipInput] = useState('');

  const { data: vendor, isLoading } = useQuery({
    queryKey: ['vendors', id],
    queryFn: () => vendorsApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: requests } = useQuery({
    queryKey: ['service-requests', { vendorId: id }],
    queryFn: () => serviceRequestsApi.list({ pageSize: 50 }).then(r => r.data),
    enabled: !!id,
  });

  const { register, handleSubmit, formState: { errors, isSubmitting }, reset } = useForm<FormData>({
    resolver: zodResolver(schema),
    values: vendor ? { companyName: vendor.companyName, phone: vendor.phone } : undefined,
  });

  const startEditing = () => {
    if (vendor) {
      setTrades([...vendor.trades]);
      setZipCodes([...vendor.zipCodes]);
    }
    setEditing(true);
  };

  const handleTagKeyDown = (
    e: KeyboardEvent<HTMLInputElement>,
    inputValue: string,
    setInput: (v: string) => void,
    tags: string[],
    setTags: (t: string[]) => void
  ) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      const val = inputValue.trim();
      if (val && !tags.includes(val)) setTags([...tags, val]);
      setInput('');
    }
  };

  const updateMutation = useMutation({
    mutationFn: (data: FormData) => vendorsApi.update(id!, { ...data, trades, zipCodes }),
    onSuccess: () => {
      toast.success('Vendor updated');
      setEditing(false);
      queryClient.invalidateQueries({ queryKey: ['vendors', id] });
    },
    onError: () => toast.error('Failed to update vendor'),
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  if (!vendor) {
    return <EmptyState title="Vendor not found" action={<Button onClick={() => navigate('/vendors')}>Back to Vendors</Button>} />;
  }

  return (
    <div>
      <button onClick={() => navigate('/vendors')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
        ← Back to Vendors
      </button>
      <PageHeader
        title={vendor.companyName}
        subtitle={vendor.user?.email}
        actions={
          !editing ? (
            <Button variant="secondary" onClick={startEditing}>
              <PencilIcon className="w-4 h-4 mr-1" /> Edit
            </Button>
          ) : (
            <Button variant="ghost" onClick={() => { setEditing(false); reset(); }}>Cancel</Button>
          )
        }
      />

      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
            {!editing ? (
              <dl className="grid grid-cols-2 gap-4">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Company</dt>
                  <dd className="mt-1 text-sm text-gray-900">{vendor.companyName}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</dt>
                  <dd className="mt-1 text-sm text-gray-900">{vendor.phone}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Trades</dt>
                  <dd className="mt-1 flex flex-wrap gap-1">
                    {vendor.trades.map(t => (
                      <span key={t} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700">{t}</span>
                    ))}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Zip Codes</dt>
                  <dd className="mt-1 flex flex-wrap gap-1">
                    {vendor.zipCodes.map(z => (
                      <span key={z} className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">{z}</span>
                    ))}
                  </dd>
                </div>
              </dl>
            ) : (
              <form onSubmit={handleSubmit(data => updateMutation.mutate(data))} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Company Name</label>
                    <input type="text" {...register('companyName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.companyName && <p className="mt-1 text-xs text-red-600">{errors.companyName.message}</p>}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                    <input type="tel" {...register('phone')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                    {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Trades</label>
                  <div className="flex flex-wrap gap-2 mb-2">
                    {trades.map(t => (
                      <span key={t} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                        {t}
                        <button type="button" onClick={() => setTrades(trades.filter(x => x !== t))}><XMarkIcon className="w-3 h-3" /></button>
                      </span>
                    ))}
                  </div>
                  <input type="text" value={tradeInput} onChange={e => setTradeInput(e.target.value)}
                    onKeyDown={e => handleTagKeyDown(e, tradeInput, setTradeInput, trades, setTrades)}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Add trade (Enter to add)" />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Zip Codes</label>
                  <div className="flex flex-wrap gap-2 mb-2">
                    {zipCodes.map(z => (
                      <span key={z} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                        {z}
                        <button type="button" onClick={() => setZipCodes(zipCodes.filter(x => x !== z))}><XMarkIcon className="w-3 h-3" /></button>
                      </span>
                    ))}
                  </div>
                  <input type="text" value={zipInput} onChange={e => setZipInput(e.target.value)}
                    onKeyDown={e => handleTagKeyDown(e, zipInput, setZipInput, zipCodes, setZipCodes)}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Add zip code (Enter to add)" />
                </div>

                <Button type="submit" loading={isSubmitting || updateMutation.isPending}>Save Changes</Button>
              </form>
            )}
          </div>

          {/* Service Requests */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Associated Service Requests</h2>
            {(requests?.items ?? []).length === 0 ? (
              <p className="text-sm text-gray-500">No service requests associated with this vendor</p>
            ) : (
              <div className="space-y-2">
                {requests?.items.slice(0, 10).map(sr => (
                  <div key={sr.id} className="flex items-center justify-between p-3 rounded-lg border border-gray-200 hover:bg-gray-50">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{sr.title}</p>
                      <p className="text-xs text-gray-500">{formatDate(sr.createdAt)}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <PriorityBadge priority={sr.priority} />
                      <StatusBadge status={sr.status} />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        <div>
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Contact Info</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Email</dt>
                <dd className="text-sm text-gray-900">{vendor.user?.email}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Phone</dt>
                <dd className="text-sm text-gray-900">{vendor.phone}</dd>
              </div>
              {vendor.rating != null && (
                <div>
                  <dt className="text-xs text-gray-500">Rating</dt>
                  <dd className="text-sm text-gray-900">{vendor.rating.toFixed(1)} / 5.0</dd>
                </div>
              )}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
