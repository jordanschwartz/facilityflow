import { Dialog, Transition } from '@headlessui/react';
import { Fragment, useState } from 'react';
import { XMarkIcon, ChevronDownIcon, ChevronUpIcon } from '@heroicons/react/24/outline';
import { formatCurrency, formatDate } from '../../utils/formatters';
import type { ProposalLineItem } from '../../types';

interface PreviewAttachment {
  id: string;
  fileName: string;
  filePath: string;
}

interface PreviewProposal {
  price: number;
  scopeOfWork: string;
  summary: string | null;
  useNtePricing: boolean;
  notToExceedPrice: number | null;
  proposedStartDate: string | null;
  estimatedDuration: string | null;
  termsAndConditions: string | null;
  attachments: PreviewAttachment[];
  serviceRequest: { title: string; location: string; category: string };
  proposalNumber?: string | null;
  lineItems?: ProposalLineItem[];
}

interface ProposalPreviewModalProps {
  open: boolean;
  onClose: () => void;
  proposal: PreviewProposal;
}

export default function ProposalPreviewModal({ open, onClose, proposal }: ProposalPreviewModalProps) {
  const [termsOpen, setTermsOpen] = useState(false);
  const API_BASE = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

  return (
    <Transition show={open} as={Fragment}>
      <Dialog onClose={onClose} className="relative z-50">
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-200"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-150"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black/30" />
        </Transition.Child>
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <Transition.Child
            as={Fragment}
            enter="ease-out duration-200"
            enterFrom="opacity-0 scale-95"
            enterTo="opacity-100 scale-100"
            leave="ease-in duration-150"
            leaveFrom="opacity-100 scale-100"
            leaveTo="opacity-0 scale-95"
          >
            <Dialog.Panel className="w-full max-w-2xl bg-gray-50 rounded-xl shadow-xl max-h-[90vh] overflow-hidden flex flex-col">
              {/* Header */}
              <div className="flex items-center justify-between px-6 py-4 bg-white border-b border-gray-200">
                <div>
                  <Dialog.Title className="text-lg font-semibold text-gray-900">
                    Client Preview
                  </Dialog.Title>
                  <p className="text-xs text-gray-500">This is how the proposal will appear to the client</p>
                </div>
                <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
                  <XMarkIcon className="w-5 h-5" />
                </button>
              </div>

              {/* Preview Content */}
              <div className="overflow-y-auto flex-1 p-6">
                <ProposalPreviewContent
                  proposal={proposal}
                  apiBase={API_BASE}
                  termsOpen={termsOpen}
                  onToggleTerms={() => setTermsOpen(!termsOpen)}
                />
              </div>
            </Dialog.Panel>
          </Transition.Child>
        </div>
      </Dialog>
    </Transition>
  );
}

export function ProposalPreviewContent({
  proposal,
  apiBase,
  termsOpen,
  onToggleTerms,
  showActions = false,
}: {
  proposal: PreviewProposal;
  apiBase: string;
  termsOpen: boolean;
  onToggleTerms: () => void;
  showActions?: boolean;
}) {
  return (
    <div>
      {/* Branding */}
      <div className="flex items-center gap-2 mb-6">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-8 h-5 flex-shrink-0">
          <rect width="48" height="28" rx="14" fill="#E8511A" />
          <circle cx="34" cy="14" r="10" fill="white" />
        </svg>
        <span className="font-bold text-lg" style={{ color: '#E8511A' }}>On-Call</span>
        <span className="text-gray-400">|</span>
        <span className="text-gray-500 text-sm">Service Proposal</span>
      </div>

      {/* Proposal Card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 mb-6">
        <div className="flex items-start justify-between mb-1">
          <h1 className="text-xl font-bold text-gray-900">{proposal.serviceRequest.title}</h1>
          {(proposal.serviceRequest?.workOrderNumber || proposal.proposalNumber) && (
            <span className="text-sm font-medium text-gray-500 flex-shrink-0 ml-4">{proposal.serviceRequest?.workOrderNumber || proposal.proposalNumber}</span>
          )}
        </div>
        {(proposal.serviceRequest.location || proposal.serviceRequest.category) && (
          <p className="text-sm text-gray-500 mb-6">
            {[proposal.serviceRequest.location, proposal.serviceRequest.category].filter(Boolean).join(' / ')}
          </p>
        )}

        {/* Summary */}
        {proposal.summary && (
          <div className="mb-6 bg-blue-50 border border-blue-100 rounded-lg p-4">
            <p className="text-sm text-blue-900 leading-relaxed">{proposal.summary}</p>
          </div>
        )}

        {/* Price */}
        <div className="bg-brand-50 rounded-xl p-6 text-center mb-6">
          <p className="text-xs font-medium text-brand-700 uppercase tracking-wider mb-1">
            {proposal.useNtePricing ? 'Not to Exceed' : 'Proposed Price'}
          </p>
          <p className="text-4xl font-bold text-brand-700">
            {formatCurrency(proposal.useNtePricing && proposal.notToExceedPrice ? proposal.notToExceedPrice : proposal.price)}
          </p>
          {proposal.useNtePricing && proposal.notToExceedPrice && (
            <p className="text-xs text-brand-600 mt-1">Estimated price: {formatCurrency(proposal.price)}</p>
          )}
        </div>

        {/* Line Items */}
        {proposal.lineItems && proposal.lineItems.length > 0 && (
          <div className="mb-6">
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
                  {proposal.lineItems.map((li, i) => (
                    <tr key={li.id || i}>
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

        {/* Scope */}
        <div className="mb-6">
          <h2 className="text-sm font-semibold text-gray-900 mb-2">Scope of Work</h2>
          <p className="text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">{proposal.scopeOfWork}</p>
        </div>

        {/* Timeline */}
        {(proposal.proposedStartDate || proposal.estimatedDuration) && (
          <div className="mb-6">
            <h2 className="text-sm font-semibold text-gray-900 mb-2">Timeline</h2>
            <div className="flex flex-wrap gap-x-8 gap-y-2">
              {proposal.proposedStartDate && (
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Start Date</p>
                  <p className="text-sm text-gray-700">{formatDate(proposal.proposedStartDate)}</p>
                </div>
              )}
              {proposal.estimatedDuration && (
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Duration</p>
                  <p className="text-sm text-gray-700">{proposal.estimatedDuration}</p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Attachments */}
        {proposal.attachments.length > 0 && (
          <div className="mb-6">
            <h2 className="text-sm font-semibold text-gray-900 mb-2">Attachments</h2>
            <div className="flex flex-wrap gap-2">
              {proposal.attachments.map(a => {
                const url = `${apiBase}${a.filePath}`;
                const isImage = /\.(jpg|jpeg|png|gif|webp)$/i.test(a.fileName);
                return isImage ? (
                  <a key={a.id} href={url} target="_blank" rel="noopener noreferrer" title={a.fileName}>
                    <img
                      src={url}
                      alt={a.fileName}
                      className="w-20 h-20 object-cover rounded-lg border border-gray-200 hover:opacity-80 transition-opacity"
                    />
                  </a>
                ) : (
                  <a
                    key={a.id}
                    href={url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-200 bg-gray-50 hover:bg-gray-100 transition-colors text-sm text-gray-700"
                  >
                    {a.fileName}
                  </a>
                );
              })}
            </div>
          </div>
        )}

        {/* Terms */}
        {proposal.termsAndConditions && (
          <div className="border border-gray-200 rounded-lg">
            <button
              type="button"
              onClick={onToggleTerms}
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
                <p className="text-sm text-gray-700 whitespace-pre-wrap pt-3">{proposal.termsAndConditions}</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
