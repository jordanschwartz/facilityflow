import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { clientsApi } from '../../api/clients';
import Button from '../../components/ui/Button';
import PageHeader from '../../components/ui/PageHeader';

const schema = z.object({
  contactName: z.string().min(2, 'Contact name required'),
  email: z.string().email('Valid email required'),
  companyName: z.string().min(2, 'Company name required'),
  phone: z.string().min(7, 'Phone required'),
  address: z.string().min(5, 'Address required'),
});

type FormData = z.infer<typeof schema>;

export default function ClientNewPage() {
  const navigate = useNavigate();

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const createMutation = useMutation({
    mutationFn: (data: FormData) => clientsApi.create(data),
    onSuccess: () => {
      toast.success('Client created successfully');
      navigate('/clients');
    },
    onError: () => toast.error('Failed to create client'),
  });

  return (
    <div className="max-w-2xl">
      <PageHeader
        title="New Client"
        subtitle="Add a new client"
        actions={<Button variant="secondary" onClick={() => navigate('/clients')}>Cancel</Button>}
      />

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <form onSubmit={handleSubmit(data => createMutation.mutate(data))} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company Name</label>
            <input type="text" {...register('companyName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {errors.companyName && <p className="mt-1 text-xs text-red-600">{errors.companyName.message}</p>}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Contact Name</label>
              <input type="text" {...register('contactName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {errors.contactName && <p className="mt-1 text-xs text-red-600">{errors.contactName.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input type="email" {...register('email')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
            <input type="tel" {...register('phone')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
            <input type="text" {...register('address')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" placeholder="123 Main St, City, State ZIP" />
            {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
          </div>
          <div className="pt-2">
            <Button type="submit" loading={isSubmitting || createMutation.isPending} size="lg">
              Create Client
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
