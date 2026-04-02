import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import type { Proposal } from '../../types';
import { proposalsApi } from '../../api/proposals';
import Button from '../ui/Button';
import StatusBadge from '../ui/StatusBadge';
import { formatCurrency, formatDate } from '../../utils/formatters';
import ProposalPreviewModal from './ProposalPreviewModal';
import {
  SparklesIcon,
  ClipboardDocumentIcon,
  PencilSquareIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  ClockIcon,
  ArrowDownTrayIcon,
} from '@heroicons/react/24/solid';

interface ProposalDetailProps {
  proposal: Proposal;
  serviceRequestId: string;
  onEdit: () => void;
}

export default function ProposalDetail({ proposal, serviceRequestId, onEdit }: ProposalDetailProps) {
  const queryClient = useQueryClient();
  const [previewOpen, setPreviewOpen] = useState(false);
  const [versionsExpanded, setVersionsExpanded] = useState(false);
  const [confirmSendOpen, setConfirmSendOpen] = useState(false);
  const [downloadingPdf, setDownloadingPdf] = useState(false);

  const sendProposal = useMutation({
    mutationFn: () => proposalsApi.send(proposal.id),
    onSuccess: () => {
      toast.success('Proposal sent to client');
      setConfirmSendOpen(false);
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId, 'proposal'] });
      queryClient.invalidateQueries({ queryKey: ['service-requests', serviceRequestId] });
    },
    onError: () => toast.error('Failed to send proposal'),
  });

  const isDraft = proposal.status === 'Draft';
  const isSent = proposal.status === 'Sent' || proposal.status === 'Viewed';
  const isResponded = proposal.status === 'Approved' || proposal.status === 'Rejected';

  const copyLink = () => {
    if (proposal.publicToken) {
      navigator.clipboard.writeText(`${window.location.origin}/proposals/view/${proposal.publicToken}`);
      toast.success('Link copied to clipboard');
    }
  };

  const handleDownloadPdf = async () => {
    setDownloadingPdf(true);
    try {
      await proposalsApi.downloadPdf(proposal.id);
    } catch {
      toast.error('Failed to download PDF');
    } finally {
      setDownloadingPdf(false);
    }
  };

  const previewData = {
    price: proposal.price,
    scopeOfWork: proposal.scopeOfWork,
    summary: proposal.summary,
    useNtePricing: proposal.useNtePricing,
    notToExceedPrice: proposal.notToExceedPrice,
    proposedStartDate: proposal.proposedStartDate,
    estimatedDuration: proposal.estimatedDuration,
    termsAndConditions: proposal.termsAndConditions,
    attachments: proposal.attachments?.map(a => ({
      id: a.attachment?.id ?? a.id,
      fileName: a.attachment?.filename ?? '',
      filePath: a.attachment?.url ?? '',
    })) ?? [],
    serviceRequest: {
      title: proposal.serviceRequest?.title ?? '',
      location: '',
      category: '',
    },
    proposalNumber: proposal.proposalNumber,
    lineItems: proposal.lineItems ?? [],
  };

  return (
    <div className="space-y-6">
      {/* Header Card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <StatusBadge status={proposal.status} />
            <span className="text-xs text-gray-400">v{proposal.version}</span>
            {proposal.summaryGeneratedByAi && (
              <span className="inline-flex items-center gap-1 text-xs text-indigo-500">
                <SparklesIcon className="w-3 h-3" /> AI summary
              </span>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Button size="sm" variant="secondary" onClick={() => setPreviewOpen(true)}>
              Preview
            </Button>
            <Button size="sm" variant="secondary" onClick={handleDownloadPdf} loading={downloadingPdf}>
              <ArrowDownTrayIcon className="w-4 h-4 mr-1" /> PDF
            </Button>
            {isDraft && (
              <>
                <Button size="sm" variant="secondary" onClick={onEdit}>
                  <PencilSquareIcon className="w-4 h-4 mr-1" /> Edit
                </Button>
                <Button
                  size="sm"
                  className="bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white"
                  onClick={() => setConfirmSendOpen(true)}
                >
                  Send to Client
                </Button>
              </>
            )}
            {(isSent || isResponded) && (
              <Button size="sm" variant="secondary" onClick={onEdit}>
                <PencilSquareIcon className="w-4 h-4 mr-1" /> Edit & Resend
              </Button>
            )}
            {proposal.publicToken && (isSent || isResponded) && (
              <Button size="sm" variant="ghost" onClick={copyLink}>
                <ClipboardDocumentIcon className="w-4 h-4 mr-1" /> Copy Link
              </Button>
            )}
          </div>
        </div>

        {/* Client response */}
        {isResponded && (
          <div
            className={`rounded-lg p-4 mb-4 ${
              proposal.status === 'Approved' ? 'bg-green-50 border border-green-200' : 'bg-red-50 border border-red-200'
            }`}
          >
            <p className={`text-sm font-semibold ${proposal.status === 'Approved' ? 'text-green-900' : 'text-red-900'}`}>
              Client {proposal.status}
            </p>
            {proposal.clientResponse && (
              <p className="text-sm text-gray-600 mt-1">{proposal.clientResponse}</p>
            )}
            {proposal.clientRespondedAt && (
              <p className="text-xs text-gray-400 mt-1">Responded {formatDate(proposal.clientRespondedAt)}</p>
            )}
          </div>
        )}

        {/* Pricing Grid - Internal view with margins */}
        <div className="bg-gray-50 rounded-lg p-4 mb-4">
          <div className="grid grid-cols-4 gap-4 text-center">
            <div>
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Vendor Cost</p>
              <p className="text-lg font-bold text-gray-900">{formatCurrency(proposal.vendorCost)}</p>
              <p className="text-xs text-gray-400">{proposal.quote?.vendorName ?? 'Vendor'}</p>
            </div>
            <div>
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Margin</p>
              <p className="text-lg font-bold text-amber-600">{proposal.marginPercentage}%</p>
              <p className="text-xs text-gray-400">{formatCurrency(proposal.price - proposal.vendorCost)}</p>
            </div>
            <div>
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Client Price</p>
              <p className="text-lg font-bold text-green-700">{formatCurrency(proposal.price)}</p>
            </div>
            {proposal.useNtePricing && proposal.notToExceedPrice != null && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">NTE</p>
                <p className="text-lg font-bold text-gray-900">{formatCurrency(proposal.notToExceedPrice)}</p>
              </div>
            )}
          </div>
        </div>

        {/* Summary */}
        {proposal.summary && (
          <div className="mb-4">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Summary</p>
            <p className="text-sm text-gray-700">{proposal.summary}</p>
          </div>
        )}

        {/* Scope of Work */}
        <div className="mb-4">
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Scope of Work</p>
          <p className="text-sm text-gray-700 whitespace-pre-wrap">{proposal.scopeOfWork}</p>
        </div>

        {/* Timeline */}
        {(proposal.proposedStartDate || proposal.estimatedDuration) && (
          <div className="mb-4 flex flex-wrap gap-x-8 gap-y-2">
            {proposal.proposedStartDate && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Start Date</p>
                <p className="text-sm text-gray-700">{formatDate(proposal.proposedStartDate)}</p>
              </div>
            )}
            {proposal.estimatedDuration && (
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-0.5">Duration</p>
                <p className="text-sm text-gray-700">{proposal.estimatedDuration}</p>
              </div>
            )}
          </div>
        )}

        {/* Internal Notes */}
        {proposal.internalNotes && (
          <div className="mb-4 bg-amber-50 border border-amber-200 rounded-lg p-3">
            <p className="text-xs font-medium text-amber-700 uppercase tracking-wider mb-1">Internal Notes</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{proposal.internalNotes}</p>
          </div>
        )}

        {/* Sent info */}
        {proposal.sentAt && (
          <p className="text-xs text-gray-400">Sent on {formatDate(proposal.sentAt)}</p>
        )}

        {/* Client link */}
        {proposal.publicToken && (isSent || isResponded) && (
          <div className="mt-3">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Client Link</p>
            <div className="flex items-center gap-2">
              <code className="text-xs bg-gray-100 rounded px-2 py-1 flex-1 truncate">
                {`${window.location.origin}/proposals/view/${proposal.publicToken}`}
              </code>
              <Button size="sm" variant="secondary" onClick={copyLink}>Copy</Button>
            </div>
          </div>
        )}
      </div>

      {/* Version History */}
      {proposal.versions && proposal.versions.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
          <button
            type="button"
            onClick={() => setVersionsExpanded(!versionsExpanded)}
            className="w-full flex items-center justify-between px-6 py-4 text-left"
          >
            <div className="flex items-center gap-2">
              <ClockIcon className="w-5 h-5 text-gray-400" />
              <h3 className="text-sm font-semibold text-gray-900">Version History</h3>
              <span className="text-xs text-gray-400">{proposal.versions.length} version{proposal.versions.length !== 1 ? 's' : ''}</span>
            </div>
            {versionsExpanded ? (
              <ChevronUpIcon className="w-4 h-4 text-gray-400" />
            ) : (
              <ChevronDownIcon className="w-4 h-4 text-gray-400" />
            )}
          </button>
          {versionsExpanded && (
            <div className="px-6 pb-4 space-y-3 border-t border-gray-100 pt-3">
              {proposal.versions.map(v => (
                <div key={v.id} className="flex items-start gap-3 text-sm">
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700 flex-shrink-0">
                    v{v.versionNumber}
                  </span>
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="text-gray-900 font-medium">{formatCurrency(v.price)}</span>
                      <span className="text-gray-400">|</span>
                      <span className="text-gray-500">{v.marginPercentage}% margin</span>
                    </div>
                    {v.changeNotes && (
                      <p className="text-gray-500 mt-0.5">{v.changeNotes}</p>
                    )}
                    <p className="text-xs text-gray-400 mt-0.5">{formatDate(v.createdAt)}</p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

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
              <strong>Price:</strong> {formatCurrency(proposal.price)}
            </p>
            <div className="flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setConfirmSendOpen(false)}>
                Cancel
              </Button>
              <Button
                className="bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white"
                loading={sendProposal.isPending}
                onClick={() => sendProposal.mutate()}
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
