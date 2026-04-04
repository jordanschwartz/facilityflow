import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { outboundEmailsApi } from '../api/outboundEmails';
import Modal from './ui/Modal';
import Button from './ui/Button';

interface ForwardEmailModalProps {
  open: boolean;
  onClose: () => void;
  emailId: string;
  emailSubject: string;
  type: 'outbound';
}

export default function ForwardEmailModal({ open, onClose, emailId, emailSubject }: ForwardEmailModalProps) {
  const [recipientEmail, setRecipientEmail] = useState('');
  const [recipientName, setRecipientName] = useState('');

  const forward = useMutation({
    mutationFn: () => outboundEmailsApi.forward(emailId, recipientEmail, recipientName || undefined),
    onSuccess: () => {
      toast.success('Email forwarded');
      onClose();
      setRecipientEmail('');
      setRecipientName('');
    },
    onError: () => toast.error('Failed to forward email'),
  });

  return (
    <Modal open={open} onClose={onClose} title="Forward Email" size="sm">
      <div className="space-y-4">
        <div>
          <p className="text-xs text-gray-500 mb-3">Subject: {emailSubject}</p>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Recipient Email *</label>
          <input
            type="email"
            value={recipientEmail}
            onChange={(e) => setRecipientEmail(e.target.value)}
            className="w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
            placeholder="email@example.com"
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Recipient Name</label>
          <input
            type="text"
            value={recipientName}
            onChange={(e) => setRecipientName(e.target.value)}
            className="w-full border border-gray-300 rounded-lg text-sm px-3 py-2 focus:ring-brand-500 focus:border-brand-500"
            placeholder="John Doe"
          />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          <Button
            size="sm"
            onClick={() => forward.mutate()}
            loading={forward.isPending}
            disabled={!recipientEmail}
          >
            Forward
          </Button>
        </div>
      </div>
    </Modal>
  );
}
