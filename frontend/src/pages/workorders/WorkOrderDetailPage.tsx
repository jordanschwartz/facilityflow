import { useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { workOrdersApi } from '../../api/workOrders';
import { commentsApi } from '../../api/comments';
import type { WorkOrderStatus } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import StatusBadge from '../../components/ui/StatusBadge';
import PriorityBadge from '../../components/ui/PriorityBadge';
import Button from '../../components/ui/Button';
import EmptyState from '../../components/ui/EmptyState';
import { formatDate, formatRelativeTime } from '../../utils/formatters';
import { useAuthStore } from '../../stores/authStore';
import { CheckCircleIcon, PaperClipIcon, XMarkIcon } from '@heroicons/react/24/solid';

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

const commentSchema = z.object({ text: z.string().min(1, 'Comment cannot be empty') });
type CommentForm = z.infer<typeof commentSchema>;

export default function WorkOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const isOperator = user?.role === 'Operator';
  const isVendor = user?.role === 'Vendor';
  const [editingNotes, setEditingNotes] = useState(false);
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: wo, isLoading } = useQuery({
    queryKey: ['work-orders', id],
    queryFn: () => workOrdersApi.get(id!).then(r => r.data),
    enabled: !!id,
  });

  const { data: comments } = useQuery({
    queryKey: ['comments', { workOrderId: id }],
    queryFn: () => commentsApi.list({ workOrderId: id! }).then(r => r.data),
    enabled: !!id,
  });

  const { register: registerNotes, handleSubmit: handleNotesSubmit, formState: { isSubmitting: isSubmittingNotes } } = useForm<NotesForm>({
    values: wo ? { vendorNotes: wo.vendorNotes ?? '' } : undefined,
  });

  const { register: registerComment, handleSubmit: handleCommentSubmit, reset: resetComment, formState: { errors: commentErrors, isSubmitting: isSubmittingComment } } = useForm<CommentForm>({
    resolver: zodResolver(commentSchema),
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

  const createComment = useMutation({
    mutationFn: (data: CommentForm) =>
      commentsApi.create({ text: data.text, workOrderId: id!, files: pendingFiles.length > 0 ? pendingFiles : undefined }),
    onSuccess: () => {
      toast.success('Comment added');
      resetComment();
      setPendingFiles([]);
      queryClient.invalidateQueries({ queryKey: ['comments', { workOrderId: id }] });
    },
    onError: () => toast.error('Failed to add comment'),
  });

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;
    setPendingFiles(prev => [...prev, ...Array.from(files)]);
    e.target.value = '';
  };

  const removePendingFile = (index: number) => {
    setPendingFiles(prev => prev.filter((_, i) => i !== index));
  };

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

  const baseUrl = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

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

          {/* Comments */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-4">Activity</h2>
            <div className="space-y-4 mb-4">
              {(comments ?? []).length === 0 ? (
                <p className="text-sm text-gray-500">No comments yet</p>
              ) : (
                comments?.map(c => (
                  <div key={c.id} className="flex gap-3">
                    <div className="w-8 h-8 rounded-full bg-brand-100 flex items-center justify-center text-brand-700 text-sm font-medium flex-shrink-0">
                      {c.author?.name?.[0]?.toUpperCase()}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-gray-900">{c.author?.name}</span>
                        <span className="text-xs text-gray-400">{formatRelativeTime(c.createdAt)}</span>
                      </div>
                      <p className="text-sm text-gray-700 mt-0.5">{c.text}</p>
                      {c.attachments?.length > 0 && (
                        <div className="flex flex-wrap gap-2 mt-2">
                          {c.attachments.map(att => {
                            const isImage = att.mimeType.startsWith('image/');
                            return isImage ? (
                              <a key={att.id} href={`${baseUrl}${att.url}`} target="_blank" rel="noopener noreferrer">
                                <img
                                  src={`${baseUrl}${att.url}`}
                                  alt={att.filename}
                                  className="h-24 w-auto rounded-lg border border-gray-200 object-cover hover:opacity-90"
                                />
                              </a>
                            ) : (
                              <a
                                key={att.id}
                                href={`${baseUrl}${att.url}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 bg-gray-50 hover:bg-gray-100 text-xs text-gray-700"
                              >
                                <PaperClipIcon className="w-3.5 h-3.5 text-gray-400" />
                                <span className="truncate max-w-[120px]">{att.filename}</span>
                              </a>
                            );
                          })}
                        </div>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Comment form with file attach */}
            <form onSubmit={handleCommentSubmit(data => createComment.mutate(data))} className="space-y-2">
              <div className="flex gap-2">
                <input
                  type="text"
                  {...registerComment('text')}
                  className="flex-1 border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
                  placeholder="Add a comment..."
                />
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="p-2 text-gray-400 hover:text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50"
                  title="Attach files"
                >
                  <PaperClipIcon className="w-5 h-5" />
                </button>
                <Button type="submit" size="sm" loading={isSubmittingComment || createComment.isPending}>Post</Button>
              </div>
              <input
                ref={fileInputRef}
                type="file"
                multiple
                accept="image/*,video/*,.pdf"
                onChange={handleFileSelect}
                className="hidden"
              />
              {commentErrors.text && <p className="text-xs text-red-600">{commentErrors.text.message}</p>}

              {/* Pending file previews */}
              {pendingFiles.length > 0 && (
                <div className="flex flex-wrap gap-2 pt-1">
                  {pendingFiles.map((file, idx) => {
                    const isImage = file.type.startsWith('image/');
                    return (
                      <div key={idx} className="relative group">
                        {isImage ? (
                          <img
                            src={URL.createObjectURL(file)}
                            alt={file.name}
                            className="h-16 w-16 rounded-lg object-cover border border-gray-200"
                          />
                        ) : (
                          <div className="h-16 w-16 rounded-lg border border-gray-200 bg-gray-50 flex items-center justify-center">
                            <PaperClipIcon className="w-5 h-5 text-gray-400" />
                          </div>
                        )}
                        <button
                          type="button"
                          onClick={() => removePendingFile(idx)}
                          className="absolute -top-1.5 -right-1.5 w-5 h-5 bg-gray-700 text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                        >
                          <XMarkIcon className="w-3 h-3" />
                        </button>
                        <span className="text-[10px] text-gray-500 truncate block w-16 mt-0.5">{file.name}</span>
                      </div>
                    );
                  })}
                </div>
              )}
            </form>
          </div>
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
