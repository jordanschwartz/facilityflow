import { useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { quotesApi } from '../../api/quotes';
import type { AttachmentDto } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import Button from '../../components/ui/Button';
import { CheckCircleIcon, TrashIcon, PlusIcon, PaperClipIcon, ArrowUpTrayIcon, XMarkIcon } from '@heroicons/react/24/solid';
import { formatCurrency } from '../../utils/formatters';

const schema = z.object({
  price: z.string().min(1, 'Price is required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Price must be greater than 0'),
  scopeOfWork: z.string().min(20, 'Please describe your scope of work in detail (min 20 characters)'),
  proposedStartDate: z.string().optional(),
  estimatedDurationValue: z.string().optional(),
  estimatedDurationUnit: z.string().optional(),
  vendorAvailability: z.string().optional(),
  notToExceedPrice: z.string().optional(),
  lineItems: z.array(z.object({
    description: z.string().min(1, 'Description required'),
    quantity: z.string().min(1, 'Qty required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Must be > 0'),
    unitPrice: z.string().min(1, 'Unit price required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) >= 0, 'Must be >= 0'),
  })).optional(),
  assumptions: z.string().optional(),
  exclusions: z.string().optional(),
  validUntil: z.string().optional(),
});
type FormData = z.infer<typeof schema>;

function OptionalBadge() {
  return (
    <span className="ml-2 text-xs font-medium text-gray-400 bg-gray-100 px-1.5 py-0.5 rounded">Optional</span>
  );
}

const ACCEPTED_TYPES = 'image/jpeg,image/png,image/webp,image/gif,image/heic,video/mp4,video/quicktime,video/x-msvideo,application/pdf';

function AttachmentsSection({ token }: { token: string }) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [uploading, setUploading] = useState(false);

  const handleFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setUploading(true);
    for (const file of Array.from(files)) {
      try {
        const res = await quotesApi.uploadAttachment(token, file);
        setAttachments(prev => [...prev, res.data]);
      } catch {
        toast.error(`Failed to upload ${file.name}`);
      }
    }
    setUploading(false);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleRemove = async (attachment: AttachmentDto) => {
    try {
      await quotesApi.deleteAttachment(token, attachment.id);
      setAttachments(prev => prev.filter(a => a.id !== attachment.id));
    } catch {
      toast.error('Failed to remove file');
    }
  };

  const isImage = (mime: string) => mime.startsWith('image/');
  const isVideo = (mime: string) => mime.startsWith('video/');

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
      <h2 className="text-base font-semibold text-gray-900 mb-1">
        Media & Attachments
        <OptionalBadge />
      </h2>
      <p className="text-xs text-gray-500 mb-4">Photos, videos, PDFs — up to 100 MB per file</p>

      {attachments.length > 0 && (
        <ul className="mb-4 space-y-2">
          {attachments.map(a => (
            <li key={a.id} className="flex items-center gap-3 p-2 rounded-lg bg-gray-50 border border-gray-200">
              {isImage(a.mimeType) ? (
                <img src={`http://localhost:5000${a.url}`} alt={a.filename} className="w-12 h-12 rounded object-cover flex-shrink-0" />
              ) : isVideo(a.mimeType) ? (
                <video src={`http://localhost:5000${a.url}`} className="w-12 h-12 rounded object-cover flex-shrink-0" />
              ) : (
                <div className="w-12 h-12 rounded bg-red-50 flex items-center justify-center flex-shrink-0">
                  <PaperClipIcon className="w-5 h-5 text-red-500" />
                </div>
              )}
              <span className="text-sm text-gray-700 truncate flex-1">{a.filename}</span>
              <button type="button" onClick={() => handleRemove(a)} className="p-1 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0">
                <XMarkIcon className="w-4 h-4" />
              </button>
            </li>
          ))}
        </ul>
      )}

      <input
        ref={fileInputRef}
        type="file"
        multiple
        accept={ACCEPTED_TYPES}
        className="hidden"
        onChange={e => handleFiles(e.target.files)}
      />
      <button
        type="button"
        onClick={() => fileInputRef.current?.click()}
        disabled={uploading}
        className="flex items-center gap-2 px-4 py-2 rounded-lg border border-dashed border-gray-300 text-sm text-gray-600 hover:border-brand-400 hover:text-brand-600 transition-colors w-full justify-center"
      >
        <ArrowUpTrayIcon className="w-4 h-4" />
        {uploading ? 'Uploading…' : 'Upload photos, videos, or PDFs'}
      </button>
    </div>
  );
}

export default function QuoteSubmitPage() {
  const { token } = useParams<{ token: string }>();
  const [submitted, setSubmitted] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ['quote-submit', token],
    queryFn: () => quotesApi.getByToken(token!).then(r => r.data),
    enabled: !!token,
    retry: false,
  });

  const { register, handleSubmit, control, watch, formState: { errors } } = useForm<FormData>({
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

  const submitMutation = useMutation({
    mutationFn: (formData: FormData) => {
      const payload: Parameters<typeof quotesApi.submitByToken>[1] = {
        price: parseFloat(formData.price),
        scopeOfWork: formData.scopeOfWork,
      };
      if (formData.proposedStartDate) payload.proposedStartDate = formData.proposedStartDate;
      if (formData.estimatedDurationValue) payload.estimatedDurationValue = parseFloat(formData.estimatedDurationValue);
      if (formData.estimatedDurationUnit) payload.estimatedDurationUnit = formData.estimatedDurationUnit;
      if (formData.vendorAvailability) payload.vendorAvailability = formData.vendorAvailability;
      if (formData.notToExceedPrice) payload.notToExceedPrice = parseFloat(formData.notToExceedPrice);
      if (formData.assumptions) payload.assumptions = formData.assumptions;
      if (formData.exclusions) payload.exclusions = formData.exclusions;
      if (formData.validUntil) payload.validUntil = formData.validUntil;
      if (formData.lineItems && formData.lineItems.length > 0) {
        payload.lineItems = formData.lineItems.map(li => ({
          description: li.description,
          quantity: parseFloat(li.quantity),
          unitPrice: parseFloat(li.unitPrice),
        }));
      }
      return quotesApi.submitByToken(token!, payload);
    },
    onSuccess: () => {
      setSubmitted(true);
      toast.success('Quote submitted successfully!');
    },
    onError: () => toast.error('Failed to submit quote. Please try again.'),
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Invalid or expired link</h1>
          <p className="text-sm text-gray-500">This quote submission link is not valid or has expired.</p>
        </div>
      </div>
    );
  }

  const sr = data.serviceRequest;
  const existingQuote = data.quote;

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="max-w-2xl mx-auto flex items-center gap-2">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-8 h-5 flex-shrink-0">
            <rect width="48" height="28" rx="14" fill="#E8511A"/>
            <circle cx="34" cy="14" r="10" fill="white"/>
          </svg>
          <span className="font-bold text-lg" style={{ color: '#E8511A' }}>On-Call</span>
          <span className="text-gray-400">|</span>
          <span className="text-gray-500 text-sm">Vendor Quote Submission</span>
        </div>
      </div>

      <div className="max-w-2xl mx-auto py-12 px-4">
        {submitted || (existingQuote && existingQuote.status !== 'Requested') ? (
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center">
            <CheckCircleIcon className="w-16 h-16 text-green-500 mx-auto mb-4" />
            <h2 className="text-xl font-bold text-gray-900 mb-2">Quote Submitted!</h2>
            <p className="text-gray-600 mb-4">Your quote has been submitted successfully. You'll be notified if your quote is selected.</p>
            {existingQuote && (
              <div className="bg-gray-50 rounded-lg p-4 text-left mt-4">
                <p className="text-sm text-gray-600"><span className="font-medium">Price:</span> {formatCurrency(existingQuote.price ?? 0)}</p>
                <p className="text-sm text-gray-600 mt-1"><span className="font-medium">Status:</span> {existingQuote.status}</p>
              </div>
            )}
          </div>
        ) : (
          <>
            {/* Service Request Info */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Service Request Details</h2>
              <dl className="space-y-3">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Title</dt>
                  <dd className="mt-1 text-sm font-medium text-gray-900">{sr.title}</dd>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Location</dt>
                    <dd className="mt-1 text-sm text-gray-900">{sr.location}</dd>
                  </div>
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Service</dt>
                    <dd className="mt-1 text-sm text-gray-900">{sr.category}</dd>
                  </div>
                </div>
              </dl>
            </div>

            {/* Quote Form */}
            <form onSubmit={handleSubmit(data => submitMutation.mutate(data))} className="space-y-6">

              {/* Section 1 — Required */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4">Your Quote</h2>
                <div className="space-y-5">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Your Price (USD)</label>
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
                    <label className="block text-sm font-medium text-gray-700 mb-1">Scope of Work</label>
                    <p className="text-xs text-gray-500 mb-2">Describe the work you will perform, timeline, materials, and any relevant details.</p>
                    <textarea
                      {...register('scopeOfWork')}
                      rows={6}
                      className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      placeholder="Describe the work you'll perform, estimated timeline, materials included, etc."
                    />
                    {errors.scopeOfWork && <p className="mt-1 text-xs text-red-600">{errors.scopeOfWork.message}</p>}
                  </div>
                </div>
              </div>

              {/* Section 2 — Attachments */}
              <AttachmentsSection token={token!} />

              {/* Section 3 — Scheduling */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4">
                  Scheduling
                  <OptionalBadge />
                </h2>
                <div className="space-y-5">
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
                      </select>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Earliest Availability</label>
                    <input
                      type="text"
                      {...register('vendorAvailability')}
                      className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      placeholder="e.g. Available within 24 hours"
                    />
                  </div>
                </div>
              </div>

              {/* Section 4 — Financial Details */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4">
                  Financial Details
                  <OptionalBadge />
                </h2>
                <div className="space-y-5">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Not-to-Exceed Price (USD)</label>
                    <p className="text-xs text-gray-500 mb-2">Maximum price cap</p>
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
                          <tbody className="space-y-2">
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

              {/* Section 5 — Scope Clarity */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4">
                  Scope Clarity
                  <OptionalBadge />
                </h2>
                <div className="space-y-5">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Assumptions</label>
                    <textarea
                      {...register('assumptions')}
                      rows={3}
                      className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      placeholder="e.g. Assumes access to electrical panel"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Exclusions</label>
                    <textarea
                      {...register('exclusions')}
                      rows={3}
                      className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                      placeholder="e.g. Does not include wall repair"
                    />
                  </div>
                </div>
              </div>

              {/* Section 6 — Quote Validity */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-semibold text-gray-900 mb-4">
                  Quote Validity
                  <OptionalBadge />
                </h2>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Valid Until</label>
                  <input
                    type="date"
                    {...register('validUntil')}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                  />
                </div>
              </div>

              <Button type="submit" loading={submitMutation.isPending} size="lg" className="w-full">
                Submit Quote
              </Button>
            </form>
          </>
        )}
      </div>
    </div>
  );
}
