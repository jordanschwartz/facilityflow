import { useState, useRef, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { activityLogsApi } from '../api/activityLogs';
import { commentsApi } from '../api/comments';
import { inboundEmailsApi } from '../api/inboundEmails';
import type { ActivityLog, ActivityLogCategory, Comment, InboundEmail, InboundEmailDetail } from '../types';
import Button from './ui/Button';
import { formatDateTime } from '../utils/formatters';
import { PaperClipIcon, XMarkIcon } from '@heroicons/react/24/solid';
import {
  ArrowPathIcon,
  ChatBubbleLeftIcon,
  DocumentArrowUpIcon,
  CurrencyDollarIcon,
  CogIcon,
  PencilSquareIcon,
  EnvelopeIcon,
  EnvelopeOpenIcon,
  PaperAirplaneIcon,
  ArrowDownTrayIcon,
} from '@heroicons/react/24/outline';

const commentSchema = z.object({ text: z.string().min(1, 'Comment cannot be empty') });
type CommentForm = z.infer<typeof commentSchema>;

interface ActivityTimelineProps {
  serviceRequestId: string;
  workOrderId?: string;
}

type FilterCategory = 'All' | ActivityLogCategory | 'Email';

const FILTER_OPTIONS: { label: string; value: FilterCategory }[] = [
  { label: 'All', value: 'All' },
  { label: 'Status Changes', value: 'StatusChange' },
  { label: 'Communications', value: 'Communication' },
  { label: 'Emails', value: 'Email' },
  { label: 'Files', value: 'FileUpload' },
  { label: 'Financial', value: 'Financial' },
  { label: 'Notes', value: 'Note' },
];

const CATEGORY_COLORS: Record<ActivityLogCategory, { bg: string; text: string; border: string }> = {
  StatusChange: { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-400' },
  Communication: { bg: 'bg-purple-50', text: 'text-purple-700', border: 'border-purple-400' },
  FileUpload: { bg: 'bg-green-50', text: 'text-green-700', border: 'border-green-400' },
  Financial: { bg: 'bg-amber-50', text: 'text-amber-700', border: 'border-amber-400' },
  System: { bg: 'bg-gray-50', text: 'text-gray-600', border: 'border-gray-400' },
  Note: { bg: 'bg-brand-50', text: 'text-brand-700', border: 'border-brand-400' },
};

const CATEGORY_LABELS: Record<ActivityLogCategory, string> = {
  StatusChange: 'Status',
  Communication: 'Communication',
  FileUpload: 'File',
  Financial: 'Financial',
  System: 'System',
  Note: 'Note',
};

const CATEGORY_ICONS: Record<ActivityLogCategory, React.ComponentType<React.SVGProps<SVGSVGElement>>> = {
  StatusChange: ArrowPathIcon,
  Communication: ChatBubbleLeftIcon,
  FileUpload: DocumentArrowUpIcon,
  Financial: CurrencyDollarIcon,
  System: CogIcon,
  Note: PencilSquareIcon,
};

interface TimelineEntry {
  id: string;
  type: 'activity' | 'comment' | 'inbound-email';
  actorName: string;
  action: string;
  category: ActivityLogCategory | 'Email';
  createdAt: string;
  comment?: Comment;
  inboundEmail?: InboundEmail;
  isHighlighted?: boolean;
  isOutboundEmail?: boolean;
}

function renderAction(action: string): React.ReactNode {
  const linkMatch = action.match(/^(.*?) — Quote link: (\/quotes\/submit\/\S+)$/);
  if (linkMatch) {
    return (
      <>
        {linkMatch[1]}
        {' — '}
        <a href={linkMatch[2]} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:text-brand-700 underline">
          Quote link
        </a>
      </>
    );
  }
  return action;
}

function getInitials(name: string): string {
  return name
    .split(' ')
    .map(p => p[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

function formatDayHeader(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);
  const entryDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());

  if (entryDate.getTime() === today.getTime()) return 'Today';
  if (entryDate.getTime() === yesterday.getTime()) return 'Yesterday';
  return new Intl.DateTimeFormat('en-US', { month: 'long', day: 'numeric', year: 'numeric' }).format(date);
}

function groupByDay(entries: TimelineEntry[]): { date: string; label: string; entries: TimelineEntry[] }[] {
  const groups: Map<string, TimelineEntry[]> = new Map();
  for (const entry of entries) {
    const d = new Date(entry.createdAt);
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key)!.push(entry);
  }
  return Array.from(groups.entries()).map(([key, items]) => ({
    date: key,
    label: formatDayHeader(items[0].createdAt),
    entries: items,
  }));
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function EmailDetailPanel({ emailId, onClose }: { emailId: string; onClose: () => void }) {
  const { data: email, isLoading } = useQuery({
    queryKey: ['inbound-email', emailId],
    queryFn: () => inboundEmailsApi.get(emailId).then(r => r.data),
  });

  if (isLoading) return <div className="p-4 text-sm text-gray-500">Loading email...</div>;
  if (!email) return <div className="p-4 text-sm text-gray-500">Email not found</div>;

  return (
    <div className="mt-3 bg-white border border-gray-200 rounded-lg shadow-sm overflow-hidden">
      <div className="px-4 py-3 bg-gray-50 border-b border-gray-200 flex items-center justify-between">
        <div className="min-w-0">
          <p className="text-sm font-medium text-gray-900 truncate">{email.subject}</p>
          <p className="text-xs text-gray-500">
            From: {email.fromName ? `${email.fromName} <${email.fromAddress}>` : email.fromAddress}
          </p>
          <p className="text-xs text-gray-400">{formatDateTime(email.receivedAt)}</p>
        </div>
        <button onClick={onClose} className="ml-2 p-1 text-gray-400 hover:text-gray-600 flex-shrink-0">
          <XMarkIcon className="w-4 h-4" />
        </button>
      </div>

      <div className="px-4 py-3">
        {email.bodyHtml ? (
          <iframe
            srcDoc={email.bodyHtml}
            title="Email body"
            className="w-full border-0 min-h-[200px]"
            sandbox="allow-same-origin"
            onLoad={(e) => {
              const iframe = e.target as HTMLIFrameElement;
              if (iframe.contentDocument) {
                iframe.style.height = iframe.contentDocument.body.scrollHeight + 'px';
              }
            }}
          />
        ) : email.bodyText ? (
          <pre className="text-sm text-gray-700 whitespace-pre-wrap font-sans">{email.bodyText}</pre>
        ) : (
          <p className="text-sm text-gray-400 italic">No email body</p>
        )}
      </div>

      {email.attachments.length > 0 && (
        <div className="px-4 py-3 border-t border-gray-100">
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
            Attachments ({email.attachments.length})
          </p>
          <div className="flex flex-wrap gap-2">
            {email.attachments.map(att => (
              <a
                key={att.id}
                href={inboundEmailsApi.getAttachmentDownloadUrl(email.id, att.id)}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 bg-gray-50 hover:bg-gray-100 text-xs text-gray-700"
              >
                <ArrowDownTrayIcon className="w-3.5 h-3.5 text-gray-400" />
                <span className="truncate max-w-[140px]">{att.fileName}</span>
                <span className="text-gray-400">({formatFileSize(att.fileSize)})</span>
              </a>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default function ActivityTimeline({ serviceRequestId, workOrderId }: ActivityTimelineProps) {
  const queryClient = useQueryClient();
  const [activeFilter, setActiveFilter] = useState<FilterCategory>('All');
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [expandedEmailId, setExpandedEmailId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<CommentForm>({
    resolver: zodResolver(commentSchema),
  });

  const { data: activityLogs } = useQuery({
    queryKey: ['activity-logs', { serviceRequestId, workOrderId }],
    queryFn: () => activityLogsApi.list({ serviceRequestId, workOrderId }).then(r => r.data),
  });

  const commentQueryParams = workOrderId
    ? { workOrderId: String(workOrderId) }
    : { serviceRequestId: String(serviceRequestId) };

  const { data: comments } = useQuery({
    queryKey: ['comments', commentQueryParams],
    queryFn: () => commentsApi.list(commentQueryParams).then(r => r.data),
  });

  const { data: inboundEmailsData } = useQuery({
    queryKey: ['inbound-emails', serviceRequestId],
    queryFn: () => inboundEmailsApi.list(serviceRequestId, 1, 50).then(r => r.data),
    enabled: !!serviceRequestId,
  });

  const createComment = useMutation({
    mutationFn: (data: CommentForm) =>
      commentsApi.create({
        text: data.text,
        ...(workOrderId ? { workOrderId: String(workOrderId) } : { serviceRequestId: String(serviceRequestId) }),
        files: pendingFiles.length > 0 ? pendingFiles : undefined,
      }),
    onSuccess: () => {
      toast.success('Comment added');
      reset();
      setPendingFiles([]);
      queryClient.invalidateQueries({ queryKey: ['comments', commentQueryParams] });
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

  const baseUrl = import.meta.env.VITE_API_URL?.replace('/api', '') ?? 'http://localhost:5000';

  const mergedEntries = useMemo(() => {
    const entries: TimelineEntry[] = [];

    // Map activity logs — detect outbound emails (Communication category with email-related actions)
    (activityLogs ?? []).forEach(log => {
      const isHighlighted =
        (log.category === 'StatusChange' && log.action.toLowerCase().includes('completed')) ||
        log.category === 'Financial';
      const isOutboundEmail = log.category === 'Communication' &&
        (log.action.toLowerCase().includes('sent work order') ||
         log.action.toLowerCase().includes('sent proposal') ||
         log.action.toLowerCase().includes('sent email') ||
         log.action.toLowerCase().includes('sent invoice'));
      entries.push({
        id: `log-${log.id}`,
        type: 'activity',
        actorName: log.actorName,
        action: log.action,
        category: isOutboundEmail ? 'Email' : log.category,
        createdAt: log.createdAt,
        isHighlighted,
        isOutboundEmail,
      });
    });

    // Map comments
    (comments ?? []).forEach(c => {
      entries.push({
        id: `comment-${c.id}`,
        type: 'comment',
        actorName: c.author?.name ?? 'Unknown',
        action: c.text,
        category: 'Note',
        createdAt: c.createdAt,
        comment: c,
      });
    });

    // Map inbound emails
    (inboundEmailsData?.items ?? []).forEach(email => {
      entries.push({
        id: `email-${email.id}`,
        type: 'inbound-email',
        actorName: email.fromName || email.fromAddress,
        action: email.subject,
        category: 'Email',
        createdAt: email.receivedAt,
        inboundEmail: email,
      });
    });

    // Sort newest first
    entries.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

    // Filter
    if (activeFilter !== 'All') {
      if (activeFilter === 'Email') {
        return entries.filter(e => e.category === 'Email');
      }
      return entries.filter(e => e.category === activeFilter);
    }
    return entries;
  }, [activityLogs, comments, inboundEmailsData, activeFilter]);

  const dayGroups = useMemo(() => groupByDay(mergedEntries), [mergedEntries]);

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
      <h2 className="text-base font-semibold text-gray-900 mb-4">Activity</h2>

      {/* Comment form */}
      <form onSubmit={handleSubmit(data => createComment.mutate(data))} className="space-y-2 mb-6">
        <div className="flex gap-2">
          <input
            type="text"
            {...register('text')}
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
          <Button type="submit" size="sm" loading={isSubmitting || createComment.isPending}>Post</Button>
        </div>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept="image/*,video/*,.pdf"
          onChange={handleFileSelect}
          className="hidden"
        />
        {errors.text && <p className="text-xs text-red-600">{errors.text.message}</p>}

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

      {/* Category filters */}
      <div className="flex flex-wrap gap-1.5 mb-5">
        {FILTER_OPTIONS.map(opt => (
          <button
            key={opt.value}
            onClick={() => setActiveFilter(opt.value)}
            className={`px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
              activeFilter === opt.value
                ? 'bg-brand-100 text-brand-700'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {/* Timeline */}
      {mergedEntries.length === 0 ? (
        <p className="text-sm text-gray-500">No activity yet</p>
      ) : (
        <div className="space-y-6">
          {dayGroups.map(group => (
            <div key={group.date}>
              <div className="flex items-center gap-3 mb-3">
                <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">{group.label}</span>
                <div className="flex-1 h-px bg-gray-200" />
              </div>
              <div className="space-y-3">
                {group.entries.map(entry => {
                  const isEmail = entry.category === 'Email';
                  const isInboundEmail = entry.type === 'inbound-email';
                  const isSystem = entry.category === 'System';

                  // Use email-specific colors or fall back to category colors
                  const colors = isEmail
                    ? (isInboundEmail
                      ? { bg: 'bg-sky-50', text: 'text-sky-700', border: 'border-sky-400' }
                      : { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-400' })
                    : CATEGORY_COLORS[entry.category as ActivityLogCategory];

                  const IconComponent = isEmail
                    ? (isInboundEmail ? EnvelopeOpenIcon : PaperAirplaneIcon)
                    : CATEGORY_ICONS[entry.category as ActivityLogCategory];

                  const categoryLabel = isEmail
                    ? (isInboundEmail ? 'Received' : 'Sent')
                    : CATEGORY_LABELS[entry.category as ActivityLogCategory];

                  const isExpanded = isInboundEmail && expandedEmailId === entry.inboundEmail?.id;

                  return (
                    <div
                      key={entry.id}
                      className={`flex gap-3 ${entry.isHighlighted ? `border-l-2 ${colors.border} pl-3` : ''}`}
                    >
                      {/* Avatar */}
                      <div
                        className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium flex-shrink-0 ${
                          isSystem ? 'bg-gray-200 text-gray-500'
                            : isInboundEmail ? 'bg-sky-100 text-sky-700'
                            : entry.isOutboundEmail ? 'bg-blue-100 text-blue-700'
                            : 'bg-brand-100 text-brand-700'
                        }`}
                      >
                        {isSystem ? (
                          <CogIcon className="w-4 h-4" />
                        ) : isEmail ? (
                          <IconComponent className="w-4 h-4" />
                        ) : (
                          getInitials(entry.actorName)
                        )}
                      </div>

                      {/* Content */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="text-sm font-medium text-gray-900">
                            {isSystem ? 'System' : isInboundEmail ? `Email from ${entry.actorName}` : entry.actorName}
                          </span>
                          <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium ${colors.bg} ${colors.text}`}>
                            <IconComponent className="w-3 h-3" />
                            {categoryLabel}
                          </span>
                          {isInboundEmail && entry.inboundEmail && entry.inboundEmail.attachmentCount > 0 && (
                            <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-gray-100 text-gray-600">
                              <PaperClipIcon className="w-3 h-3" />
                              {entry.inboundEmail.attachmentCount}
                            </span>
                          )}
                          <span className="text-xs text-gray-400">{formatDateTime(entry.createdAt)}</span>
                        </div>

                        {/* Inbound email: subject + preview */}
                        {isInboundEmail && entry.inboundEmail ? (
                          <>
                            <p className="text-sm font-medium text-gray-800 mt-0.5">{entry.inboundEmail.subject}</p>
                            {entry.inboundEmail.bodyPreview && (
                              <p className="text-sm text-gray-500 mt-0.5 line-clamp-2">{entry.inboundEmail.bodyPreview}</p>
                            )}
                            <button
                              onClick={() => setExpandedEmailId(isExpanded ? null : entry.inboundEmail!.id)}
                              className="text-xs text-brand-600 hover:text-brand-700 mt-1 font-medium"
                            >
                              {isExpanded ? 'Hide email' : 'View full email'}
                            </button>
                            {isExpanded && (
                              <EmailDetailPanel
                                emailId={entry.inboundEmail.id}
                                onClose={() => setExpandedEmailId(null)}
                              />
                            )}
                          </>
                        ) : (
                          <p className="text-sm text-gray-700 mt-0.5">{renderAction(entry.action)}</p>
                        )}

                        {/* Comment attachments */}
                        {entry.comment?.attachments && entry.comment.attachments.length > 0 && (
                          <div className="flex flex-wrap gap-2 mt-2">
                            {entry.comment.attachments.map(att => {
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
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
