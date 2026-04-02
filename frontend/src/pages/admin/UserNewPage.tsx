import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { usersApi } from '../../api/users';
import Button from '../../components/ui/Button';
import PageHeader from '../../components/ui/PageHeader';

const schema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  email: z.string().email('Valid email required'),
  password: z.string().optional().refine(
    val => !val || val.length >= 8,
    { message: 'Password must be at least 8 characters' }
  ),
});

type FormData = z.infer<typeof schema>;

export default function UserNewPage() {
  const navigate = useNavigate();

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const createMutation = useMutation({
    mutationFn: (data: FormData) => usersApi.create({
      firstName: data.firstName,
      lastName: data.lastName,
      email: data.email,
      password: data.password || undefined,
    }),
    onSuccess: (res) => {
      toast.success('User created successfully');
      navigate(`/admin/users/${res.data.id}`);
    },
    onError: () => toast.error('Failed to create user'),
  });

  return (
    <div className="max-w-2xl">
      <PageHeader
        title="New User"
        subtitle="Add a new user to the platform"
        actions={<Button variant="secondary" onClick={() => navigate('/admin/users')}>Cancel</Button>}
      />

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <form onSubmit={handleSubmit(data => createMutation.mutate(data))} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input type="text" {...register('firstName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {errors.firstName && <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input type="text" {...register('lastName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {errors.lastName && <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
            <input type="email" {...register('email')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input type="password" {...register('password')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" placeholder="Leave blank to generate a temporary password" />
            {errors.password && <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>}
            <p className="mt-1 text-xs text-gray-500">Leave blank to generate a temporary password</p>
          </div>
          <div className="pt-2">
            <Button type="submit" loading={isSubmitting || createMutation.isPending} size="lg">
              Create User
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
