import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { serviceRequestsApi } from '../../api/serviceRequests';
import { clientsApi } from '../../api/clients';
import Button from '../../components/ui/Button';
import PageHeader from '../../components/ui/PageHeader';

const schema = z.object({
  title: z.string().min(3, 'Title must be at least 3 characters'),
  description: z.string().min(10, 'Description must be at least 10 characters'),
  location: z.string().min(3, 'Location is required'),
  category: z.string().min(1, 'Service is required'),
  priority: z.string().min(1, 'Priority is required'),
  clientId: z.string().min(1, 'Client is required'),
});

type FormData = z.infer<typeof schema>;

const FALLBACK_SERVICES = ['HVAC', 'Electrical', 'Plumbing', 'Roofing', 'General', 'Other'];
const PRIORITIES = ['Low', 'Medium', 'High', 'Urgent'];

export default function RequestNewPage() {
  const navigate = useNavigate();

  const { data: servicesData } = useQuery({
    queryKey: ['services'],
    queryFn: () => serviceRequestsApi.getServices(),
    staleTime: 5 * 60 * 1000,
  });
  const services = servicesData?.data?.length ? servicesData.data : FALLBACK_SERVICES;

  const { data: clientsData } = useQuery({
    queryKey: ['clients', 'all'],
    queryFn: () => clientsApi.list({ pageSize: 100 }).then(r => r.data),
  });

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const selectedClientId = watch('clientId');
  const selectedClient = clientsData?.items.find(c => c.id === selectedClientId);

  const createMutation = useMutation({
    mutationFn: (data: FormData) => serviceRequestsApi.create(data),
    onSuccess: (res) => {
      toast.success('Work order created');
      navigate(`/work-orders/${res.data.id}`);
    },
    onError: () => toast.error('Failed to create work order'),
  });

  return (
    <div className="max-w-2xl">
      <PageHeader
        title="New Work Order"
        subtitle="Create a new work order"
        actions={
          <Button variant="secondary" onClick={() => navigate('/work-orders')}>
            Cancel
          </Button>
        }
      />

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <form onSubmit={handleSubmit(data => createMutation.mutate(data))} className="space-y-5">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Title</label>
            <input
              type="text"
              {...register('title')}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="e.g. HVAC unit repair in Building A"
            />
            {errors.title && <p className="mt-1 text-xs text-red-600">{errors.title.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              {...register('description')}
              rows={4}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Describe the issue in detail..."
            />
            {errors.description && <p className="mt-1 text-xs text-red-600">{errors.description.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Client</label>
            <select
              {...register('clientId', {
                onChange: (e) => {
                  const client = clientsData?.items.find(c => c.id === e.target.value);
                  if (client?.address) {
                    setValue('location', client.address);
                  }
                },
              })}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
            >
              <option value="">Select client...</option>
              {clientsData?.items.map(c => (
                <option key={c.id} value={c.id}>{c.companyName}</option>
              ))}
            </select>
            {errors.clientId && <p className="mt-1 text-xs text-red-600">{errors.clientId.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Location</label>
            <input
              type="text"
              {...register('location')}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="e.g. 123 Main St, Building A, Floor 2"
            />
            {selectedClient?.address && (
              <p className="mt-1 text-xs text-gray-500">Pre-filled from client address</p>
            )}
            {errors.location && <p className="mt-1 text-xs text-red-600">{errors.location.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Service</label>
              <input
                type="text"
                list="service-options"
                autoComplete="off"
                {...register('category')}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                placeholder="Select or type a service..."
              />
              <datalist id="service-options">
                {services.map(c => <option key={c} value={c} />)}
              </datalist>
              {errors.category && <p className="mt-1 text-xs text-red-600">{errors.category.message}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
              <select
                {...register('priority')}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              >
                <option value="">Select priority...</option>
                {PRIORITIES.map(p => <option key={p} value={p}>{p}</option>)}
              </select>
              {errors.priority && <p className="mt-1 text-xs text-red-600">{errors.priority.message}</p>}
            </div>
          </div>

          <div className="pt-2">
            <Button type="submit" loading={createMutation.isPending} size="lg">
              Create Work Order
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
