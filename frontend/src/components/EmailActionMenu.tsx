import { useState, useRef, useEffect } from 'react';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { inboundEmailsApi } from '../api/inboundEmails';
import { outboundEmailsApi } from '../api/outboundEmails';
import { EllipsisVerticalIcon } from '@heroicons/react/24/outline';

interface EmailActionMenuProps {
  type: 'inbound' | 'outbound';
  emailId: string;
  attachmentCount?: number;
  recipientAddress?: string;
  onForward: () => void;
}

export default function EmailActionMenu({ type, emailId, attachmentCount, recipientAddress, onForward }: EmailActionMenuProps) {
  const [open, setOpen] = useState(false);
  const [showAttachmentPicker, setShowAttachmentPicker] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
        setShowAttachmentPicker(false);
      }
    }
    if (open) document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open]);

  // Fetch email detail for attachment picker only when needed
  const { data: emailDetail } = useQuery({
    queryKey: ['inbound-email', emailId],
    queryFn: () => inboundEmailsApi.get(emailId).then(r => r.data),
    enabled: showAttachmentPicker && type === 'inbound',
  });

  const createQuote = useMutation({
    mutationFn: () => inboundEmailsApi.createQuoteFromEmail(emailId),
    onSuccess: (res) => {
      toast.success('Quote created from email');
      setOpen(false);
      navigate(`/quotes/${res.data.quoteId}`);
    },
    onError: () => toast.error('Failed to create quote'),
  });

  const attachAsPo = useMutation({
    mutationFn: (attachmentId: string) => inboundEmailsApi.attachAsPo(emailId, attachmentId),
    onSuccess: () => {
      toast.success('Attachment linked as PO');
      setOpen(false);
      setShowAttachmentPicker(false);
      queryClient.invalidateQueries({ queryKey: ['service-request'] });
    },
    onError: () => toast.error('Failed to attach as PO'),
  });

  const addToNotes = useMutation({
    mutationFn: () => inboundEmailsApi.addToNotes(emailId),
    onSuccess: () => {
      toast.success('Email added to notes');
      setOpen(false);
      queryClient.invalidateQueries({ queryKey: ['activity-logs'] });
    },
    onError: () => toast.error('Failed to add to notes'),
  });

  const resend = useMutation({
    mutationFn: () => outboundEmailsApi.resend(emailId),
    onSuccess: () => {
      toast.success('Email re-sent');
      setOpen(false);
    },
    onError: () => toast.error('Failed to re-send email'),
  });

  const handleResend = () => {
    if (confirm(`Re-send this email to ${recipientAddress}?`)) {
      resend.mutate();
    }
  };

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={(e) => { e.stopPropagation(); setOpen(!open); }}
        className="p-1 text-gray-400 hover:text-gray-600 rounded hover:bg-gray-100"
      >
        <EllipsisVerticalIcon className="w-4 h-4" />
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-1 w-48 bg-white border border-gray-200 rounded-lg shadow-lg z-20 py-1">
          {type === 'inbound' ? (
            <>
              <button
                onClick={() => createQuote.mutate()}
                disabled={createQuote.isPending}
                className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Create Quote
              </button>
              {(attachmentCount ?? 0) > 0 && (
                <button
                  onClick={() => setShowAttachmentPicker(!showAttachmentPicker)}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  Attach as PO
                </button>
              )}
              <button
                onClick={() => addToNotes.mutate()}
                disabled={addToNotes.isPending}
                className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Add to Notes
              </button>
            </>
          ) : (
            <>
              <button
                onClick={handleResend}
                disabled={resend.isPending}
                className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Re-send
              </button>
              <button
                onClick={() => { setOpen(false); onForward(); }}
                className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Forward
              </button>
            </>
          )}
        </div>
      )}

      {/* Attachment picker for "Attach as PO" */}
      {showAttachmentPicker && emailDetail && (
        <div className="absolute right-0 top-full mt-1 w-56 bg-white border border-gray-200 rounded-lg shadow-lg z-30 py-1">
          <p className="px-3 py-1.5 text-xs font-medium text-gray-500 uppercase">Select attachment</p>
          {emailDetail.attachments.map(att => (
            <button
              key={att.id}
              onClick={() => attachAsPo.mutate(att.id)}
              disabled={attachAsPo.isPending}
              className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 disabled:opacity-50 truncate"
            >
              {att.fileName}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
