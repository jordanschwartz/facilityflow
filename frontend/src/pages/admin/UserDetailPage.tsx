import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { usersApi } from '../../api/users';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import StatusBadge from '../../components/ui/StatusBadge';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatDateTime } from '../../utils/formatters';
import Badge from '../../components/ui/Badge';
import { PencilIcon, TrashIcon } from '@heroicons/react/24/outline';
import { useAuthStore } from '../../stores/authStore';

const roleColors: Record<string, string> = {
  Admin: 'bg-purple-100 text-purple-700',
  Operator: 'bg-blue-100 text-blue-700',
  Client: 'bg-gray-100 text-gray-700',
  Vendor: 'bg-green-100 text-green-700',
};

const schema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  email: z.string().email('Valid email required'),
  status: z.enum(['Active', 'Inactive']),
  role: z.enum(['Admin', 'Operator', 'Client', 'Vendor']),
});
type FormData = z.infer<typeof schema>;

export default function UserDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [tempPassword, setTempPassword] = useState<string | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const currentUser = useAuthStore((s) => s.user);

  const { data: user, isLoading } = useQuery({
    queryKey: ['users', id],
    queryFn: () => usersApi.getById(id!).then(r => r.data),
    enabled: !!id,
  });

  const { register, handleSubmit, formState: { errors, isSubmitting }, reset } = useForm<FormData>({
    resolver: zodResolver(schema),
    values: user ? { firstName: user.firstName, lastName: user.lastName, email: user.email, status: user.status, role: user.role } : undefined,
  });

  const updateMutation = useMutation({
    mutationFn: (data: FormData) => usersApi.update(id!, data),
    onSuccess: () => {
      toast.success('User updated');
      setEditing(false);
      queryClient.invalidateQueries({ queryKey: ['users', id] });
    },
    onError: () => toast.error('Failed to update user'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => usersApi.delete(id!),
    onSuccess: () => {
      toast.success('User deactivated');
      navigate('/admin/users');
    },
    onError: () => toast.error('Failed to deactivate user'),
  });

  const resetPasswordMutation = useMutation({
    mutationFn: () => usersApi.resetPassword(id!),
    onSuccess: (res) => {
      setTempPassword(res.data.temporaryPassword);
    },
    onError: () => toast.error('Failed to reset password'),
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  if (!user) {
    return <EmptyState title="User not found" action={<Button onClick={() => navigate('/admin/users')}>Back to Users</Button>} />;
  }

  return (
    <div>
      <button onClick={() => navigate('/admin/users')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
        &larr; Back to Users
      </button>
      <PageHeader
        title={`${user.firstName} ${user.lastName}`}
        subtitle={user.email}
        actions={
          <div className="flex gap-2">
            {!editing ? (
              <Button variant="secondary" onClick={() => setEditing(true)}>
                <PencilIcon className="w-4 h-4 mr-1" /> Edit
              </Button>
            ) : (
              <Button variant="ghost" onClick={() => { setEditing(false); reset(); }}>Cancel</Button>
            )}
            <Button
              variant="secondary"
              onClick={() => resetPasswordMutation.mutate()}
              loading={resetPasswordMutation.isPending}
            >
              Reset Password
            </Button>
            {currentUser?.id !== id && user?.status === 'Active' && (
              <Button
                variant="danger"
                onClick={() => setShowDeleteConfirm(true)}
              >
                <TrashIcon className="w-4 h-4 mr-1" /> Deactivate
              </Button>
            )}
          </div>
        }
      />

      {/* Temporary Password Modal */}
      {tempPassword && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-xl shadow-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Password Reset</h3>
            <p className="text-sm text-gray-600 mb-4">
              A temporary password has been generated. Share this with the user securely.
            </p>
            <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 mb-4">
              <p className="text-sm font-mono text-gray-900 select-all">{tempPassword}</p>
            </div>
            <p className="text-xs text-gray-500 mb-4">
              The user will be required to change their password on next login.
            </p>
            <Button onClick={() => setTempPassword(null)} className="w-full">Done</Button>
          </div>
        </div>
      )}

      {/* Deactivate Confirmation Dialog */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-xl shadow-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Deactivate User</h3>
            <p className="text-sm text-gray-600 mb-4">
              Are you sure you want to deactivate <span className="font-medium">{user.firstName} {user.lastName}</span>? They will no longer be able to log in.
            </p>
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancel</Button>
              <Button variant="danger" onClick={() => deleteMutation.mutate()} loading={deleteMutation.isPending}>Deactivate</Button>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            {!editing ? (
              <dl className="grid grid-cols-2 gap-4">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">First Name</dt>
                  <dd className="mt-1 text-sm text-gray-900">{user.firstName}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Last Name</dt>
                  <dd className="mt-1 text-sm text-gray-900">{user.lastName}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Email</dt>
                  <dd className="mt-1 text-sm text-gray-900">{user.email}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Role</dt>
                  <dd className="mt-1"><Badge label={user.role} className={roleColors[user.role] ?? 'bg-gray-100 text-gray-700'} /></dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Status</dt>
                  <dd className="mt-1"><StatusBadge status={user.status} /></dd>
                </div>
              </dl>
            ) : (
              <form onSubmit={handleSubmit(data => updateMutation.mutate(data))} className="space-y-4">
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
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Role</label>
                    <select {...register('role')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2">
                      <option value="Admin">Admin</option>
                      <option value="Operator">Operator</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
                    <select {...register('status')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2">
                      <option value="Active">Active</option>
                      <option value="Inactive">Inactive</option>
                    </select>
                  </div>
                </div>
                <Button type="submit" loading={isSubmitting || updateMutation.isPending}>Save Changes</Button>
              </form>
            )}
          </div>
        </div>

        <div>
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">User Info</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Role</dt>
                <dd className="text-sm text-gray-900">{user.role}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Created</dt>
                <dd className="text-sm text-gray-900">{formatDate(user.createdAt)}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Last Login</dt>
                <dd className="text-sm text-gray-900">{user.lastLoginAt ? formatDateTime(user.lastLoginAt) : 'Never'}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Password Changed</dt>
                <dd className="text-sm text-gray-900">{user.passwordChangedAt ? formatDateTime(user.passwordChangedAt) : 'Never'}</dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
