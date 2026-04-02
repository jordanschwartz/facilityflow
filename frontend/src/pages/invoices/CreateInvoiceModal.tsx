import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { invoicesApi } from '../../api/invoices';
import type { BillableWorkOrder } from '../../types';
import Button from '../../components/ui/Button';
import { XMarkIcon } from '@heroicons/react/24/solid';

const schema = z.object({
  amount: z.coerce.number().positive('Amount must be greater than 0'),
  description: z.string().min(1, 'Description is required'),
  billToName: z.string().min(1, 'Bill-To Name is required'),
  billToEmail: z.string().email('Must be a valid email'),
  notes: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  workOrder: BillableWorkOrder;
  onClose: () => void;
}

export default function CreateInvoiceModal({ workOrder, onClose }: Props) {
  const queryClient = useQueryClient();

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      amount: workOrder.proposalAmount ?? 0,
      description: workOrder.scopeOfWork || workOrder.title,
      billToName: workOrder.clientName,
      billToEmail: workOrder.clientEmail,
      notes: '',
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: FormData) => invoicesApi.create(workOrder.id, data),
    onSuccess: () => {
      toast.success('Invoice created');
      queryClient.invalidateQueries({ queryKey: ['invoices'] });
      queryClient.invalidateQueries({ queryKey: ['billable-work-orders'] });
      onClose();
    },
    onError: () => toast.error('Failed to create invoice'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-lg mx-4">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Create Invoice</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <XMarkIcon className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit(data => createMutation.mutate(data))} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Amount</label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm">$</span>
              <input
                type="number"
                step="0.01"
                {...register('amount')}
                className="block w-full pl-7 pr-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm"
              />
            </div>
            {errors.amount && <p className="mt-1 text-xs text-red-600">{errors.amount.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              {...register('description')}
              rows={3}
              className="block w-full border border-gray-300 rounded-lg shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2"
            />
            {errors.description && <p className="mt-1 text-xs text-red-600">{errors.description.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Bill-To Name</label>
            <input
              type="text"
              {...register('billToName')}
              className="block w-full border border-gray-300 rounded-lg shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2"
            />
            {errors.billToName && <p className="mt-1 text-xs text-red-600">{errors.billToName.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Bill-To Email</label>
            <input
              type="email"
              {...register('billToEmail')}
              className="block w-full border border-gray-300 rounded-lg shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2"
            />
            {errors.billToEmail && <p className="mt-1 text-xs text-red-600">{errors.billToEmail.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Notes (optional)</label>
            <textarea
              {...register('notes')}
              rows={2}
              className="block w-full border border-gray-300 rounded-lg shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm px-3 py-2"
              placeholder="Any additional notes..."
            />
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" loading={createMutation.isPending}>Create Invoice</Button>
          </div>
        </form>
      </div>
    </div>
  );
}
