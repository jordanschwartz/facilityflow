import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { proposalsApi } from '../../api/proposals';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import Button from '../../components/ui/Button';
import Modal from '../../components/ui/Modal';
import { formatCurrency, formatDate } from '../../utils/formatters';
import {
  CheckCircleIcon,
  XCircleIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  CalendarDaysIcon,
  ClockIcon,
  PaperClipIcon,
  DocumentTextIcon,
  ArrowDownTrayIcon,
} from '@heroicons/react/24/solid';

const rejectSchema = z.object({
  clientResponse: z.string().min(5, 'Please provide a reason'),
});
type RejectForm = z.infer<typeof rejectSchema>;

const approveSchema = z.object({
  clientResponse: z.string().optional(),
});
type ApproveForm = z.infer<typeof approveSchema>;

export default function ProposalViewPage() {
  const { token } = useParams<{ token: string }>();
  const queryClient = useQueryClient();
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [approveModalOpen, setApproveModalOpen] = useState(false);
  const [termsOpen, setTermsOpen] = useState(false);
  const [downloadingPdf, setDownloadingPdf] = useState(false);

  const { data: proposal, isLoading, error } = useQuery({
    queryKey: ['proposal-view', token],
    queryFn: () => proposalsApi.getByToken(token!).then(r => r.data),
    enabled: !!token,
    retry: false,
  });

  const rejectForm = useForm<RejectForm>({ resolver: zodResolver(rejectSchema) });
  const approveForm = useForm<ApproveForm>({ resolver: zodResolver(approveSchema) });

  const respondMutation = useMutation({
    mutationFn: ({ decision, clientResponse }: { decision: string; clientResponse?: string }) =>
      proposalsApi.respond(proposal!.id.toString(), { token, decision, clientResponse }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['proposal-view', token] });
      setRejectModalOpen(false);
      setApproveModalOpen(false);
    },
    onError: () => toast.error('Failed to submit response. Please try again.'),
  });

  const handleDownloadPdf = async () => {
    if (!token) return;
    setDownloadingPdf(true);
    try {
      await proposalsApi.downloadPublicPdf(token);
    } catch {
      toast.error('Failed to download PDF');
    } finally {
      setDownloadingPdf(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !proposal) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center max-w-md px-4">
          <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <DocumentTextIcon className="w-8 h-8 text-gray-400" />
          </div>
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Proposal not found</h1>
          <p className="text-sm text-gray-500">This proposal link is not valid or has expired.</p>
        </div>
      </div>
    );
  }

  const isResponded = proposal.status === 'Approved' || proposal.status === 'Rejected';
  const API_BASE = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-2xl mx-auto px-4 sm:px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-8 h-5 flex-shrink-0">
                <rect width="48" height="28" rx="14" fill="#E8511A" />
                <circle cx="34" cy="14" r="10" fill="white" />
              </svg>
              <span className="font-bold text-lg" style={{ color: '#E8511A' }}>On-Call</span>
              <span className="text-gray-400">|</span>
              <span className="text-gray-500 text-sm">Service Proposal</span>
            </div>
            <Button size="sm" variant="secondary" onClick={handleDownloadPdf} loading={downloadingPdf}>
              <ArrowDownTrayIcon className="w-4 h-4 mr-1" /> Download PDF
            </Button>
          </div>
        </div>
      </div>

      <div className="max-w-2xl mx-auto py-8 sm:py-12 px-4 sm:px-6">
        {/* Responded Banner */}
        {isResponded && (
          <div
            className={`rounded-xl border p-6 mb-6 ${
              proposal.status === 'Approved'
                ? 'bg-green-50 border-green-200'
                : 'bg-red-50 border-red-200'
            }`}
          >
            <div className="flex items-start gap-4">
              {proposal.status === 'Approved' ? (
                <CheckCircleIcon className="w-8 h-8 text-green-500 flex-shrink-0 mt-0.5" />
              ) : (
                <XCircleIcon className="w-8 h-8 text-red-500 flex-shrink-0 mt-0.5" />
              )}
              <div>
                <p
                  className={`text-base font-semibold ${
                    proposal.status === 'Approved' ? 'text-green-900' : 'text-red-900'
                  }`}
                >
                  {proposal.status === 'Approved'
                    ? 'Proposal Approved'
                    : 'Proposal Declined'}
                </p>
                <p className="text-sm text-gray-600 mt-1">
                  {proposal.status === 'Approved'
                    ? 'Thank you for approving this proposal. Our team will be in touch shortly to coordinate the work.'
                    : 'This proposal has been declined.'}
                </p>
                {proposal.clientResponse && (
                  <p className="text-sm text-gray-600 mt-2 italic">"{proposal.clientResponse}"</p>
                )}
                {proposal.clientRespondedAt && (
                  <p className="text-xs text-gray-500 mt-2">
                    Responded on {formatDate(proposal.clientRespondedAt)}
                  </p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Main Proposal Card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mb-6">
          {/* Title Section */}
          <div className="px-6 sm:px-8 pt-8 pb-4">
            <div className="flex items-start justify-between">
              <h1 className="text-xl font-bold text-gray-900 mb-1">
                {proposal.serviceRequest?.title}
              </h1>
              {(proposal.serviceRequest?.workOrderNumber || proposal.proposalNumber) && (
                <span className="text-sm font-medium text-gray-500 flex-shrink-0 ml-4">{proposal.serviceRequest?.workOrderNumber || proposal.proposalNumber}</span>
              )}
            </div>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-gray-500">
              {proposal.serviceRequest?.location && (
                <span>{proposal.serviceRequest.location}</span>
              )}
              {proposal.serviceRequest?.location && proposal.serviceRequest?.category && (
                <span className="text-gray-300">|</span>
              )}
              {proposal.serviceRequest?.category && (
                <span>{proposal.serviceRequest.category}</span>
              )}
            </div>
          </div>

          {/* Summary */}
          {proposal.summary && (
            <div className="mx-6 sm:mx-8 mb-6 bg-blue-50 border border-blue-100 rounded-lg p-4">
              <p className="text-sm text-blue-900 leading-relaxed">{proposal.summary}</p>
            </div>
          )}

          {/* Price */}
          <div className="mx-6 sm:mx-8 mb-6">
            <div className="bg-brand-50 rounded-xl p-6 text-center">
              <p className="text-xs font-medium text-brand-700 uppercase tracking-wider mb-1">
                {proposal.useNtePricing ? 'Not to Exceed' : 'Proposed Price'}
              </p>
              <p className="text-4xl font-bold text-brand-700">
                {formatCurrency(
                  proposal.useNtePricing && proposal.notToExceedPrice
                    ? proposal.notToExceedPrice
                    : proposal.price
                )}
              </p>
              {proposal.useNtePricing && proposal.notToExceedPrice && (
                <p className="text-xs text-brand-600 mt-1">
                  Estimated price: {formatCurrency(proposal.price)}
                </p>
              )}
            </div>
          </div>

          {/* Line Items */}
          {proposal.lineItems && proposal.lineItems.length > 0 && (
            <div className="px-6 sm:px-8 mb-6">
              <h2 className="text-sm font-semibold text-gray-900 mb-3">Line Items</h2>
              <div className="border border-gray-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
                      <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase tracking-wider">Qty</th>
                      <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase tracking-wider">Unit Price</th>
                      <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase tracking-wider">Total</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {proposal.lineItems.map((li) => (
                      <tr key={li.id}>
                        <td className="px-4 py-2 text-gray-700">{li.description}</td>
                        <td className="px-4 py-2 text-right text-gray-700">{li.quantity}</td>
                        <td className="px-4 py-2 text-right text-gray-700">{formatCurrency(li.unitPrice)}</td>
                        <td className="px-4 py-2 text-right font-medium text-gray-900">{formatCurrency(li.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-gray-50">
                    <tr>
                      <td colSpan={3} className="px-4 py-2 text-right text-sm font-semibold text-gray-700">Subtotal</td>
                      <td className="px-4 py-2 text-right text-sm font-bold text-gray-900">
                        {formatCurrency(proposal.lineItems.reduce((sum, li) => sum + li.total, 0))}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          )}

          {/* Scope of Work */}
          <div className="px-6 sm:px-8 mb-6">
            <h2 className="text-sm font-semibold text-gray-900 mb-2">Scope of Work</h2>
            <p className="text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">
              {proposal.scopeOfWork}
            </p>
          </div>

          {/* Timeline */}
          {(proposal.proposedStartDate || proposal.estimatedDuration) && (
            <div className="px-6 sm:px-8 mb-6">
              <h2 className="text-sm font-semibold text-gray-900 mb-3">Timeline</h2>
              <div className="grid grid-cols-2 gap-4">
                {proposal.proposedStartDate && (
                  <div className="flex items-start gap-3 bg-gray-50 rounded-lg p-3">
                    <CalendarDaysIcon className="w-5 h-5 text-gray-400 flex-shrink-0 mt-0.5" />
                    <div>
                      <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Start Date
                      </p>
                      <p className="text-sm font-medium text-gray-900 mt-0.5">
                        {formatDate(proposal.proposedStartDate)}
                      </p>
                    </div>
                  </div>
                )}
                {proposal.estimatedDuration && (
                  <div className="flex items-start gap-3 bg-gray-50 rounded-lg p-3">
                    <ClockIcon className="w-5 h-5 text-gray-400 flex-shrink-0 mt-0.5" />
                    <div>
                      <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Duration
                      </p>
                      <p className="text-sm font-medium text-gray-900 mt-0.5">
                        {proposal.estimatedDuration}
                      </p>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Attachments */}
          {proposal.attachments && proposal.attachments.length > 0 && (
            <div className="px-6 sm:px-8 mb-6">
              <h2 className="text-sm font-semibold text-gray-900 mb-3">Attachments</h2>
              <div className="flex flex-wrap gap-3">
                {proposal.attachments.map(a => {
                  const url = `${API_BASE}${a.filePath}`;
                  const isImage = /\.(jpg|jpeg|png|gif|webp)$/i.test(a.fileName);
                  return isImage ? (
                    <a
                      key={a.id}
                      href={url}
                      target="_blank"
                      rel="noopener noreferrer"
                      title={a.fileName}
                    >
                      <img
                        src={url}
                        alt={a.fileName}
                        className="w-24 h-24 object-cover rounded-lg border border-gray-200 hover:opacity-80 transition-opacity"
                      />
                    </a>
                  ) : (
                    <a
                      key={a.id}
                      href={url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 px-4 py-3 rounded-lg border border-gray-200 bg-gray-50 hover:bg-gray-100 transition-colors"
                    >
                      <PaperClipIcon className="w-4 h-4 text-gray-400" />
                      <span className="text-sm text-gray-700">{a.fileName}</span>
                    </a>
                  );
                })}
              </div>
            </div>
          )}

          {/* Terms & Conditions */}
          {proposal.termsAndConditions && (
            <div className="mx-6 sm:mx-8 mb-6 border border-gray-200 rounded-lg">
              <button
                type="button"
                onClick={() => setTermsOpen(!termsOpen)}
                className="w-full flex items-center justify-between px-4 py-3 text-sm font-semibold text-gray-900 hover:bg-gray-50 transition-colors rounded-lg"
              >
                Terms & Conditions
                {termsOpen ? (
                  <ChevronUpIcon className="w-4 h-4 text-gray-500" />
                ) : (
                  <ChevronDownIcon className="w-4 h-4 text-gray-500" />
                )}
              </button>
              {termsOpen && (
                <div className="px-4 pb-4 border-t border-gray-100">
                  <p className="text-sm text-gray-700 whitespace-pre-wrap pt-3 leading-relaxed">
                    {proposal.termsAndConditions}
                  </p>
                </div>
              )}
            </div>
          )}

          {/* Sent date footer */}
          {proposal.sentAt && (
            <div className="px-6 sm:px-8 pb-6">
              <p className="text-xs text-gray-400">Proposal sent on {formatDate(proposal.sentAt)}</p>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        {!isResponded && (
          <div className="space-y-3">
            <Button
              size="lg"
              className="w-full bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white"
              onClick={() => setApproveModalOpen(true)}
            >
              <CheckCircleIcon className="w-5 h-5 mr-2" />
              Approve Proposal
            </Button>
            <Button
              size="lg"
              variant="secondary"
              className="w-full"
              onClick={() => setRejectModalOpen(true)}
            >
              <XCircleIcon className="w-5 h-5 mr-2 text-gray-400" />
              Decline Proposal
            </Button>
          </div>
        )}

        {/* Footer */}
        <div className="mt-8 text-center">
          <div className="flex items-center justify-center gap-2 opacity-40">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-6 h-4">
              <rect width="48" height="28" rx="14" fill="#E8511A" />
              <circle cx="34" cy="14" r="10" fill="white" />
            </svg>
            <span className="text-xs text-gray-400">Powered by On-Call</span>
          </div>
        </div>
      </div>

      {/* Approve Modal */}
      <Modal open={approveModalOpen} onClose={() => setApproveModalOpen(false)} title="Approve Proposal">
        <form
          onSubmit={approveForm.handleSubmit(data =>
            respondMutation.mutate({ decision: 'Approved', clientResponse: data.clientResponse || undefined })
          )}
          className="space-y-4"
        >
          <p className="text-sm text-gray-600">
            You are approving a proposal for{' '}
            <strong>
              {formatCurrency(
                proposal.useNtePricing && proposal.notToExceedPrice
                  ? proposal.notToExceedPrice
                  : proposal.price
              )}
            </strong>
            . Our team will begin coordinating the work.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Comments (optional)
            </label>
            <textarea
              {...approveForm.register('clientResponse')}
              rows={3}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Any additional comments..."
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" type="button" onClick={() => setApproveModalOpen(false)}>
              Cancel
            </Button>
            <Button
              type="submit"
              className="bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white"
              loading={respondMutation.isPending}
            >
              Confirm Approval
            </Button>
          </div>
        </form>
      </Modal>

      {/* Reject Modal */}
      <Modal open={rejectModalOpen} onClose={() => setRejectModalOpen(false)} title="Decline Proposal">
        <form
          onSubmit={rejectForm.handleSubmit(data =>
            respondMutation.mutate({ decision: 'Rejected', clientResponse: data.clientResponse })
          )}
          className="space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Reason for declining
            </label>
            <textarea
              {...rejectForm.register('clientResponse')}
              rows={4}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Please explain why you're declining this proposal..."
            />
            {rejectForm.formState.errors.clientResponse && (
              <p className="mt-1 text-xs text-red-600">
                {rejectForm.formState.errors.clientResponse.message}
              </p>
            )}
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" type="button" onClick={() => setRejectModalOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="danger"
              type="submit"
              loading={respondMutation.isPending}
            >
              Decline Proposal
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
