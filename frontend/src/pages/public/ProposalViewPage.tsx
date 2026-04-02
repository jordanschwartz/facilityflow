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
import { CheckCircleIcon, XCircleIcon } from '@heroicons/react/24/solid';

const rejectSchema = z.object({
  clientResponse: z.string().min(5, 'Please provide a reason for rejection'),
});
type RejectForm = z.infer<typeof rejectSchema>;

export default function ProposalViewPage() {
  const { token } = useParams<{ token: string }>();
  const queryClient = useQueryClient();
  const [rejectModalOpen, setRejectModalOpen] = useState(false);

  const { data: proposal, isLoading, error } = useQuery({
    queryKey: ['proposal-view', token],
    queryFn: () => proposalsApi.getByToken(token!).then(r => r.data),
    enabled: !!token,
    retry: false,
  });

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<RejectForm>({
    resolver: zodResolver(rejectSchema),
  });

  const respondMutation = useMutation({
    mutationFn: ({ decision, clientResponse }: { decision: string; clientResponse?: string }) =>
      proposalsApi.respond(proposal!.id, { token, decision, clientResponse }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['proposal-view', token] });
      setRejectModalOpen(false);
    },
    onError: () => toast.error('Failed to submit response. Please try again.'),
  });

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
        <div className="text-center">
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Proposal not found</h1>
          <p className="text-sm text-gray-500">This proposal link is not valid or has expired.</p>
        </div>
      </div>
    );
  }

  const isResponded = proposal.status === 'Approved' || proposal.status === 'Rejected';

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="max-w-2xl mx-auto flex items-center gap-2">
          <span className="text-gray-900 font-bold text-lg">FacilityFlow</span>
          <span className="text-gray-400">|</span>
          <span className="text-gray-500 text-sm">Service Proposal</span>
        </div>
      </div>

      <div className="max-w-2xl mx-auto py-12 px-4">
        {/* Already Responded State */}
        {isResponded && (
          <div className={`rounded-xl border p-6 mb-6 flex items-center gap-4 ${
            proposal.status === 'Approved' ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'
          }`}>
            {proposal.status === 'Approved'
              ? <CheckCircleIcon className="w-8 h-8 text-green-500 flex-shrink-0" />
              : <XCircleIcon className="w-8 h-8 text-red-500 flex-shrink-0" />
            }
            <div>
              <p className={`text-sm font-semibold ${proposal.status === 'Approved' ? 'text-green-900' : 'text-red-900'}`}>
                Proposal {proposal.status}
              </p>
              {proposal.clientResponse && (
                <p className="text-sm text-gray-600 mt-0.5">{proposal.clientResponse}</p>
              )}
              {proposal.clientRespondedAt && (
                <p className="text-xs text-gray-500 mt-1">Responded on {formatDate(proposal.clientRespondedAt)}</p>
              )}
            </div>
          </div>
        )}

        {/* Proposal Card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 mb-6">
          <h1 className="text-xl font-bold text-gray-900 mb-1">{proposal.serviceRequest?.title}</h1>
          <p className="text-sm text-gray-500 mb-6">{proposal.serviceRequest?.client?.companyName}</p>

          {/* Price */}
          <div className="bg-brand-50 rounded-xl p-6 text-center mb-6">
            <p className="text-xs font-medium text-brand-700 uppercase tracking-wider mb-1">Proposed Price</p>
            <p className="text-4xl font-bold text-brand-700">{formatCurrency(proposal.price)}</p>
          </div>

          {/* Scope */}
          <div className="mb-6">
            <h2 className="text-sm font-semibold text-gray-900 mb-2">Scope of Work</h2>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{proposal.scopeOfWork}</p>
          </div>

          {proposal.sentAt && (
            <p className="text-xs text-gray-400">Sent on {formatDate(proposal.sentAt)}</p>
          )}
        </div>

        {/* Action Buttons */}
        {!isResponded && (
          <div className="flex gap-4">
            <Button
              size="lg"
              className="flex-1 bg-green-600 hover:bg-green-700 focus:ring-green-500 text-white"
              onClick={() => respondMutation.mutate({ decision: 'Approved' })}
              loading={respondMutation.isPending}
            >
              <CheckCircleIcon className="w-5 h-5 mr-2" />
              Approve Proposal
            </Button>
            <Button
              size="lg"
              variant="danger"
              className="flex-1"
              onClick={() => setRejectModalOpen(true)}
            >
              <XCircleIcon className="w-5 h-5 mr-2" />
              Reject Proposal
            </Button>
          </div>
        )}
      </div>

      {/* Reject Modal */}
      <Modal open={rejectModalOpen} onClose={() => setRejectModalOpen(false)} title="Reject Proposal">
        <form onSubmit={handleSubmit(data => respondMutation.mutate({ decision: 'Rejected', clientResponse: data.clientResponse }))} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Reason for rejection</label>
            <textarea
              {...register('clientResponse')}
              rows={4}
              className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
              placeholder="Please explain why you're rejecting this proposal..."
            />
            {errors.clientResponse && <p className="mt-1 text-xs text-red-600">{errors.clientResponse.message}</p>}
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" type="button" onClick={() => setRejectModalOpen(false)}>Cancel</Button>
            <Button variant="danger" type="submit" loading={isSubmitting || respondMutation.isPending}>
              Reject Proposal
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
