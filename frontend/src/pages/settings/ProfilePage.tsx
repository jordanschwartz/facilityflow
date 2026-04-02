import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { usersApi } from '../../api/users';
import { useAuthStore } from '../../stores/authStore';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';

const profileSchema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  email: z.string().email('Valid email required'),
});
type ProfileFormData = z.infer<typeof profileSchema>;

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: z.string().min(8, 'Password must be at least 8 characters'),
  confirmPassword: z.string().min(1, 'Please confirm your password'),
}).refine(data => data.newPassword === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});
type PasswordFormData = z.infer<typeof passwordSchema>;

export default function ProfilePage() {
  const queryClient = useQueryClient();
  const { updateUser } = useAuthStore();

  const { data: profile, isLoading } = useQuery({
    queryKey: ['profile'],
    queryFn: () => usersApi.getProfile().then(r => r.data),
  });

  const profileForm = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    values: profile ? { firstName: profile.firstName, lastName: profile.lastName, email: profile.email } : undefined,
  });

  const passwordForm = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
  });

  const updateProfileMutation = useMutation({
    mutationFn: (data: ProfileFormData) => usersApi.updateProfile(data),
    onSuccess: (res) => {
      toast.success('Profile updated');
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      updateUser({
        firstName: res.data.firstName,
        lastName: res.data.lastName,
        email: res.data.email,
        name: `${res.data.firstName} ${res.data.lastName}`,
      });
    },
    onError: () => toast.error('Failed to update profile'),
  });

  const changePasswordMutation = useMutation({
    mutationFn: (data: PasswordFormData) => usersApi.changePassword(data),
    onSuccess: () => {
      toast.success('Password changed successfully');
      passwordForm.reset();
    },
    onError: () => toast.error('Failed to change password. Check your current password.'),
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  return (
    <div className="max-w-2xl">
      <PageHeader title="My Profile" subtitle="Manage your account settings" />

      {/* Personal Information */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Personal Information</h2>
        <form onSubmit={profileForm.handleSubmit(data => updateProfileMutation.mutate(data))} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input type="text" {...profileForm.register('firstName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {profileForm.formState.errors.firstName && <p className="mt-1 text-xs text-red-600">{profileForm.formState.errors.firstName.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input type="text" {...profileForm.register('lastName')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
              {profileForm.formState.errors.lastName && <p className="mt-1 text-xs text-red-600">{profileForm.formState.errors.lastName.message}</p>}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
            <input type="email" {...profileForm.register('email')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {profileForm.formState.errors.email && <p className="mt-1 text-xs text-red-600">{profileForm.formState.errors.email.message}</p>}
          </div>
          <Button type="submit" loading={profileForm.formState.isSubmitting || updateProfileMutation.isPending}>
            Save Changes
          </Button>
        </form>
      </div>

      {/* Change Password */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Change Password</h2>
        <form onSubmit={passwordForm.handleSubmit(data => changePasswordMutation.mutate(data))} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
            <input type="password" {...passwordForm.register('currentPassword')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {passwordForm.formState.errors.currentPassword && <p className="mt-1 text-xs text-red-600">{passwordForm.formState.errors.currentPassword.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
            <input type="password" {...passwordForm.register('newPassword')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {passwordForm.formState.errors.newPassword && <p className="mt-1 text-xs text-red-600">{passwordForm.formState.errors.newPassword.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
            <input type="password" {...passwordForm.register('confirmPassword')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
            {passwordForm.formState.errors.confirmPassword && <p className="mt-1 text-xs text-red-600">{passwordForm.formState.errors.confirmPassword.message}</p>}
          </div>
          <Button type="submit" loading={passwordForm.formState.isSubmitting || changePasswordMutation.isPending}>
            Change Password
          </Button>
        </form>
      </div>
    </div>
  );
}
