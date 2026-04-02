import { useState, useEffect, useMemo } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { proposalsApi } from '../../api/proposals';
import type { Quote, Proposal, AttachmentDto } from '../../types';
import Button from '../ui/Button';
import { formatCurrency } from '../../utils/formatters';
import ProposalPreviewModal from './ProposalPreviewModal';
import {
  SparklesIcon,
  XMarkIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  PaperClipIcon,
  DocumentTextIcon,
  CalendarDaysIcon,
  CurrencyDollarIcon,
  ChatBubbleBottomCenterTextIcon,
  PhotoIcon,
  ExclamationTriangleIcon,
  ArrowDownTrayIcon,
} from '@heroicons/react/24/solid';
import { ClockIcon } from '@heroicons/react/24/outline';

const DEFAULT_TERMS = `1. Payment is due within 30 days of invoice date.
2. This proposal is valid for 30 days from the date of issue.
3. Any changes to the scope of work may result in additional charges.
4. Work will be performed during normal business hours unless otherwise agreed.
5. Client is responsible for providing access to the work area.`;

const proposalSchema = z.object({
  marginPercentage: z.number().min(0, 'Margin must be 0 or greater').max(100, 'Margin cannot exceed 100%'),
  scopeOfWork: z.string().min(10, 'Scope of work must be at least 10 characters'),
  summary: z.string().optional(),
  useNtePricing: z.boolean(),
  notToExceedPrice: z.number().optional().nullable(),
  proposedStartDate: z.string().optional(),
  estimatedDuration: z.string().optional(),
  termsAndConditions: z.string().optional(),
  internalNotes: z.string().optional(),
  attachmentIds: z.array(z.string()),
});

type ProposalFormData = z.infer<typeof proposalSchema>;

interface ProposalBuilderProps {
  serviceRequestId: string;
  quote: Quote;
  existingProposal?: Proposal | null;
  allQuotes: Quote[];
  onSuccess: () => void;
}

export default function ProposalBuilder({
  serviceRequestId,
  quote,
  existingProposal,
  allQuotes,
  onSuccess,
}: ProposalBuilderProps) {
  const queryClient = useQueryClient();
  const [previewOpen, setPreviewOpen] = useState(false);
  const [termsExpanded, setTermsExpanded] = useState(false);
  const [generatingSummary, setGeneratingSummary] = useState(false);
  const [confirmSendOpen, setConfirmSendOpen] = useState(false);
  const [downloadingPdf, setDownloadingPdf] = useState(false);

  const isEditing = !!existingProposal;
  const vendorCost = quote.price;

  const {
    register,
    handleSubmit,
    control,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ProposalFormData>({
    resolver: zodResolver(proposalSchema),
    defaultValues: {
      marginPercentage: existingProposal?.marginPercentage ?? 25,
      scopeOfWork: existingProposal?.scopeOfWork ?? quote.scopeOfWork ?? '',
      summary: existingProposal?.summary ?? '',
      useNtePricing: existingProposal?.useNtePricing ?? false,
      notToExceedPrice: existingProposal?.notToExceedPrice ?? null,
      proposedStartDate: existingProposal?.proposedStartDate ?? quote.proposedStartDate ?? '',
      estimatedDuration:
        existingProposal?.estimatedDuration ??
        (quote.estimatedDurationValue && quote.estimatedDurationUnit
          ? `${quote.estimatedDurationValue} ${quote.estimatedDurationUnit}`
          : ''),
      termsAndConditions: existingProposal?.termsAndConditions ?? DEFAULT_TERMS,
      internalNotes: existingProposal?.internalNotes ?? '',
      attachmentIds: existingProposal?.attachments?.map(a => a.attachmentId) ?? quote.attachments?.map(a => a.id) ?? [],
    },
  });

  const marginPercentage = watch('marginPercentage');
  const useNtePricing = watch('useNtePricing');
  const selectedAttachmentIds = watch('attachmentIds');
  const scopeOfWork = watch('scopeOfWork');
  const summary = watch('summary');

  const clientPrice = useMemo(
    () => vendorCost * (1 + (marginPercentage || 0) / 100),
    [vendorCost, marginPercentage]
  );

  const marginAmount = useMemo(() => clientPrice - vendorCost, [clientPrice, vendorCost]);

  // All attachments from the quote
  const quoteAttachments = quote.attachments ?? [];

  const createProposal = useMutation({
    mutationFn: (data: ProposalFormData) =>
      proposalsApi.create(serviceRequestId, {
        quoteId: quote.id,
        marginPercentage: data.marginPercentage,
        scopeOfWork: data.scopeOfWork,
        summary: data.summary || undefined,
        useNtePricing: data.useNtePricing,
        notToExceedPrice: data.useNtePricing && data.notToExceedPrice ? data.notToExceedPrice : undefined,
        proposedStartDate: data.proposedStartDate || undefined,
        estimatedDuration: data.estimatedDuration || undefined,
        termsAndConditions: data.termsAndConditions || undefined,
        internalNotes: data.internalNotes || undefined,
        attachmentIds: data.attachmentIds,
      }),
    onSuccess: () => {
      toast.success('Proposal saved as draft');
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'proposal'] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId] });
      onSuccess();
    },
    onError: () => toast.error('Failed to create proposal'),
  });

  const updateProposal = useMutation({
    mutationFn: (data: ProposalFormData) =>
      proposalsApi.update(existingProposal!.id, {
        marginPercentage: data.marginPercentage,
        scopeOfWork: data.scopeOfWork,
        summary: data.summary || undefined,
        useNtePricing: data.useNtePricing,
        notToExceedPrice: data.useNtePricing && data.notToExceedPrice ? data.notToExceedPrice : undefined,
        proposedStartDate: data.proposedStartDate || undefined,
        estimatedDuration: data.estimatedDuration || undefined,
        termsAndConditions: data.termsAndConditions || undefined,
        internalNotes: data.internalNotes || undefined,
        attachmentIds: data.attachmentIds,
      }),
    onSuccess: () => {
      toast.success('Proposal updated');
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'proposal'] });
      onSuccess();
    },
    onError: () => toast.error('Failed to update proposal'),
  });

  const sendProposalMutation = useMutation({
    mutationFn: (proposalId: string) => proposalsApi.send(proposalId),
    onSuccess: () => {
      toast.success('Proposal sent to client');
      setConfirmSendOpen(false);
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'proposal'] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId] });
      onSuccess();
    },
    onError: () => toast.error('Failed to send proposal'),
  });

  const handleGenerateSummary = async () => {
    if (!existingProposal) {
      toast.error('Save proposal as draft first to generate a summary');
      return;
    }
    setGeneratingSummary(true);
    try {
      const res = await proposalsApi.generateSummary(existingProposal.id, {
        scopeOfWork,
        notes: watch('internalNotes') || undefined,
        jobDescription: existingProposal.serviceRequest?.title ?? '',
      });
      setValue('summary', res.data.summary);
      toast.success('Summary generated');
    } catch {
      toast.error('Failed to generate summary');
    } finally {
      setGeneratingSummary(false);
    }
  };

  const handleDownloadPdf = async () => {
    if (!existingProposal) return;
    setDownloadingPdf(true);
    try {
      await proposalsApi.downloadPdf(existingProposal.id);
    } catch {
      toast.error('Failed to download PDF');
    } finally {
      setDownloadingPdf(false);
    }
  };

  const onSubmit = (data: ProposalFormData) => {
    if (isEditing) {
      updateProposal.mutate(data);
    } else {
      createProposal.mutate(data);
    }
  };

  const handleSendClick = () => {
    if (existingProposal) {
      setConfirmSendOpen(true);
    }
  };

  const toggleAttachment = (attachmentId: string) => {
    const current = selectedAttachmentIds ?? [];
    if (current.includes(attachmentId)) {
      setValue('attachmentIds', current.filter(id => id !== attachmentId));
    } else {
      setValue('attachmentIds', [...current, attachmentId]);
    }
  };

  const API_BASE = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

  const previewData = {
    price: clientPrice,
    scopeOfWork,
    summary: summary ?? null,
    useNtePricing,
    notToExceedPrice: watch('notToExceedPrice') ?? null,
    proposedStartDate: watch('proposedStartDate') ?? null,
    estimatedDuration: watch('estimatedDuration') ?? null,
    termsAndConditions: watch('termsAndConditions') ?? null,
    attachments: quoteAttachments
      .filter(a => (selectedAttachmentIds ?? []).includes(a.id))
      .map(a => ({ id: a.id, fileName: a.filename, filePath: a.url })),
    serviceRequest: existingProposal?.serviceRequest
      ? { title: existingProposal.serviceRequest.title, location: '', category: '' }
      : { title: 'Service Request', location: '', category: '' },
  };

  return (
    <div className="space-y-6">
      <form onSubmit={handleSubmit(onSubmit)}>
        {/* Section 1: Pricing & Margin */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          <div className="flex items-center gap-2 mb-4">
            <CurrencyDollarIcon className="w-5 h-5 text-brand-600" />
            <h3 className="text-base font-semibold text-gray-900">Pricing & Margin</h3>
          </div>

          {/* Pricing Breakdown */}
          <div className="bg-gray-50 rounded-lg p-4 mb-5">
            <div className="grid grid-cols-3 gap-4 text-center">
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Vendor Cost</p>
                <p className="text-lg font-bold text-gray-900">{formatCurrency(vendorCost)}</p>
                <p className="text-xs text-gray-400">{quote.vendor?.companyName}</p>
              </div>
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">+ Margin</p>
                <p className="text-lg font-bold text-amber-600">{marginPercentage}%</p>
                <p className="text-xs text-gray-400">{formatCurrency(marginAmount)}</p>
              </div>
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Client Price</p>
                <p className="text-lg font-bold text-green-700">{formatCurrency(clientPrice)}</p>
              </div>
            </div>
          </div>

          {/* Margin Input */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Margin Percentage</label>
            <div className="flex items-center gap-4">
              <Controller
                name="marginPercentage"
                control={control}
                render={({ field }) => (
                  <>
                    <input
                      type="range"
                      min="0"
                      max="50"
                      step="1"
                      value={field.value}
                      onChange={e => field.onChange(parseFloat(e.target.value))}
                      className="flex-1 h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-brand-600"
                    />
                    <div className="relative w-24">
                      <input
                        type="number"
                        min="0"
                        max="100"
                        step="0.5"
                        value={field.value}
                        onChange={e => field.onChange(parseFloat(e.target.value) || 0)}
                        className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2 pr-8"
                      />
                      <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm">%</span>
                    </div>
                  </>
                )}
              />
            </div>
            {errors.marginPercentage && (
              <p className="mt-1 text-xs text-red-600">{errors.marginPercentage.message}</p>
            )}
          </div>

          {/* NTE Toggle */}
          <div className="border-t border-gray-100 pt-4">
            <label className="flex items-center gap-3 cursor-pointer">
              <Controller
                name="useNtePricing"
                control={control}
                render={({ field }) => (
                  <input
                    type="checkbox"
                    checked={field.value}
                    onChange={field.onChange}
                    className="rounded border-gray-300 text-brand-600 focus:ring-brand-500 h-4 w-4"
                  />
                )}
              />
              <div>
                <span className="text-sm font-medium text-gray-700">Not-to-Exceed Pricing</span>
                <p className="text-xs text-gray-500">Set a maximum price cap for the client</p>
              </div>
            </label>
            {useNtePricing && (
              <div className="mt-3 ml-7">
                <label className="block text-sm font-medium text-gray-700 mb-1">NTE Amount</label>
                <div className="relative w-48">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm">$</span>
                  <Controller
                    name="notToExceedPrice"
                    control={control}
                    render={({ field }) => (
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        value={field.value ?? ''}
                        onChange={e => field.onChange(e.target.value ? parseFloat(e.target.value) : null)}
                        className="block w-full pl-7 pr-3 py-2 rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border"
                        placeholder="0.00"
                      />
                    )}
                  />
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Section 2: Scope & Summary */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          <div className="flex items-center gap-2 mb-4">
            <DocumentTextIcon className="w-5 h-5 text-brand-600" />
            <h3 className="text-base font-semibold text-gray-900">Scope & Summary</h3>
          </div>

          <div className="mb-5">
            <label className="block text-sm font-medium text-gray-700 mb-1">Scope of Work</label>
            <textarea
              {...register('scopeOfWork')}
              rows={6}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Describe the work to be performed..."
            />
            {errors.scopeOfWork && <p className="mt-1 text-xs text-red-600">{errors.scopeOfWork.message}</p>}
          </div>

          {/* AI Summary */}
          <div className="mb-5">
            <div className="flex items-center justify-between mb-1">
              <label className="block text-sm font-medium text-gray-700">Summary</label>
              <div className="flex items-center gap-2">
                {summary && (
                  <button
                    type="button"
                    onClick={() => setValue('summary', '')}
                    className="text-xs text-gray-500 hover:text-gray-700 flex items-center gap-1"
                  >
                    <XMarkIcon className="w-3 h-3" /> Clear
                  </button>
                )}
                <button
                  type="button"
                  onClick={handleGenerateSummary}
                  disabled={generatingSummary}
                  className="inline-flex items-center gap-1.5 text-xs font-medium text-brand-600 hover:text-brand-700 disabled:opacity-50"
                >
                  <SparklesIcon className="w-3.5 h-3.5" />
                  {generatingSummary ? 'Generating...' : 'Generate with AI'}
                </button>
              </div>
            </div>
            <p className="text-xs text-gray-500 mb-2">A concise summary shown prominently to the client. You can write one or generate it with AI.</p>
            <textarea
              {...register('summary')}
              rows={3}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Brief summary of the proposal for the client..."
            />
            {existingProposal?.summaryGeneratedByAi && summary && (
              <p className="mt-1 text-xs text-indigo-500 flex items-center gap-1">
                <SparklesIcon className="w-3 h-3" /> AI-generated summary
              </p>
            )}
          </div>

          {/* Internal Notes */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Internal Notes</label>
            <p className="text-xs text-amber-600 mb-2 flex items-center gap-1">
              <ExclamationTriangleIcon className="w-3 h-3" />
              Internal only — not visible to the client
            </p>
            <textarea
              {...register('internalNotes')}
              rows={3}
              className="block w-full rounded-lg border-amber-200 bg-amber-50/50 shadow-sm focus:ring-amber-500 focus:border-amber-500 sm:text-sm border px-3 py-2"
              placeholder="Notes for internal team..."
            />
          </div>
        </div>

        {/* Section 3: Attachments */}
        {quoteAttachments.length > 0 && (
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
            <div className="flex items-center gap-2 mb-4">
              <PaperClipIcon className="w-5 h-5 text-brand-600" />
              <h3 className="text-base font-semibold text-gray-900">Attachments</h3>
              <span className="text-xs text-gray-400 ml-1">
                {(selectedAttachmentIds ?? []).length} of {quoteAttachments.length} selected
              </span>
            </div>
            <p className="text-xs text-gray-500 mb-3">Select which quote attachments to include in the proposal.</p>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
              {quoteAttachments.map(att => {
                const isSelected = (selectedAttachmentIds ?? []).includes(att.id);
                const isImage = att.mimeType?.startsWith('image/');
                return (
                  <label
                    key={att.id}
                    className={`flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                      isSelected
                        ? 'border-brand-300 bg-brand-50'
                        : 'border-gray-200 hover:bg-gray-50'
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => toggleAttachment(att.id)}
                      className="rounded border-gray-300 text-brand-600 focus:ring-brand-500 h-4 w-4 flex-shrink-0"
                    />
                    {isImage ? (
                      <img
                        src={`${API_BASE}${att.url}`}
                        alt={att.filename}
                        className="w-10 h-10 rounded object-cover flex-shrink-0"
                      />
                    ) : (
                      <div className="w-10 h-10 rounded bg-gray-100 flex items-center justify-center flex-shrink-0">
                        <PhotoIcon className="w-5 h-5 text-gray-400" />
                      </div>
                    )}
                    <span className="text-sm text-gray-700 truncate">{att.filename}</span>
                  </label>
                );
              })}
            </div>
          </div>
        )}

        {/* Section 4: Timeline */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          <div className="flex items-center gap-2 mb-4">
            <CalendarDaysIcon className="w-5 h-5 text-brand-600" />
            <h3 className="text-base font-semibold text-gray-900">Timeline</h3>
          </div>
          <div className="grid grid-cols-2 gap-4">
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
              <input
                type="text"
                {...register('estimatedDuration')}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                placeholder="e.g., 2-3 business days"
              />
            </div>
          </div>
        </div>

        {/* Section 5: Terms & Conditions */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm mb-6">
          <button
            type="button"
            onClick={() => setTermsExpanded(!termsExpanded)}
            className="w-full flex items-center justify-between px-6 py-4 text-left"
          >
            <div className="flex items-center gap-2">
              <ChatBubbleBottomCenterTextIcon className="w-5 h-5 text-brand-600" />
              <h3 className="text-base font-semibold text-gray-900">Terms & Conditions</h3>
            </div>
            {termsExpanded ? (
              <ChevronUpIcon className="w-4 h-4 text-gray-400" />
            ) : (
              <ChevronDownIcon className="w-4 h-4 text-gray-400" />
            )}
          </button>
          {termsExpanded && (
            <div className="px-6 pb-6">
              <textarea
                {...register('termsAndConditions')}
                rows={8}
                className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              />
            </div>
          )}
        </div>

        {/* Actions */}
        <div className="flex items-center gap-3">
          <Button type="submit" loading={createProposal.isPending || updateProposal.isPending}>
            {isEditing ? 'Update Draft' : 'Save Draft'}
          </Button>
          <Button type="button" variant="secondary" onClick={() => setPreviewOpen(true)}>
            Preview
          </Button>
          {isEditing && (
            <Button type="button" variant="secondary" onClick={handleDownloadPdf} loading={downloadingPdf}>
              <ArrowDownTrayIcon className="w-4 h-4 mr-1" /> Download PDF
            </Button>
          )}
          {isEditing && (
            <Button
              type="button"
              className="bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white ml-auto"
              onClick={handleSendClick}
            >
              {existingProposal?.status === 'Draft' ? 'Send to Client' : 'Update & Resend'}
            </Button>
          )}
        </div>
      </form>

      {/* Preview Modal */}
      <ProposalPreviewModal
        open={previewOpen}
        onClose={() => setPreviewOpen(false)}
        proposal={previewData}
      />

      {/* Confirm Send Dialog */}
      {confirmSendOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/30" onClick={() => setConfirmSendOpen(false)} />
          <div className="relative bg-white rounded-xl shadow-xl p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Send Proposal to Client?</h3>
            <p className="text-sm text-gray-600 mb-1">
              The client will receive a link to view and respond to this proposal.
            </p>
            <p className="text-sm text-gray-600 mb-4">
              <strong>Price:</strong> {formatCurrency(clientPrice)}
            </p>
            <div className="flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setConfirmSendOpen(false)}>
                Cancel
              </Button>
              <Button
                className="bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white"
                loading={sendProposalMutation.isPending}
                onClick={() => sendProposalMutation.mutate(existingProposal!.id)}
              >
                Send Proposal
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
