import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { workOrdersApi } from '../../api/workOrders';
import type { WorkOrderStatus } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate } from '../../utils/formatters';
import { useAuthStore } from '../../stores/authStore';
import { CheckCircleIcon } from '@heroicons/react/24/solid';
import ActivityTimeline from '../../components/ActivityTimeline';

const STATUS_STEPS: WorkOrderStatus[] = ['Assigned', 'InProgress', 'Completed', 'Closed'];
const STATUS_LABELS: Record<WorkOrderStatus, string> = {
  Assigned: 'Assigned',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Closed: 'Closed',
};

const notesSchema = z.object({
  vendorNotes: z.string().optional(),
});
type NotesForm = z.infer<typeof notesSchema>;

export default function WorkOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const isOperator = user?.role === 'Operator';
  const isVendor = user?.role === 'Vendor';
  const [editingNotes, setEditingNotes] = useState(false);

  const { data: wo, isLoading } = useQuery({
    queryKey: ['work-orders', id],
    queryFn: () => workOrdersApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { register: registerNotes, handleSubmit: handleNotesSubmit, formState: { isSubmitting: isSubmittingNotes } } = useForm<NotesForm>({
    values: wo ? { vendorNotes: wo.vendorNotes ?? '' } : undefined,
  });

  const updateStatus = useMutation({
    mutationFn: ({ status, vendorNotes }: { status: string; vendorNotes?: string }) =>
      workOrdersApi.updateStatus(id!, { status, vendorNotes }),
    onSuccess: () => {
      toast.success('Status updated');
      setEditingNotes(false);
      queryClient.invalidateQueries({ queryKey: ['work-orders', id] });
    },
    onError: () => toast.error('Failed to update status'),
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><LoadingSpinner /></div>;
  }

  if (!wo) {
    return <EmptyState title="Work order not found" action={<Button onClick={() => navigate('/work-orders')}>Back to Work Orders</Button>} />;
  }

  const currentStepIndex = STATUS_STEPS.indexOf(wo.status);

  const canMoveToInProgress = (isVendor || isOperator) && wo.status === 'Assigned';
  const canMoveToCompleted = (isVendor || isOperator) && wo.status === 'InProgress';
  const canClose = isOperator && wo.status === 'Completed';

  return (
    <div>
      <button onClick={() => navigate('/work-orders')} className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1">
        ← Back to Work Orders
      </button>

      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{wo.serviceRequest?.title}</h1>
          <div className="flex items-center gap-3 mt-2">
            <StatusBadge status={wo.status} />
            <PriorityBadge priority={wo.serviceRequest?.priority} />
            <span className="text-sm text-gray-500">{wo.vendor?.companyName}</span>
          </div>
        </div>
        <div className="flex gap-2">
          {canMoveToInProgress && (
            <Button onClick={() => updateStatus.mutate({ status: 'InProgress' })} loading={updateStatus.isPending}>
              Start Work
            </Button>
          )}
          {canMoveToCompleted && (
            <Button onClick={() => updateStatus.mutate({ status: 'Completed' })} loading={updateStatus.isPending}>
              Mark Completed
            </Button>
          )}
          {canClose && (
            <Button onClick={() => updateStatus.mutate({ status: 'Closed' })} loading={updateStatus.isPending} variant="secondary">
              Close Work Order
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2 space-y-6">
          {/* Status Stepper */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-6">Progress</h2>
            <div className="flex items-center">
              {STATUS_STEPS.map((step, idx) => {
                const isCompleted = idx < currentStepIndex;
                const isCurrent = idx === currentStepIndex;
                const isLast = idx === STATUS_STEPS.length - 1;
                return (
                  <div key={step} className="flex items-center flex-1">
                    <div className="flex flex-col items-center">
                      <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
                        isCompleted ? 'bg-brand-600 border-brand-600' :
                        isCurrent ? 'bg-white border-brand-600' :
                        'bg-white border-gray-300'
                      }`}>
                        {isCompleted ? (
                          <CheckCircleIcon className="w-5 h-5 text-white" />
                        ) : (
                          <span className={`text-xs font-medium ${isCurrent ? 'text-brand-600' : 'text-gray-400'}`}>{idx + 1}</span>
                        )}
                      </div>
                      <span className={`mt-2 text-xs font-medium ${isCurrent ? 'text-brand-600' : isCompleted ? 'text-gray-700' : 'text-gray-400'}`}>
                        {STATUS_LABELS[step]}
                      </span>
                    </div>
                    {!isLast && (
                      <div className={`flex-1 h-0.5 mx-2 ${idx < currentStepIndex ? 'bg-brand-600' : 'bg-gray-200'}`} />
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Vendor Notes */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-base font-semibold text-gray-900">Vendor Notes</h2>
              {isVendor && !editingNotes && (
                <Button size="sm" variant="secondary" onClick={() => setEditingNotes(true)}>Edit Notes</Button>
              )}
            </div>
            {!editingNotes ? (
              <p className="text-sm text-gray-700">{wo.vendorNotes || <span className="text-gray-400">No notes yet</span>}</p>
            ) : (
              <form onSubmit={handleNotesSubmit(data => updateStatus.mutate({ status: wo.status, vendorNotes: data.vendorNotes }))} className="space-y-3">
                <textarea
                  {...registerNotes('vendorNotes')}
                  rows={4}
                  className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                  placeholder="Add notes about the work performed..."
                />
                <div className="flex gap-2">
                  <Button type="submit" size="sm" loading={isSubmittingNotes || updateStatus.isPending}>Save Notes</Button>
                  <Button type="button" size="sm" variant="ghost" onClick={() => setEditingNotes(false)}>Cancel</Button>
                </div>
              </form>
            )}
          </div>

          {/* Activity Timeline */}
          <ActivityTimeline serviceRequestId={wo.serviceRequestId} workOrderId={wo.id} />
        </div>

        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Work Order Info</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-xs text-gray-500">Vendor</dt>
                <dd className="text-sm font-medium text-gray-900">{wo.vendor?.companyName}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Client</dt>
                <dd className="text-sm font-medium text-gray-900">{wo.serviceRequest?.client?.companyName}</dd>
              </div>
              <div>
                <dt className="text-xs text-gray-500">Status</dt>
                <dd className="mt-1"><StatusBadge status={wo.status} /></dd>
              </div>
              {wo.completedAt && (
                <div>
                  <dt className="text-xs text-gray-500">Completed</dt>
                  <dd className="text-sm text-gray-900">{formatDate(wo.completedAt)}</dd>
                </div>
              )}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
