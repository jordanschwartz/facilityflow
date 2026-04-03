import { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import Modal from '../ui/Modal';
import Button from '../ui/Button';
import { quotesApi } from '../../api/quotes';
import { formatCurrency } from '../../utils/formatters';
import { TrashIcon, PlusIcon, ChevronDownIcon, ChevronUpIcon } from '@heroicons/react/24/solid';

interface ManualQuoteModalProps {
  isOpen: boolean;
  onClose: () => void;
  serviceRequestId: string;
  vendorInviteId: string;
  vendorName: string;
}

const schema = z.object({
  price: z.string().min(1, 'Price is required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Price must be greater than 0'),
  scopeOfWork: z.string().min(10, 'Scope of work must be at least 10 characters'),
  proposedStartDate: z.string().optional(),
  estimatedDurationValue: z.string().optional(),
  estimatedDurationUnit: z.string().optional(),
  notToExceedPrice: z.string().optional(),
  lineItems: z.array(z.object({
    description: z.string().min(1, 'Description required'),
    quantity: z.string().min(1, 'Qty required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Must be > 0'),
    unitPrice: z.string().min(1, 'Unit price required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) >= 0, 'Must be >= 0'),
  })).optional(),
  assumptions: z.string().optional(),
  exclusions: z.string().optional(),
  vendorAvailability: z.string().optional(),
  validUntil: z.string().optional(),
});
type FormData = z.infer<typeof schema>;

function OptionalBadge() {
  return (
    <span className="ml-2 text-xs font-medium text-gray-400 bg-gray-100 px-1.5 py-0.5 rounded">Optional</span>
  );
}

export default function ManualQuoteModal({ isOpen, onClose, serviceRequestId, vendorInviteId, vendorName }: ManualQuoteModalProps) {
  const queryClient = useQueryClient();
  const [showOptional, setShowOptional] = useState(false);

  const { register, handleSubmit, control, watch, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { lineItems: [] },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'lineItems' });
  const watchedLineItems = watch('lineItems') ?? [];
  const lineItemsTotal = watchedLineItems.reduce((sum, item) => {
    const qty = parseFloat(item.quantity) || 0;
    const up = parseFloat(item.unitPrice) || 0;
    return sum + qty * up;
  }, 0);

  const mutation = useMutation({
    mutationFn: (formData: FormData) => {
      const payload: Parameters<typeof quotesApi.manualEntry>[0] = {
        serviceRequestId,
        vendorInviteId,
        price: parseFloat(formData.price),
        scopeOfWork: formData.scopeOfWork,
      };
      if (formData.proposedStartDate) payload.proposedStartDate = formData.proposedStartDate;
      if (formData.estimatedDurationValue) payload.estimatedDurationValue = parseFloat(formData.estimatedDurationValue);
      if (formData.estimatedDurationUnit) payload.estimatedDurationUnit = formData.estimatedDurationUnit;
      if (formData.notToExceedPrice) payload.notToExceedPrice = parseFloat(formData.notToExceedPrice);
      if (formData.assumptions) payload.assumptions = formData.assumptions;
      if (formData.exclusions) payload.exclusions = formData.exclusions;
      if (formData.vendorAvailability) payload.vendorAvailability = formData.vendorAvailability;
      if (formData.validUntil) payload.validUntil = formData.validUntil;
      if (formData.lineItems && formData.lineItems.length > 0) {
        payload.lineItems = formData.lineItems.map(li => ({
          description: li.description,
          quantity: parseFloat(li.quantity),
          unitPrice: parseFloat(li.unitPrice),
        }));
      }
      return quotesApi.manualEntry(payload);
    },
    onSuccess: () => {
      toast.success(`Quote entered for ${vendorName}`);
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'invites'] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'quotes'] });
      queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
      reset();
      setShowOptional(false);
      onClose();
    },
    onError: () => toast.error('Failed to save quote. Please try again.'),
  });

  const handleClose = () => {
    reset();
    setShowOptional(false);
    onClose();
  };

  return (
    <Modal open={isOpen} onClose={handleClose} title={`Enter Quote for ${vendorName}`} size="lg">
      <form onSubmit={handleSubmit(data => mutation.mutate(data))} className="space-y-5 max-h-[70vh] overflow-y-auto pr-1">

        {/* Required Fields */}
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Price (USD) <span className="text-red-500">*</span></label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm">$</span>
              <input
                type="number"
                step="0.01"
                min="0"
                {...register('price')}
                className="block w-full pl-7 pr-3 py-2 rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border"
                placeholder="0.00"
              />
            </div>
            {errors.price && <p className="mt-1 text-xs text-red-600">{errors.price.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Scope of Work <span className="text-red-500">*</span></label>
            <textarea
              {...register('scopeOfWork')}
              rows={4}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Describe the work the vendor will perform..."
            />
            {errors.scopeOfWork && <p className="mt-1 text-xs text-red-600">{errors.scopeOfWork.message}</p>}
          </div>
        </div>

        {/* Toggle Optional Fields */}
        <button
          type="button"
          onClick={() => setShowOptional(!showOptional)}
          className="flex items-center gap-1.5 text-sm font-medium text-brand-600 hover:text-brand-700"
        >
          {showOptional ? <ChevronUpIcon className="w-4 h-4" /> : <ChevronDownIcon className="w-4 h-4" />}
          {showOptional ? 'Hide' : 'Show'} optional fields
        </button>

        {showOptional && (
          <div className="space-y-5">
            {/* Scheduling */}
            <div>
              <h3 className="text-sm font-semibold text-gray-900 mb-3">
                Scheduling
                <OptionalBadge />
              </h3>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Proposed Start Date</label>
                  <input
                    type="date"
                    {...register('proposedStartDate')}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Estimated Duration</label>
                  <div className="flex gap-3">
                    <input
                      type="number"
                      min="0"
                      step="0.5"
                      {...register('estimatedDurationValue')}
                      className="block w-32 rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      placeholder="e.g. 4"
                    />
                    <select
                      {...register('estimatedDurationUnit')}
                      className="block rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    >
                      <option value="">Unit</option>
                      <option value="Hours">Hours</option>
                      <option value="Days">Days</option>
                      <option value="Weeks">Weeks</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Vendor Availability</label>
                  <input
                    type="text"
                    {...register('vendorAvailability')}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="e.g. Available within 24 hours"
                  />
                </div>
              </div>
            </div>

            {/* Financial Details */}
            <div>
              <h3 className="text-sm font-semibold text-gray-900 mb-3">
                Financial Details
                <OptionalBadge />
              </h3>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Not-to-Exceed Price (USD)</label>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm">$</span>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...register('notToExceedPrice')}
                      className="block w-full pl-7 pr-3 py-2 rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border"
                      placeholder="0.00"
                    />
                  </div>
                </div>

                {/* Line Items */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Line Items</label>
                  {fields.length > 0 && (
                    <div className="mb-3 overflow-x-auto">
                      <table className="min-w-full text-sm">
                        <thead>
                          <tr className="text-xs font-medium text-gray-500 uppercase tracking-wider">
                            <th className="text-left pb-2 pr-3">Description</th>
                            <th className="text-left pb-2 pr-3 w-20">Qty</th>
                            <th className="text-left pb-2 pr-3 w-28">Unit Price</th>
                            <th className="text-left pb-2 pr-3 w-24">Total</th>
                            <th className="pb-2 w-8"></th>
                          </tr>
                        </thead>
                        <tbody>
                          {fields.map((field, index) => {
                            const qty = parseFloat(watchedLineItems[index]?.quantity) || 0;
                            const up = parseFloat(watchedLineItems[index]?.unitPrice) || 0;
                            return (
                              <tr key={field.id} className="align-top">
                                <td className="pr-3 pb-2">
                                  <input
                                    type="text"
                                    {...register(`lineItems.${index}.description`)}
                                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                                    placeholder="Description"
                                  />
                                  {errors.lineItems?.[index]?.description && (
                                    <p className="mt-0.5 text-xs text-red-600">{errors.lineItems[index]?.description?.message}</p>
                                  )}
                                </td>
                                <td className="pr-3 pb-2">
                                  <input
                                    type="number"
                                    min="0"
                                    step="1"
                                    {...register(`lineItems.${index}.quantity`)}
                                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                                    placeholder="1"
                                  />
                                  {errors.lineItems?.[index]?.quantity && (
                                    <p className="mt-0.5 text-xs text-red-600">{errors.lineItems[index]?.quantity?.message}</p>
                                  )}
                                </td>
                                <td className="pr-3 pb-2">
                                  <div className="relative">
                                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm">$</span>
                                    <input
                                      type="number"
                                      step="0.01"
                                      min="0"
                                      {...register(`lineItems.${index}.unitPrice`)}
                                      className="block w-full pl-7 pr-3 py-2 rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border"
                                      placeholder="0.00"
                                    />
                                  </div>
                                  {errors.lineItems?.[index]?.unitPrice && (
                                    <p className="mt-0.5 text-xs text-red-600">{errors.lineItems[index]?.unitPrice?.message}</p>
                                  )}
                                </td>
                                <td className="pr-3 pb-2">
                                  <div className="py-2 text-sm text-gray-900 font-medium">{formatCurrency(qty * up)}</div>
                                </td>
                                <td className="pb-2">
                                  <button
                                    type="button"
                                    onClick={() => remove(index)}
                                    className="p-2 text-gray-400 hover:text-red-500 transition-colors"
                                  >
                                    <TrashIcon className="w-4 h-4" />
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                      {fields.length > 0 && (
                        <div className="flex justify-end pr-10 pt-1 border-t border-gray-100">
                          <span className="text-sm font-medium text-gray-700">Total: {formatCurrency(lineItemsTotal)}</span>
                        </div>
                      )}
                    </div>
                  )}
                  <button
                    type="button"
                    onClick={() => append({ description: '', quantity: '', unitPrice: '' })}
                    className="flex items-center gap-1.5 text-sm text-brand-600 hover:text-brand-700 font-medium"
                  >
                    <PlusIcon className="w-4 h-4" />
                    Add line item
                  </button>
                </div>
              </div>
            </div>

            {/* Scope Clarity */}
            <div>
              <h3 className="text-sm font-semibold text-gray-900 mb-3">
                Scope Clarity
                <OptionalBadge />
              </h3>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Assumptions</label>
                  <textarea
                    {...register('assumptions')}
                    rows={2}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="e.g. Assumes access to electrical panel"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Exclusions</label>
                  <textarea
                    {...register('exclusions')}
                    rows={2}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="e.g. Does not include wall repair"
                  />
                </div>
              </div>
            </div>

            {/* Quote Validity */}
            <div>
              <h3 className="text-sm font-semibold text-gray-900 mb-3">
                Quote Validity
                <OptionalBadge />
              </h3>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Valid Until</label>
                <input
                  type="date"
                  {...register('validUntil')}
                  className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                />
              </div>
            </div>
          </div>
        )}

        {/* Footer */}
        <div className="flex justify-end gap-3 pt-2 border-t border-gray-200">
          <Button type="button" variant="secondary" onClick={handleClose}>Cancel</Button>
          <Button type="submit" loading={mutation.isPending}>Save Quote</Button>
        </div>
      </form>
    </Modal>
  );
}
