import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { vendorsApi } from '../../api/vendors';
import Button from '../../components/ui/Button';
import PageHeader from '../../components/ui/PageHeader';
import { XMarkIcon } from '@heroicons/react/24/outline';

const schema = z.object({
  companyName: z.string().min(2, 'Company name required'),
  primaryContactName: z.string().min(2, 'Primary contact name required'),
  email: z.string().email('Valid email required'),
  phone: z.string().optional(),
  primaryZip: z.string().trim().regex(/^\d{5}$/, 'Must be a 5-digit ZIP code'),
  serviceRadiusMiles: z.coerce.number().min(1, 'Radius must be at least 1 mile').max(500, 'Radius must be at most 500 miles'),
  isActive: z.boolean(),
});

type FormData = z.infer<typeof schema>;

export default function VendorNewPage() {
  const navigate = useNavigate();
  const [trades, setTrades] = useState<string[]>([]);
  const [tradeInput, setTradeInput] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      serviceRadiusMiles: 25,
      isActive: true,
    },
  });

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

  const createMutation = useMutation({
    mutationFn: (data: FormData) =>
      vendorsApi.create({
        companyName: data.companyName,
        primaryContactName: data.primaryContactName,
        email: data.email,
        phone: data.phone || undefined,
        primaryZip: data.primaryZip,
        serviceRadiusMiles: data.serviceRadiusMiles,
        trades,
        isActive: data.isActive,
      }),
    onSuccess: () => {
      toast.success('Vendor created successfully');
      navigate('/vendors');
    },
    onError: () => toast.error('Failed to create vendor'),
  });

  return (
    <div className="max-w-2xl">
      <PageHeader
        title="New Vendor"
        subtitle="Register a new vendor partner"
        actions={<Button variant="secondary" onClick={() => navigate('/vendors')}>Cancel</Button>}
      />

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <form onSubmit={handleSubmit(data => createMutation.mutate(data))} className="space-y-5">
          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-4">Vendor Profile</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Company Name <span className="text-red-500">*</span></label>
                <input type="text" {...register('companyName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.companyName && <p className="mt-1 text-xs text-red-600">{errors.companyName.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Primary Contact Name <span className="text-red-500">*</span></label>
                <input type="text" {...register('primaryContactName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.primaryContactName && <p className="mt-1 text-xs text-red-600">{errors.primaryContactName.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Email <span className="text-red-500">*</span></label>
                <input type="email" {...register('email')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                <input type="tel" {...register('phone')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Primary ZIP Code <span className="text-red-500">*</span></label>
                <input type="text" {...register('primaryZip')} maxLength={5} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" placeholder="e.g. 90210" />
                {errors.primaryZip && <p className="mt-1 text-xs text-red-600">{errors.primaryZip.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Service Radius (miles) <span className="text-red-500">*</span></label>
                <input type="number" {...register('serviceRadiusMiles')} min={1} max={500} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.serviceRadiusMiles && <p className="mt-1 text-xs text-red-600">{errors.serviceRadiusMiles.message}</p>}
              </div>
            </div>

            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">Services</label>
              <p className="text-xs text-gray-500 mb-2">Press Enter or comma to add a service</p>
              <div className="flex flex-wrap gap-2 mb-2">
                {trades.map(t => (
                  <span key={t} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                    {t}
                    <button type="button" onClick={() => setTrades(trades.filter(x => x !== t))} className="hover:text-blue-900">
                      <XMarkIcon className="w-3 h-3" />
                    </button>
                  </span>
                ))}
              </div>
              <input
                type="text"
                value={tradeInput}
                onChange={e => setTradeInput(e.target.value)}
                onKeyDown={e => handleTagKeyDown(e, tradeInput, setTradeInput, trades, setTrades)}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                placeholder="e.g. HVAC, Electrical..."
              />
            </div>

            <div className="mt-4 flex items-center gap-3">
              <input
                type="checkbox"
                id="isActive"
                {...register('isActive')}
                className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
              />
              <label htmlFor="isActive" className="text-sm font-medium text-gray-700">Active vendor</label>
            </div>
          </div>

          <div className="pt-2">
            <Button type="submit" loading={isSubmitting || createMutation.isPending} size="lg">
              Create Vendor
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
