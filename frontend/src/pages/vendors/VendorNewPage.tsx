import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { authApi } from '../../api/auth';
import { vendorsApi } from '../../api/vendors';
import Button from '../../components/ui/Button';
import PageHeader from '../../components/ui/PageHeader';
import { XMarkIcon } from '@heroicons/react/24/outline';

const schema = z.object({
  name: z.string().min(2, 'Name required'),
  email: z.string().email('Valid email required'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  companyName: z.string().min(2, 'Company name required'),
  phone: z.string().min(7, 'Phone required'),
});

type FormData = z.infer<typeof schema>;

export default function VendorNewPage() {
  const navigate = useNavigate();
  const [trades, setTrades] = useState<string[]>([]);
  const [tradeInput, setTradeInput] = useState('');
  const [zipCodes, setZipCodes] = useState<string[]>([]);
  const [zipInput, setZipInput] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
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
    mutationFn: async (data: FormData) => {
      const userRes = await authApi.register({ name: data.name, email: data.email, password: data.password, role: 'Vendor' });
      const userId = userRes.data.user.id;
      await vendorsApi.create({ userId, companyName: data.companyName, phone: data.phone, trades, zipCodes });
    },
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
          <div className="border-b border-gray-200 pb-5">
            <h3 className="text-sm font-semibold text-gray-700 mb-4">User Account</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Full Name</label>
                <input type="text" {...register('name')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <input type="email" {...register('email')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
              </div>
            </div>
            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
              <input type="password" {...register('password')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {errors.password && <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>}
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-4">Vendor Profile</h3>
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

            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">Trades</label>
              <p className="text-xs text-gray-500 mb-2">Press Enter or comma to add a trade</p>
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

            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">Zip Codes Served</label>
              <p className="text-xs text-gray-500 mb-2">Press Enter or comma to add a zip code</p>
              <div className="flex flex-wrap gap-2 mb-2">
                {zipCodes.map(z => (
                  <span key={z} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                    {z}
                    <button type="button" onClick={() => setZipCodes(zipCodes.filter(x => x !== z))} className="hover:text-gray-900">
                      <XMarkIcon className="w-3 h-3" />
                    </button>
                  </span>
                ))}
              </div>
              <input
                type="text"
                value={zipInput}
                onChange={e => setZipInput(e.target.value)}
                onKeyDown={e => handleTagKeyDown(e, zipInput, setZipInput, zipCodes, setZipCodes)}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                placeholder="e.g. 90210, 10001..."
              />
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
