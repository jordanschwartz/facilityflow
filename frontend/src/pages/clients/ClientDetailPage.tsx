import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { clientsApi } from '../../api/clients';
import { serviceRequestsApi } from '../../api/serviceRequests';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PageHeader from '../../components/ui/PageHeader';
import Button from '../../components/ui/Button';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate } from '../../utils/formatters';
import { PencilIcon } from '@heroicons/react/24/outline';

const schema = z.object({
  companyName: z.string().min(2, 'Company name required'),
  phone: z.string().min(7, 'Phone required'),
  address: z.string().min(5, 'Address required'),
});
type FormData = z.infer<typeof schema>;

export default function ClientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);

  const { data: client, isLoading } = useQuery({
    queryKey: ['clients', id],
    queryFn: () => clientsApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: requests } = useQuery({
    queryKey: ['service-requests', { clientId: id }],
    queryFn: () => serviceRequestsApi.list({ clientId: id!, pageSize: 50 }).then(r => r.data),
    enabled: !!id,
  });

  const { register, handleSubmit, formState: { errors, isSubmitting }, reset } = useForm<FormData>({
    resolver: zodResolver(schema),
    values: client ? { companyName: client.companyName, phone: client.phone, address: client.address } : undefined,
  });

  const updateMutation = useMutation({
    mutationFn: (data: FormData) => clientsApi.update(id!, data),
    onSuccess: () => {
      toast.success('Client updated');
      setEditing(false);
      queryClient.invalidateQueries({ queryKey: ['clients', id] });
    },
    onError: () => toast.error('Failed to update client'),
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  if (!client) {
    return <EmptyState title="Client not found" action={<Button onClick={() => navigate('/clients')}>Back to Clients</Button>} />;
  }

  return (
    <div>
      <button onClick={() => navigate('/clients')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
        ← Back to Clients
      </button>
      <PageHeader
        title={client.companyName}
        subtitle={client.user?.email}
        actions={
          !editing ? (
            <Button variant="secondary" onClick={() => setEditing(true)}>
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
                  <dd className="mt-1 text-sm text-gray-900">{client.companyName}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</dt>
                  <dd className="mt-1 text-sm text-gray-900">{client.phone}</dd>
                </div>
                <div className="col-span-2">
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Address</dt>
                  <dd className="mt-1 text-sm text-gray-900">{client.address}</dd>
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
                  <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                  <input type="text" {...register('address')} className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2" />
                  {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
                </div>
                <Button type="submit" loading={isSubmitting || updateMutation.isPending}>Save Changes</Button>
              </form>
            )}
          </div>

          {/* Service Requests */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-base font-semibold text-gray-900">Service Requests</h2>
              <Link to={`/requests/new`} className="text-sm text-brand-600 hover:text-brand-700 font-medium">+ New Request</Link>
            </div>
            {(requests?.items ?? []).length === 0 ? (
              <p className="text-sm text-gray-500">No service requests for this client yet</p>
            ) : (
              <div className="space-y-2">
                {requests?.items.map(sr => (
                  <Link key={sr.id} to={`/requests/${sr.id}`} className="flex items-center justify-between p-3 rounded-lg border border-gray-200 hover:bg-gray-50 hover:border-brand-200 transition-colors">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{sr.title}</p>
                      <p className="text-xs text-gray-500">{formatDate(sr.createdAt)}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <PriorityBadge priority={sr.priority} />
                      <StatusBadge status={sr.status} />
                    </div>
                  </Link>
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
                <dd className="text-sm text-gray-900">{client.user?.email}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Phone</dt>
                <dd className="text-sm text-gray-900">{client.phone}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Total Requests</dt>
                <dd className="text-sm text-gray-900">{requests?.totalCount ?? 0}</dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
