import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { authApi } from '../../api/auth';
import { clientsApi } from '../../api/clients';
import Button from '../../components/ui/Button';
import PageHeader from '../../components/ui/PageHeader';

const schema = z.object({
  name: z.string().min(2, 'Name required'),
  email: z.string().email('Valid email required'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
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
    mutationFn: async (data: FormData) => {
      const userRes = await authApi.register({ name: data.name, email: data.email, password: data.password, role: 'Client' });
      const userId = userRes.data.user.id;
      await clientsApi.create({ userId, companyName: data.companyName, phone: data.phone, address: data.address });
    },
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
        subtitle="Register a new client account"
        actions={<Button variant="secondary" onClick={() => navigate('/clients')}>Cancel</Button>}
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
            <h3 className="text-sm font-semibold text-gray-700 mb-4">Client Profile</h3>
            <div className="space-y-4">
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
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                <input type="text" {...register('address')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" placeholder="123 Main St, City, State ZIP" />
                {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
              </div>
            </div>
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
